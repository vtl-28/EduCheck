namespace EduCheck.Application.DTOs.Institute;

public class InstituteDto
{
    public int Id { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public string AccreditationNumber { get; set; } = string.Empty;
    public string? AccreditationPeriod { get; set; }
    public string? ProviderType { get; set; }
    public string? PostalAddress { get; set; }
    public string? PhysicalAddress { get; set; }
    public string? Telephone { get; set; }
    public string? Province { get; set; }
    public string? City { get; set; }
    public bool IsAccredited { get; set; }
}