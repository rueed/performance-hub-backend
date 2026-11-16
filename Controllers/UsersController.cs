using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerformanceApi.Data;
using PerformanceApi.Dtos;
using PerformanceApi.Models;

namespace PerformanceApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<User> _hasher = new();

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    // Get current user ID from JWT
    private int CurrentUserId =>
        int.Parse(User.FindFirst("userId")!.Value);


    // 1. HR: Get all users
    [HttpGet]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users
            .OrderBy(u => u.Id)
            .ToListAsync();

        var result = new List<UserDto>();

        foreach (var user in users)
        {
            result.Add(await ToDtoAsync(user));
        }

        return Ok(result);
    }


    // 2. Get employees assigned to the current user
    [HttpGet("my-team")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> MyTeam()
    {
        var team = await _db.Users
            .Where(u =>
                u.ManagerId == CurrentUserId &&
                u.IsActive)
            .ToListAsync();

        if (team.Count == 0)
            return Forbid();

        var result = new List<UserDto>();

        foreach (var employee in team)
        {
            result.Add(await ToDtoAsync(employee));
        }

        return Ok(result);
    }


    // 3. Get user by ID
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _db.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        bool allowed =
            User.IsInRole("HR") ||
            user.Id == CurrentUserId ||
            (User.IsInRole("Employee") &&
             user.ManagerId == CurrentUserId &&
             user.IsActive);

        if (!allowed)
            return Forbid();

        return Ok(await ToDtoAsync(user));
    }


    // 4. HR: Create a user
    [HttpPost]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Create(CreateUserDto dto)
    {
        var email = dto.Email.Trim();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return BadRequest("Email already exists");

        if (!await _db.Departments.AnyAsync(d =>
            d.Id == dto.DepartmentId))
            return BadRequest("Department not found");

        // Users can only be HR or Employee
        if (dto.Role != Role.HR && dto.Role != Role.Employee)
            return BadRequest("Invalid role");

        if (dto.ManagerId != null)
        {
            var manager = await _db.Users.FindAsync(dto.ManagerId);

            if (manager == null ||
                !manager.IsActive ||
                manager.Role != Role.Employee)
            {
                return BadRequest("Invalid manager");
            }

            if (dto.Role == Role.HR)
                return BadRequest("HR cannot have a manager");
        }

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            Role = dto.Role,
            DepartmentId = dto.DepartmentId,
            ManagerId = dto.ManagerId,
            IsActive = true
        };

        user.PasswordHash =
            _hasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            await ToDtoAsync(user));
    }


    // 5. HR: Update a user
    [HttpPut("{id:int}")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Update(int id, UpdateUserDto dto)
    {
        var user = await _db.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        var email = dto.Email.Trim();

        if (await _db.Users.AnyAsync(u =>
            u.Email == email && u.Id != id))
            return BadRequest("Email already exists");

        if (!await _db.Departments.AnyAsync(d =>
            d.Id == dto.DepartmentId))
            return BadRequest("Department not found");

        if (dto.Role != Role.HR && dto.Role != Role.Employee)
            return BadRequest("Invalid role");

        if (dto.ManagerId == id)
            return BadRequest("A user cannot manage himself");

        if (dto.ManagerId != null)
        {
            var manager = await _db.Users.FindAsync(dto.ManagerId);

            if (manager == null ||
                !manager.IsActive ||
                manager.Role != Role.Employee)
            {
                return BadRequest("Invalid manager");
            }

            if (dto.Role == Role.HR)
                return BadRequest("HR cannot have a manager");

            // Prevent circular manager relationships
            int? nextManagerId = manager.ManagerId;

            var visited = new HashSet<int> { manager.Id };

            while (nextManagerId != null)
            {
                if (nextManagerId == id)
                    return BadRequest("Circular manager relationship");

                if (!visited.Add(nextManagerId.Value))
                    return BadRequest("Circular manager relationship");

                var nextManager = await _db.Users
                    .FindAsync(nextManagerId.Value);

                nextManagerId = nextManager?.ManagerId;
            }
        }

        user.FullName = dto.FullName.Trim();
        user.Email = email;
        user.Role = dto.Role;
        user.DepartmentId = dto.DepartmentId;
        user.ManagerId = dto.ManagerId;
        user.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();

        return Ok(await ToDtoAsync(user));
    }


    // 6. HR: Deactivate a user
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (id == CurrentUserId)
            return BadRequest("You cannot deactivate yourself");

        var user = await _db.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        user.IsActive = false;

        await _db.SaveChangesAsync();

        return NoContent();
    }


    // Convert User to UserDto and calculate IsManager
    private async Task<UserDto> ToDtoAsync(User user)
    {
        bool isManager =
            user.IsActive &&
            user.Role == Role.Employee &&
            await _db.Users.AnyAsync(u =>
                u.ManagerId == user.Id && u.IsActive);

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            DepartmentId = user.DepartmentId,
            ManagerId = user.ManagerId,
            IsActive = user.IsActive,
            IsManager = isManager
        };
    }
}