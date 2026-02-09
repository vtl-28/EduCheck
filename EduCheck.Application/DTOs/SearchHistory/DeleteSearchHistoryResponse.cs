namespace EduCheck.Application.DTOs.SearchHistory;

public class DeleteSearchHistoryResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DeletedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}