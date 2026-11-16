using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PerformanceApi.Models;

namespace PerformanceApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var hasher = new PasswordHasher<User>();
        const string password = "Test@123"; // Development only

        // 1. Ensure departments exist
        var it = await db.Departments
            .FirstOrDefaultAsync(d => d.Name == "IT");

        if (it == null)
        {
            it = new Department { Name = "IT" };
            db.Departments.Add(it);
            await db.SaveChangesAsync();
        }

        var hrDept = await db.Departments
            .FirstOrDefaultAsync(d => d.Name == "HR");

        if (hrDept == null)
        {
            hrDept = new Department { Name = "HR" };
            db.Departments.Add(hrDept);
            await db.SaveChangesAsync();
        }

        // 2. Create or update a demo user
        async Task<User> EnsureUser(
            string email,
            string fullName,
            Role role,
            int departmentId,
            int? managerId = null,
            string? oldEmail = null)
        {
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            // Find an existing legacy demo account
            if (user == null && oldEmail != null)
            {
                user = await db.Users
                    .FirstOrDefaultAsync(u => u.Email == oldEmail);
            }

            if (user == null)
            {
                user = new User
                {
                    FullName = fullName,
                    Email = email,
                    Role = role,
                    DepartmentId = departmentId,
                    ManagerId = managerId,
                    IsActive = true
                };

                user.PasswordHash =
                    hasher.HashPassword(user, password);

                db.Users.Add(user);
            }
            else
            {
                // Update only the designated demo account
                user.FullName = fullName;
                user.Email = email;
                user.Role = role;
                user.DepartmentId = departmentId;
                user.ManagerId = managerId;
            }

            await db.SaveChangesAsync();

            return user;
        }

        // 3. HR users
        await EnsureUser(
            "sarah.ahmad@test.com",
            "Sarah Ahmad",
            Role.HR,
            hrDept.Id,
            oldEmail: "hr@test.com");

        await EnsureUser(
            "lina.khaled@test.com",
            "Lina Khaled",
            Role.HR,
            hrDept.Id);

        // 4. Managers (their actual Role is Employee)
        var omar = await EnsureUser(
            "omar.hassan@test.com",
            "Omar Hassan",
            Role.Employee,
            it.Id,
            oldEmail: "manager@test.com");

        var ahmad = await EnsureUser(
            "ahmad.saleh@test.com",
            "Ahmad Saleh",
            Role.Employee,
            it.Id);

        // 5. Omar's employees
        await EnsureUser(
            "yazan.ali@test.com",
            "Yazan Ali",
            Role.Employee,
            it.Id,
            omar.Id,
            oldEmail: "employee@test.com");

        await EnsureUser(
            "rawan.mahmoud@test.com",
            "Rawan Mahmoud",
            Role.Employee,
            it.Id,
            omar.Id);

        await EnsureUser(
            "yousef.ibrahim@test.com",
            "Yousef Ibrahim",
            Role.Employee,
            it.Id,
            omar.Id);

        // 6. Ahmad's employees
        await EnsureUser(
            "dana.nasser@test.com",
            "Dana Nasser",
            Role.Employee,
            it.Id,
            ahmad.Id);

        await EnsureUser(
            "kareem.sami@test.com",
            "Kareem Sami",
            Role.Employee,
            it.Id,
            ahmad.Id);

        await EnsureUser(
            "noor.adel@test.com",
            "Noor Adel",
            Role.Employee,
            it.Id,
            ahmad.Id);
    }
}