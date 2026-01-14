using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCheck.Domain.Entities;

public class Institute
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string InstitutionName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string AccreditationNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? AccreditationPeriod { get; set; }

    [MaxLength(50)]
    public string? ProviderType { get; set; }

    public string? PostalAddress { get; set; }

    public string? PhysicalAddress { get; set; }

    [MaxLength(50)]
    public string? Telephone { get; set; }

    [MaxLength(50)]
    public string? Province { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [Column(TypeName = "decimal(10,8)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(11,8)")]
    public decimal? Longitude { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<InstituteSearchHistory> SearchHistory { get; set; } = new List<InstituteSearchHistory>();
    public virtual ICollection<FavoriteInstitute> Favorites { get; set; } = new List<FavoriteInstitute>();
    public virtual ICollection<FraudReport> FraudReports { get; set; } = new List<FraudReport>();

    [NotMapped]
    public bool IsAccredited =>
        !string.IsNullOrEmpty(ProviderType) &&
        ProviderType.Equals("Accredited", StringComparison.OrdinalIgnoreCase);

    [NotMapped]
    public bool HasLocation => Latitude.HasValue && Longitude.HasValue;

    [NotMapped]
    public DateTime? AccreditationDate
    {
        get
        {
            if (string.IsNullOrEmpty(AccreditationPeriod))
                return null;

            if (DateTime.TryParse(AccreditationPeriod, out var date))
                return date;

            return null;
        }
    }
}