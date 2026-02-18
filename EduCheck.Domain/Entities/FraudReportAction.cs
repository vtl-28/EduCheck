using EduCheck.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;

namespace EduCheck.Domain.Entities;

public class FraudReportAction
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid FraudReportId { get; set; }

    [Required]
    public Guid AdminId { get; set; }

    public FraudActionType ActionType { get; set; }

    [MaxLength(50)]
    public string? PreviousStatus { get; set; }

    [MaxLength(50)]
    public string? NewStatus { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(FraudReportId))]
    public virtual FraudReport FraudReport { get; set; } = null!;

    [ForeignKey(nameof(AdminId))]
    public virtual Admin Admin { get; set; } = null!;
}