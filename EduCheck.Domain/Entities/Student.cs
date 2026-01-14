using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCheck.Domain.Entities;

public class Student
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [MaxLength(50)]
    public string? Province { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [Column(TypeName = "decimal(10,8)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(11,8)")]
    public decimal? Longitude { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;

    public virtual ICollection<InstituteSearchHistory> SearchHistory { get; set; } = new List<InstituteSearchHistory>();
    public virtual ICollection<FavoriteInstitute> Favorites { get; set; } = new List<FavoriteInstitute>();
    public virtual ICollection<FraudReport> FraudReports { get; set; } = new List<FraudReport>();

    [NotMapped]
    public bool HasLocation => Latitude.HasValue && Longitude.HasValue;
}