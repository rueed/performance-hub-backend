using Microsoft.EntityFrameworkCore;
using PerformanceApi.Models;

namespace PerformanceApi.Data;

public class AppDbContext : DbContext
{
    // The constructor receives the options (database provider, connection string...)
    // from Program.cs and passes them to the base DbContext.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Ruaa: Objectives and Evaluations
    public DbSet<Objective> Objectives { get; set; }
    public DbSet<Evaluation> Evaluations { get; set; }

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