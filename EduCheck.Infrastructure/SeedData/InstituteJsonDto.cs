using System.Text.Json.Serialization;

namespace EduCheck.Infrastructure.SeedData;

public class InstituteJsonDto
{
    [JsonPropertyName("Institution Name")]
    public string InstitutionName { get; set; } = string.Empty;

    [JsonPropertyName("Accreditation Number")]
    public string AccreditationNumber { get; set; } = string.Empty;

    [JsonPropertyName("Accreditation Period")]
    public string? AccreditationPeriod { get; set; }

    [JsonPropertyName("Provider Type")]
    public string? ProviderType { get; set; }

    [JsonPropertyName("Postal Address")]
    public string? PostalAddress { get; set; }

    [JsonPropertyName("Physical Address")]
    public string? PhysicalAddress { get; set; }

    [JsonPropertyName("Telephone")]
    public string? Telephone { get; set; }
}