using System.ComponentModel.DataAnnotations;
using PerformanceApi.Models;

namespace PerformanceApi.Dtos;

public class UpdateUserDto
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    public Role Role { get; set; }

    public int DepartmentId { get; set; }

    public int? ManagerId { get; set; }

    public bool IsActive { get; set; } = true;
}
