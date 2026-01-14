using EduCheck.Domain.Enums;

namespace EduCheck.Application.DTOs.Auth;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public UserRole Role { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }

    // Student-specific fields (null if Admin)
    public string? Province { get; set; }
    public string? City { get; set; }

    // Admin-specific fields (null if Student)
    public string? Department { get; set; }
    public string? EmployeeId { get; set; }
    public AdminLevel? AdminLevel { get; set; }
}
