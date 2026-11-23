using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerformanceApi.Data;
using PerformanceApi.Models;

namespace PerformanceApi.Controllers;

// HR = an account with the Admin role. Read-only: HR can only look, not change.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class HrController : ControllerBase
{
    private readonly AppDbContext _context;

    public HrController(AppDbContext context)
    {
        _context = context;
    }

    // GET api/hr/employees  -> every active employee
    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await _context.Users
            .Where(u => u.IsActive && u.Role == Role.Employee)
            .OrderBy(u => u.FullName)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                Department = _context.Departments
                    .Where(d => d.Id == u.DepartmentId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),
                ManagerName = _context.Users
                    .Where(m => m.Id == u.ManagerId)
                    .Select(m => m.FullName)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(employees);
    }

    // GET api/hr/objectives  -> the objectives of every active employee
    // (the frontend filters by user when HR clicks an employee)
    [HttpGet("objectives")]
    public async Task<IActionResult> GetAllObjectives()
    {
        var list = await _context.Objectives
            .Where(o => _context.Users.Any(u =>
                u.Id == o.UserId && u.IsActive && u.Role == Role.Employee))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(list);
    }
}