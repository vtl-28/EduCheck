using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCheck.Domain.Entities;

public class InstituteSearchHistory
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int InstituteId { get; set; }

    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Institute Institute { get; set; } = null!;
}