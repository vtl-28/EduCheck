using System.ComponentModel.DataAnnotations;

namespace EduCheck.Application.DTOs.Auth;

public class ExternalAuthRequest
{
    [Required(ErrorMessage = "Provider is required")]
    public string Provider { get; set; } = string.Empty;

    [Required(ErrorMessage = "IdToken is required")]
    public string IdToken { get; set; } = string.Empty;
}