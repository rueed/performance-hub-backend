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

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.Trim());

        // Same message for "no such user" and "wrong password" (don't reveal which)
        if (user is null || !user.IsActive)
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password", Status = 401 });

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password", Status = 401 });

        return Ok(new LoginResponseDto
        {
            Token = _tokens.CreateToken(user),
            User = ToDto(user)
        });
    }

    // GET /api/auth/me
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        // Read the id from the token, never from the client
        var idClaim = User.FindFirst("userId")?.Value;
        if (!int.TryParse(idClaim, out var userId))
            return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user is null || !user.IsActive)
            return Unauthorized();

        return Ok(ToDto(user));
    }

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        DepartmentId = user.DepartmentId,
        ManagerId = user.ManagerId,
        IsActive = user.IsActive
    };
}