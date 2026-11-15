namespace PerformanceApi.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int? ManagerId { get; set; }
    public User? Manager { get; set; }

    public bool IsActive { get; set; } = true;
}
