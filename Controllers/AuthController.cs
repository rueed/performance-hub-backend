using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerformanceApi.Data;
using PerformanceApi.Dtos;
using PerformanceApi.Models;
using PerformanceApi.Services;

namespace PerformanceApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthController(AppDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    // Login
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.Trim());

        if (user == null || !user.IsActive)
            return Unauthorized("Invalid email or password");

        var result = _hasher.VerifyHashedPassword(
            user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid email or password");

        return Ok(new LoginResponseDto
        {
            Token = _tokens.CreateToken(user),
            User = await ToDtoAsync(user)
        });
    }

    // Get current user
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var id = User.FindFirst("userId")?.Value;

        if (!int.TryParse(id, out int userId))
            return Unauthorized();

        var user = await _db.Users.FindAsync(userId);

        if (user == null || !user.IsActive)
            return Unauthorized();

        return Ok(await ToDtoAsync(user));
    }

    // Convert User to UserDto
    private async Task<UserDto> ToDtoAsync(User user)
    {
        bool isManager = user.Role == Role.Employee &&
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