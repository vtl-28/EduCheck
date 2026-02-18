namespace EduCheck.Application.DTOs.Admin;

/// <summary>
/// Response for getting a list of fraud reports (admin).
/// </summary>
public class AdminFraudReportsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public AdminFraudReportsData? Data { get; set; }
    public List<string>? Errors { get; set; }
}

/// <summary>
/// Data container for admin fraud reports list.
/// </summary>
public class AdminFraudReportsData
{
    public List<AdminFraudReportDto> Reports { get; set; } = new();
    public AdminPaginationDto Pagination { get; set; } = null!;
}

/// <summary>
/// Response for getting a single fraud report (admin).
/// </summary>
public class AdminFraudReportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public AdminFraudReportDto? Data { get; set; }
    public List<string>? Errors { get; set; }
}

/// <summary>
/// Response for fraud report statistics.
/// </summary>
public class FraudReportStatisticsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public FraudReportStatisticsDto? Data { get; set; }
    public List<string>? Errors { get; set; }
}