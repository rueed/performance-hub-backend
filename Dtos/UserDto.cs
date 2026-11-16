using PerformanceApi.Models;

namespace PerformanceApi.Dtos;

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Role Role { get; set; }
    public int DepartmentId { get; set; }
    public int? ManagerId { get; set; }
    public bool IsActive { get; set; }
}
