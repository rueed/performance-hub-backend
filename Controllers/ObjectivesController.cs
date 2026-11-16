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

    public ObjectivesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateObjectiveDto dto)
    {
        
        var userId = GetUserIdFromContext();
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

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetUserIdFromContext();
        var objectives = await _context.Objectives.Where(o => o.UserId == userId).ToListAsync();
        return Ok(objectives);
    }
    // POST api/objectives/5/submit
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        var userId = GetUserIdFromContext();

        // بدور بس بين objectives المستخدم نفسه (مش أي واحد)
        var objective = await _context.Objectives
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (objective == null)
            return NotFound();

        if (objective.Status != ObjectiveStatus.Draft &&
            objective.Status != ObjectiveStatus.Rejected)
            return BadRequest(new { title = "Only Draft or Rejected objectives can be submitted" });

        objective.Status = ObjectiveStatus.PendingApproval;
        objective.RejectionReason = null;
        objective.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(objective);
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
    private int GetUserIdFromContext() {

        var value = User.FindFirst("userId")?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    
    }
}


