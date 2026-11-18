using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PerformanceApi.Data;
using PerformanceApi.Models;

namespace PerformanceApi.Authorization;

// A "manager" is an Employee who has at least one active direct report.
// Same rule as isManager in AuthController. Checked in the database on every
// request, so it is always up to date (it is not stored in the token).
public class ManagerRequirement : IAuthorizationRequirement { }

public class ManagerHandler : AuthorizationHandler<ManagerRequirement>
{
    private readonly AppDbContext _db;

    public ManagerHandler(AppDbContext db)
    {
        _db = db;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ManagerRequirement requirement)
    {
        var value = context.User.FindFirst("userId")?.Value;
        if (!int.TryParse(value, out var userId))
            return;

        var isManager = await _db.Users.AnyAsync(u =>
            u.Id == userId &&
            u.IsActive &&
            u.Role == Role.Employee &&
            _db.Users.Any(r => r.ManagerId == u.Id && r.IsActive));

        if (isManager)
            context.Succeed(requirement);
    }
}