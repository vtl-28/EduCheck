using EduCheck.Application.DTOs.Institute;

namespace EduCheck.Application.DTOs.SearchHistory;

public class SearchHistoryResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public SearchHistoryData? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class SearchHistoryData
{
    public List<SearchHistoryDto> History { get; set; } = new();
    public PaginationDto Pagination { get; set; } = new();
}