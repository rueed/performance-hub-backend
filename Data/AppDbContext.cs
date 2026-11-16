using Microsoft.EntityFrameworkCore;
using PerformanceApi.Models;//EF is a framework that allows us to interact with the database using C# code instead of SQL queries. It is an Object-Relational Mapping (ORM) framework that maps the database tables to C# classes and vice versa.

namespace PerformanceApi.Data;
public class AppDbContext : DbContext 
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)//constructor that takes DbContextOptions as a parameter and passes it to the base class constructor
    {//options are used to configure the DbContext, such as the database provider, connection string, etc.
    }
    public DbSet<Objective> Objectives { get; set; }
    public DbSet<Evaluation> Evaluations { get; set; }

}
