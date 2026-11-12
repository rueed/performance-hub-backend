using Microsoft.EntityFrameworkCore;

namespace PerformanceApi.Data;

// The single DbContext of the project. Each teammate adds her own DbSet lines,
// for example:
//   public DbSet<Objective> Objectives => Set<Objective>();
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
