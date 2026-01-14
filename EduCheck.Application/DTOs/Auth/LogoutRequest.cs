using System.ComponentModel.DataAnnotations;

namespace EduCheck.Application.DTOs.Auth;

public class LogoutRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}