using EduCheck.Application.DTOs.Institute;

namespace EduCheck.Application.DTOs.SearchHistory;

public class SearchHistoryDto
{
    public int Id { get; set; }
    public DateTime SearchedAt { get; set; }
    public InstituteDto Institute { get; set; } = new();
}