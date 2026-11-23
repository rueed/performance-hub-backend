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
    private readonly AppDbContext _context;//Dependency injection of the AppDbContext class, which is used to interact with the database. The _context variable is used to access the database and perform CRUD operations on the Objectives table.

    private const int MinObjectives = 3; // an employee must submit at least 3 objectives together
    public ObjectivesController(AppDbContext context)// Dependency injection
    {
        _context = context;

    }

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

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetUserIdFromContext();
        var objectives = await _context.Objectives.Where(o => o.UserId == userId).ToListAsync();
        return Ok(objectives);
    }
    // POST api/objectives/5/submit
    // Only for a REJECTED objective (edit it, then send it again on its own).
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        var userId = GetUserIdFromContext();

        var objective = await _context.Objectives
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (objective == null)
            return NotFound();

        if (objective.Status != ObjectiveStatus.Rejected)
            return BadRequest(new { title = "Only a rejected objective can be sent again on its own" });

        objective.Status = ObjectiveStatus.PendingApproval;
        objective.RejectionReason = null;
        objective.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(objective);
    }

    // POST api/objectives/submit-drafts
    // Sends ALL my draft objectives to the manager together (at least 3).
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

    // PATCH api/objectives/5/progress
    [HttpPatch("{id}/progress")]
    public async Task<IActionResult> UpdateProgress(int id, UpdateProgressDto dto)
    {
        var userId = GetUserIdFromContext();

        var objective = await _context.Objectives
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (objective == null)
            return NotFound();

        if (objective.Status != ObjectiveStatus.Approved &&
            objective.Status != ObjectiveStatus.InProgress)
            return BadRequest(new { title = "Progress can only be updated on approved objectives" });

        if (dto.Progress < 0 || dto.Progress > 100)
            return BadRequest(new { title = "Progress must be between 0 and 100" });

        objective.Progress = dto.Progress;
        objective.Status = dto.Progress == 100
            ? ObjectiveStatus.Completed
            : ObjectiveStatus.InProgress;
        objective.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(objective);
    }
    // GET api/objectives/pending  (a manager sees what his team submitted)
    [HttpGet("pending")]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> GetPending()
    {
        var managerId = GetUserIdFromContext();

        var pending = await _context.Objectives
            .Where(o => o.Status == ObjectiveStatus.PendingApproval
                        && _context.Users.Any(u => u.Id == o.UserId && u.ManagerId == managerId))
            .ToListAsync();

        return Ok(pending);
    }

    // POST api/objectives/5/approve
    [HttpPost("{id}/approve")]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> Approve(int id)
    {
        var managerId = GetUserIdFromContext();
        var objective = await FindObjectiveOfMyTeam(id, managerId);

        if (objective == null)
            return NotFound();

        if (objective.Status != ObjectiveStatus.PendingApproval)
            return BadRequest(new { title = "Only objectives waiting for approval can be approved" });

        objective.Status = ObjectiveStatus.Approved;
        objective.ReviewedByUserId = managerId;
        objective.RejectionReason = null;
        objective.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(objective);
    }

    // POST api/objectives/5/reject   body: { "reason": "..." }
    [HttpPost("{id}/reject")]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> Reject(int id, RejectObjectiveDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest(new { title = "A rejection reason is required" });

        var managerId = GetUserIdFromContext();
        var objective = await FindObjectiveOfMyTeam(id, managerId);

        if (objective == null)
            return NotFound();

        if (objective.Status != ObjectiveStatus.PendingApproval)
            return BadRequest(new { title = "Only objectives waiting for approval can be rejected" });

        objective.Status = ObjectiveStatus.Rejected;
        objective.ReviewedByUserId = managerId;
        objective.RejectionReason = dto.Reason.Trim();
        objective.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(objective);
    }
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


    // Finds an objective only if its owner reports to this manager
    private async Task<Objective?> FindObjectiveOfMyTeam(int id, int managerId)
    {
        return await _context.Objectives.FirstOrDefaultAsync(o =>
            o.Id == id &&
            _context.Users.Any(u => u.Id == o.UserId && u.ManagerId == managerId));
    }
    private int GetUserIdFromContext() {

        var value = User.FindFirst("userId")?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    
    }
}


