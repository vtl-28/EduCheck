using System.ComponentModel.DataAnnotations;

namespace EduCheck.Application.DTOs.Auth;

public class UpdateProfileRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    // Student-only fields — ignored for admins
    [MaxLength(50)]
    public string? Province { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }
}