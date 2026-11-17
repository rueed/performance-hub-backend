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


    // 1. Admin: Get all users
    [HttpGet]
    [Authorize(Roles = "Admin")]
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


    // 2. Get employees assigned to current user
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
            User.IsInRole("Admin") ||
            user.Id == CurrentUserId ||
            (User.IsInRole("Employee") &&
             user.ManagerId == CurrentUserId &&
             user.IsActive);

        if (!allowed)
            return Forbid();

        return Ok(await ToDtoAsync(user));
    }


    // 4. Admin: Create Employee
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateUserDto dto)
    {
        // Only Employee accounts can be created through API
        if (dto.Role != Role.Employee)
            return BadRequest("Only Employee accounts can be created");

        var email = dto.Email.Trim();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return BadRequest("Email already exists");

        if (!await _db.Departments.AnyAsync(d =>
            d.Id == dto.DepartmentId))
            return BadRequest("Department not found");

        if (dto.ManagerId != null)
        {
            var manager = await _db.Users.FindAsync(dto.ManagerId);

            if (manager == null ||
                !manager.IsActive ||
                manager.Role != Role.Employee)
            {
                return BadRequest("Invalid manager");
            }
        }

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            Role = Role.Employee,
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


    // 5. Admin: Update Employee
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        UpdateUserDto dto)
    {
        var user = await _db.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        // Admin accounts are managed in Database only
        if (user.Role == Role.Admin)
            return BadRequest(
                "Admin accounts cannot be modified here");

        // Prevent changing Employee into Admin
        if (dto.Role != Role.Employee)
            return BadRequest(
                "Cannot change a user role to Admin");

        var email = dto.Email.Trim();

        if (await _db.Users.AnyAsync(u =>
            u.Email == email && u.Id != id))
        {
            return BadRequest("Email already exists");
        }

        if (!await _db.Departments.AnyAsync(d =>
            d.Id == dto.DepartmentId))
        {
            return BadRequest("Department not found");
        }

        if (dto.ManagerId == id)
            return BadRequest(
                "A user cannot manage himself");

        if (dto.ManagerId != null)
        {
            var manager = await _db.Users.FindAsync(dto.ManagerId);

            if (manager == null ||
                !manager.IsActive ||
                manager.Role != Role.Employee)
            {
                return BadRequest("Invalid manager");
            }

            // Prevent circular manager relationships
            int? nextManagerId = manager.ManagerId;

            var visited = new HashSet<int>
            {
                manager.Id
            };

            while (nextManagerId != null)
            {
                if (nextManagerId == id)
                {
                    return BadRequest(
                        "Circular manager relationship");
                }

                if (!visited.Add(nextManagerId.Value))
                {
                    return BadRequest(
                        "Circular manager relationship");
                }

                var nextManager = await _db.Users
                    .FindAsync(nextManagerId.Value);

                nextManagerId = nextManager?.ManagerId;
            }
        }

        user.FullName = dto.FullName.Trim();
        user.Email = email;
        user.Role = Role.Employee;
        user.DepartmentId = dto.DepartmentId;
        user.ManagerId = dto.ManagerId;
        user.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();

        return Ok(await ToDtoAsync(user));
    }


    // 6. Admin: Deactivate Employee
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var user = await _db.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        // Admin accounts cannot be deactivated through API
        if (user.Role == Role.Admin)
        {
            return BadRequest(
                "Admin accounts cannot be deactivated here");
        }

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
                u.ManagerId == user.Id &&
                u.IsActive);

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