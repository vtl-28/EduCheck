namespace EduCheck.Application.DTOs.Institute;

public class InstituteDetailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public InstituteDto? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}