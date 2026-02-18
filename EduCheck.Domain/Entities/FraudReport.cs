using EduCheck.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCheck.Domain.Entities;

public class FraudReport
{
    [Key]
    public Guid Id { get; set; }

    public Guid? StudentId { get; set; }

    public int? InstituteId { get; set; }

    [Required]
    [MaxLength(255)]
    public string ReportedInstituteName { get; set; } = string.Empty;

    public string? ReportedInstituteAddress { get; set; }

    [MaxLength(50)]
    public string? ReportedInstitutePhone { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    public FraudReportStatus Status { get; set; } = FraudReportStatus.Submitted;

    public FraudSeverity Severity { get; set; } = FraudSeverity.Medium;

    public bool IsAnonymous { get; set; } = false;

    [Column(TypeName = "jsonb")]
    public List<string>? EvidenceUrls { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(StudentId))]
    public virtual Student? Student { get; set; }

    [ForeignKey(nameof(InstituteId))]
    public virtual Institute? Institute { get; set; }

    public virtual ICollection<FraudReportAction> Actions { get; set; } = new List<FraudReportAction>();

    [NotMapped]
    public bool IsPending => Status == FraudReportStatus.Submitted || Status == FraudReportStatus.UnderReview;

    [NotMapped]
    public bool IsResolved => Status == FraudReportStatus.Dismissed || Status == FraudReportStatus.ActionTaken;
}