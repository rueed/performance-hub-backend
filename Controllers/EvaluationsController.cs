using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerformanceApi.Data;
using PerformanceApi.Dtos;
using PerformanceApi.Models;

namespace PerformanceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EvaluationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EvaluationsController(AppDbContext context)
    {
        _context = context;
    }

    // The id always comes from the token, never from the client.
    private int GetUserIdFromContext()
    {
        var value = User.FindFirst("userId")?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    }

    // GET api/evaluations/team-members  (a manager sees who he can evaluate)
    [HttpGet("team-members")]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> GetTeamMembers()
    {
        var managerId = GetUserIdFromContext();

        var members = await _context.Users
            .Where(u => u.ManagerId == managerId && u.IsActive)
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync();

        return Ok(members);
    }

    // POST api/evaluations  (a manager evaluates one of his team members)
    [HttpPost]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> Create(CreateEvaluationDto dto)
    {
        var managerId = GetUserIdFromContext();

        if (string.IsNullOrWhiteSpace(dto.Period))
            return BadRequest(new { title = "Period is required" });

        if (dto.Score < 0 || dto.Score > 100)
            return BadRequest(new { title = "Score must be between 0 and 100" });

        var isMyTeamMember = await _context.Users
            .AnyAsync(u => u.Id == dto.UserId && u.ManagerId == managerId);

        if (!isMyTeamMember)
            return BadRequest(new { title = "This employee is not in your team" });

        var evaluation = new Evaluation
        {
            UserId = dto.UserId,
            ManagerId = managerId,
            Period = dto.Period.Trim(),
            Score = dto.Score,
            Comment = dto.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Evaluations.Add(evaluation);
        await _context.SaveChangesAsync();

        return Ok(evaluation);
    }

    // GET api/evaluations/mine  (an employee sees his own evaluations)
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetUserIdFromContext();

        var list = await _context.Evaluations
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Ok(list);
    }

    // GET api/evaluations/team  (a manager sees the evaluations he wrote)
    [HttpGet("team")]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> GetTeam()
    {
        var managerId = GetUserIdFromContext();

        var list = await _context.Evaluations
            .Where(e => e.ManagerId == managerId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Ok(list);
    }
}