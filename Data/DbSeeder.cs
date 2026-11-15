using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PerformanceApi.Models;

namespace PerformanceApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // If there are already users, do nothing (so it never runs twice)
        if (await db.Users.AnyAsync()) return;

        var hasher = new PasswordHasher<User>();
        const string password = "Test@123"; // development only

        // 1) Departments
        var it = new Department { Name = "IT" };
        var hrDept = new Department { Name = "HR" };
        db.Departments.AddRange(it, hrDept);
        await db.SaveChangesAsync();

        // 2) HR user and Manager user
        var hr = new User
        {
            FullName = "HR Test",
            Email = "hr@test.com",
            Role = Role.HR,
            DepartmentId = hrDept.Id
        };
        hr.PasswordHash = hasher.HashPassword(hr, password);

        var manager = new User
        {
            FullName = "Manager Test",
            Email = "manager@test.com",
            Role = Role.Manager,
            DepartmentId = it.Id
        };
        manager.PasswordHash = hasher.HashPassword(manager, password);

        db.Users.AddRange(hr, manager);
        await db.SaveChangesAsync();

        // 3) Employee (his manager is the manager above)
        var employee = new User
        {
            FullName = "Employee Test",
            Email = "employee@test.com",
            Role = Role.Employee,
            DepartmentId = it.Id,
            ManagerId = manager.Id
        };
        employee.PasswordHash = hasher.HashPassword(employee, password);

        db.Users.Add(employee);
        await db.SaveChangesAsync();
    }
}