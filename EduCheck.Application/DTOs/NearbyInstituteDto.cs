namespace EduCheck.Application.DTOs;

/// <summary>
/// Response DTO for nearby institutes search
/// Includes institute details and calculated distance from user
/// </summary>
public class NearbyInstituteDto
{
    public int Id { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public string? ProviderType { get; set; }
    public string? PhysicalAddress { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    /// <summary>
    /// Distance from user's location in kilometers
    /// Rounded to 1 decimal place
    /// </summary>
    public decimal Distance { get; set; }

    public bool IsAccredited { get; set; }
}