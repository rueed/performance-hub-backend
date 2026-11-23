using Microsoft.AspNetCore.Mvc;
using PerformanceApi.Data;
using PerformanceApi.Dtos;
using PerformanceApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
namespace PerformanceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ObjectivesController : ControllerBase
{
    // Dependency injection of the AppDbContext: it is used to read and write the Objectives table.
    private readonly AppDbContext _context;

    private const int MinObjectives = 3; // an employee must submit at least 3 objectives together

    public ObjectivesController(AppDbContext context)
    {
        _context = context;
    }

    // POST api/objectives   (creates a Draft objective)
    [HttpPost]
    public async Task<IActionResult> Create(CreateObjectiveDto dto)
    {
        var userId = GetUserIdFromContext();

        // once something was submitted, no new objectives
        var alreadySubmitted = await _context.Objectives
            .AnyAsync(o => o.UserId == userId && o.Status != ObjectiveStatus.Draft);
        if (alreadySubmitted)
            return BadRequest(new { title = "You already submitted your objectives, so you cannot add new ones" });

        var objective = new Objective
        {
            Title = dto.Title,
            Description = dto.Description,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Objectives.Add(objective);
        await _context.SaveChangesAsync();

        return Ok(objective);
    }

    // PUT api/objectives/5   (edit a Draft or a Rejected objective)
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CreateObjectiveDto dto)
    {
        var userId = GetUserIdFromContext();

        // only my own objective
        var objective = await _context.Objectives
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (objective == null)
            return NotFound();

        if (objective.Status != ObjectiveStatus.Draft &&
            objective.Status != ObjectiveStatus.Rejected)
            return BadRequest(new { title = "Only Draft or Rejected objectives can be edited" });

        objective.Title = dto.Title;
        objective.Description = dto.Description;
        objective.StartDate = dto.StartDate;
        objective.EndDate = dto.EndDate;
        objective.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(objective);
    }

    // GET api/objectives/mine
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetUserIdFromContext();
        var objectives = await _context.Objectives.Where(o => o.UserId == userId).ToListAsync();
        return Ok(objectives);
    }

    // POST api/objectives/submit-drafts   body: { "ids": [1, 2, 3] }
    // Sends the chosen draft objectives to the manager together (at least 3).
    // Drafts that are not in the list stay as drafts.
    [HttpPost("submit-drafts")]
    public async Task<IActionResult> SubmitDrafts(SubmitObjectivesDto dto)
    {
        var userId = GetUserIdFromContext();

        var alreadySubmitted = await _context.Objectives
            .AnyAsync(o => o.UserId == userId && o.Status != ObjectiveStatus.Draft);
        if (alreadySubmitted)
            return BadRequest(new { title = "You already submitted your objectives" });

        var ids = dto.Ids.Distinct().ToList();
        if (ids.Count < MinObjectives)
            return BadRequest(new { title = $"You need at least {MinObjectives} objectives to submit" });

        // only my own objectives (all of them are drafts, checked above)
        var chosen = await _context.Objectives
            .Where(o => o.UserId == userId && ids.Contains(o.Id))
            .ToListAsync();

        if (chosen.Count != ids.Count)
            return BadRequest(new { title = "Some of the objectives were not found" });

        foreach (var objective in chosen)
        {
            objective.Status = ObjectiveStatus.PendingApproval;
            objective.RejectionReason = null;
            objective.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(chosen);
    }

    // POST api/objectives/resubmit-rejected
    // The employee sends ALL his rejected objectives again (after editing them).
    [HttpPost("resubmit-rejected")]
    public async Task<IActionResult> ResubmitRejected()
    {
        var userId = GetUserIdFromContext();

        var rejected = await _context.Objectives
            .Where(o => o.UserId == userId && o.Status == ObjectiveStatus.Rejected)
            .ToListAsync();

        if (rejected.Count == 0)
            return BadRequest(new { title = "You have no rejected objectives to resubmit" });

        foreach (var objective in rejected)
        {
            objective.Status = ObjectiveStatus.PendingApproval;
            objective.RejectionReason = null;
            objective.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    // GET api/objectives/team   (objectives of the employees who report to me)
    [HttpGet("team")]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> GetTeam()
    {
        var managerId = GetUserIdFromContext();

        var list = await _context.Objectives
            .Where(o => _context.Users.Any(u => u.Id == o.UserId && u.ManagerId == managerId))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(list);
    }

    // POST api/objectives/team/5/approve
    // The manager approves ALL pending objectives of one employee together.
    [HttpPost("team/{userId:int}/approve")]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> ApproveTeamMember(int userId)
    {
        var managerId = GetUserIdFromContext();
        if (!await IsInMyTeam(userId, managerId))
            return NotFound(new { title = "This employee is not in your team" });

        var pending = await _context.Objectives
            .Where(o => o.UserId == userId && o.Status == ObjectiveStatus.PendingApproval)
            .ToListAsync();

        if (pending.Count == 0)
            return BadRequest(new { title = "This employee has no objectives waiting for approval" });

        foreach (var objective in pending)
        {
            objective.Status = ObjectiveStatus.Approved;
            objective.RejectionReason = null;
            objective.ReviewedByUserId = managerId;
            objective.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    // POST api/objectives/team/5/reject   body: { "reason": "..." }
    // The manager rejects ALL pending objectives of one employee together, with one reason.
    [HttpPost("team/{userId:int}/reject")]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> RejectTeamMember(int userId, RejectTeamMemberDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest(new { title = "Please write the reason for rejecting" });

        var managerId = GetUserIdFromContext();
        if (!await IsInMyTeam(userId, managerId))
            return NotFound(new { title = "This employee is not in your team" });

        var pending = await _context.Objectives
            .Where(o => o.UserId == userId && o.Status == ObjectiveStatus.PendingApproval)
            .ToListAsync();

        if (pending.Count == 0)
            return BadRequest(new { title = "This employee has no objectives waiting for approval" });

        foreach (var objective in pending)
        {
            objective.Status = ObjectiveStatus.Rejected;
            objective.RejectionReason = dto.Reason.Trim();
            objective.ReviewedByUserId = managerId;
            objective.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    // Is this employee a direct, active report of the manager?
    private async Task<bool> IsInMyTeam(int employeeId, int managerId)
    {
        return await _context.Users.AnyAsync(u =>
            u.Id == employeeId && u.ManagerId == managerId && u.IsActive);
    }

    // Reads the claim "userId" from the JWT token of the current request
    private int GetUserIdFromContext()
    {
        var value = User.FindFirst("userId")?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    }
}