namespace EduCheck.Application.DTOs.FraudReport;

/// <summary>
/// Response for getting a list of fraud reports.
/// </summary>
public class FraudReportsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public FraudReportsData? Data { get; set; }
    public List<string>? Errors { get; set; }
}

/// <summary>
/// Data container for fraud reports list.
/// </summary>
public class FraudReportsData
{
    public List<FraudReportDto> Reports { get; set; } = new();
    public FraudReportPaginationDto Pagination { get; set; } = null!;
}

/// <summary>
/// Response for getting a single fraud report.
/// </summary>
public class FraudReportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public FraudReportDto? Data { get; set; }
    public List<string>? Errors { get; set; }
}

/// <summary>
/// Response for creating a fraud report.
/// </summary>
public class CreateFraudReportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public FraudReportDto? Data { get; set; }
    public List<string>? Errors { get; set; }
}