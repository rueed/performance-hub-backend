using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerformanceApi.Data;
using PerformanceApi.Models;

namespace PerformanceApi.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DepartmentsController(AppDbContext db)
    {
        _db = db;
    }

    // 1. Get all departments
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var departments = await _db.Departments.ToListAsync();

        return Ok(departments);
    }

    // 2. Admin: Create a department
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Department department)
    {
        if (string.IsNullOrWhiteSpace(department.Name))
            return BadRequest("Department name is required");

        department.Name = department.Name.Trim();

        if (await _db.Departments.AnyAsync(d => d.Name == department.Name))
            return BadRequest("Department already exists");

        _db.Departments.Add(department);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetAll),
            new { },
            department);
    }

    // 3. HR: Update a department
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, Department updated)
    {
        var department = await _db.Departments.FindAsync(id);

        if (department == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(updated.Name))
            return BadRequest("Department name is required");

        var name = updated.Name.Trim();

        if (await _db.Departments.AnyAsync(d =>
            d.Name == name && d.Id != id))
        {
            return BadRequest("Department already exists");
        }

        department.Name = name;

        await _db.SaveChangesAsync();

        return Ok(department);
    }
}
