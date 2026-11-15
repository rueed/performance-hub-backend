using Microsoft.EntityFrameworkCore;
using PerformanceApi.Models;

namespace PerformanceApi.Data;

// The single DbContext of the project. Each teammate adds her own DbSet lines,
// for example:
//   public DbSet<Objective> Objectives => Set<Objective>();
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Jana: Users and Departments
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>(entity =>
        {
            entity.Property(d => d.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(d => d.Name).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(u => u.Email).IsUnique();

            // Store the Role as text ("HR") not a number
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(u => u.Department)
                  .WithMany()
                  .HasForeignKey(u => u.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.Manager)
                  .WithMany()
                  .HasForeignKey(u => u.ManagerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}