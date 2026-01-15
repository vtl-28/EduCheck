namespace EduCheck.Application.DTOs.Institute;

public class InstituteSearchResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public InstituteSearchData? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class InstituteSearchData
{
    public List<InstituteDto> Institutes { get; set; } = new();
    public PaginationDto Pagination { get; set; } = new();
    public SearchSuggestions? Suggestions { get; set; }
}

public class SearchSuggestions
{
    public bool ReportFraud { get; set; }
    public bool FindNearbyInstitutes { get; set; }
}