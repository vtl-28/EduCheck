using System.ComponentModel.DataAnnotations;

namespace EduCheck.Application.DTOs.Institute;

public class InstituteSearchRequest
{
    [Required(ErrorMessage = "Search query is required")]
    [MinLength(2, ErrorMessage = "Search query must be at least 2 characters")]
    [MaxLength(255, ErrorMessage = "Search query cannot exceed 255 characters")]
    public string Query { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "Province cannot exceed 50 characters")]
    public string? Province { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
    public int Page { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "Page size must be between 1 and 50")]
    public int PageSize { get; set; } = 10;
}