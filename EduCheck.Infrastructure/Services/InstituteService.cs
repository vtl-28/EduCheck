using EduCheck.Application.DTOs.Institute;
using EduCheck.Application.Interfaces;
using EduCheck.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduCheck.Infrastructure.Services;

public class InstituteService : IInstituteService
{
    private readonly ApplicationDbContext _context;
    private readonly ISearchHistoryService _searchHistoryService;
    private readonly ILogger<InstituteService> _logger;

    public InstituteService(
        ApplicationDbContext context,
        ISearchHistoryService searchHistoryService,
        ILogger<InstituteService> logger)
    {
        _context = context;
        _searchHistoryService = searchHistoryService;
        _logger = logger;
    }

    public async Task<InstituteSearchResponse> SearchInstitutesAsync(InstituteSearchRequest request, Guid? userId = null)
    {
        _logger.LogInformation("Searching institutes with query: {Query}", request.Query);

        try
        {
            var query = request.Query.Trim();
            var isAccreditationNumberSearch = IsAccreditationNumber(query);

            var institutesQuery = _context.Institutes
                .AsNoTracking()
                .Where(i => i.IsActive);

            if (isAccreditationNumberSearch)
            {
                institutesQuery = institutesQuery
                    .Where(i => i.AccreditationNumber.ToLower() == query.ToLower());
            }
            else
            {
                institutesQuery = institutesQuery
                    .Where(i => i.InstitutionName.ToLower().Contains(query.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(request.Province))
            {
                institutesQuery = institutesQuery
                    .Where(i => i.Province != null &&
                                i.Province.ToLower() == request.Province.ToLower());
            }

            var totalCount = await institutesQuery.CountAsync();

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
            var skip = (request.Page - 1) * request.PageSize;

            var institutes = await institutesQuery
                .OrderBy(i => i.InstitutionName)
                .Skip(skip)
                .Take(request.PageSize)
                .Select(i => new InstituteDto
                {
                    Id = i.Id,
                    InstitutionName = i.InstitutionName,
                    AccreditationNumber = i.AccreditationNumber,
                    AccreditationPeriod = i.AccreditationPeriod,
                    ProviderType = i.ProviderType,
                    PostalAddress = i.PostalAddress,
                    PhysicalAddress = i.PhysicalAddress,
                    Telephone = i.Telephone,
                    Province = i.Province,
                    City = i.City,
                    IsAccredited = i.ProviderType != null &&
                                   i.ProviderType.ToLower() == "accredited"
                })
                .ToListAsync();

            _logger.LogInformation("Found {Count} institutes matching query: {Query}",
                totalCount, request.Query);

            if (userId.HasValue && institutes.Count > 0)
            {
                foreach (var institute in institutes)
                {
                    await _searchHistoryService.RecordSearchAsync(userId.Value, institute.Id);
                }
            }

            var response = new InstituteSearchResponse
            {
                Success = true,
                Message = totalCount > 0
                    ? $"{totalCount} institute(s) found"
                    : "No institutes found matching your search",
                Data = new InstituteSearchData
                {
                    Institutes = institutes,
                    Pagination = new PaginationDto
                    {
                        CurrentPage = request.Page,
                        PageSize = request.PageSize,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        HasNextPage = request.Page < totalPages,
                        HasPreviousPage = request.Page > 1
                    }
                }
            };

            if (totalCount == 0)
            {
                response.Data.Suggestions = new SearchSuggestions
                {
                    ReportFraud = true,
                    FindNearbyInstitutes = true
                };
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching institutes with query: {Query}", request.Query);

            return new InstituteSearchResponse
            {
                Success = false,
                Message = "An error occurred while searching for institutes",
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }

    public async Task<InstituteDetailResponse> GetInstituteByIdAsync(int id)
    {
        _logger.LogInformation("Getting institute by ID: {Id}", id);

        try
        {
            var institute = await _context.Institutes
                .AsNoTracking()
                .Where(i => i.Id == id && i.IsActive)
                .Select(i => new InstituteDto
                {
                    Id = i.Id,
                    InstitutionName = i.InstitutionName,
                    AccreditationNumber = i.AccreditationNumber,
                    AccreditationPeriod = i.AccreditationPeriod,
                    ProviderType = i.ProviderType,
                    PostalAddress = i.PostalAddress,
                    PhysicalAddress = i.PhysicalAddress,
                    Telephone = i.Telephone,
                    Province = i.Province,
                    City = i.City,
                    IsAccredited = i.ProviderType != null &&
                                   i.ProviderType.ToLower() == "accredited"
                })
                .FirstOrDefaultAsync();

            if (institute == null)
            {
                _logger.LogWarning("Institute not found with ID: {Id}", id);

                return new InstituteDetailResponse
                {
                    Success = false,
                    Message = "Institute not found",
                    Errors = new List<string> { $"No institute found with ID {id}" }
                };
            }

            _logger.LogInformation("Found institute: {Name}", institute.InstitutionName);

            return new InstituteDetailResponse
            {
                Success = true,
                Message = "Institute found",
                Data = institute
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting institute by ID: {Id}", id);

            return new InstituteDetailResponse
            {
                Success = false,
                Message = "An error occurred while retrieving the institute",
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }

    private static bool IsAccreditationNumber(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;

        var hasDigits = query.Any(char.IsDigit);
        var hasSpaces = query.Contains(' ');
        var digitCount = query.Count(char.IsDigit);
        var letterCount = query.Count(char.IsLetter);

        return hasDigits && (hasSpaces || digitCount > letterCount);
    }
}