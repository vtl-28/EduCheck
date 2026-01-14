using System.ComponentModel.DataAnnotations;

namespace EduCheck.Application.DTOs.Auth;

public class GoogleAuthRequest
{
    [Required(ErrorMessage = "Authorization code is required")]
    public string Code { get; set; } = string.Empty;
}