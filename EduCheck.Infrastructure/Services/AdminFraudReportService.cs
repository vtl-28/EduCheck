using EduCheck.Application.DTOs.Admin;
using EduCheck.Application.Interfaces;
using EduCheck.Domain.Enums;
using EduCheck.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduCheck.Infrastructure.Services;

/// <summary>
/// Service for admin fraud report management.
/// </summary>
public class AdminFraudReportService : IAdminFraudReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminFraudReportService> _logger;

    public AdminFraudReportService(
        ApplicationDbContext context,
        ILogger<AdminFraudReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AdminFraudReportsResponse> GetAllReportsAsync(AdminFraudReportFilterRequest filter)
    {
        try
        {
            // Validate pagination parameters
            filter.Page = Math.Max(1, filter.Page);
            filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);

            // Start with base query
            var query = _context.FraudReports
                .AsNoTracking()
                .Include(r => r.Student)
                    .ThenInclude(s => s!.User)
                .AsQueryable();

            // Apply filters
            if (filter.Status.HasValue)
            {
                query = query.Where(r => r.Status == filter.Status.Value);
            }

            if (filter.Severity.HasValue)
            {
                query = query.Where(r => r.Severity == filter.Severity.Value);
            }

            if (filter.FromDate.HasValue)
            {
                var fromDate = filter.FromDate.Value.Date;
                query = query.Where(r => r.CreatedAt >= fromDate);
            }

            if (filter.ToDate.HasValue)
            {
                var toDate = filter.ToDate.Value.Date.AddDays(1); // Include entire day
                query = query.Where(r => r.CreatedAt < toDate);
            }

            if (!string.IsNullOrWhiteSpace(filter.Province))
            {
                query = query.Where(r => r.ReportedInstituteAddress != null &&
                                         r.ReportedInstituteAddress.ToLower().Contains(filter.Province.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                query = query.Where(r => r.ReportedInstituteAddress != null &&
                                         r.ReportedInstituteAddress.ToLower().Contains(filter.City.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(r => r.ReportedInstituteName.ToLower().Contains(searchTerm));
            }

            // Order by most recent first
            query = query.OrderByDescending(r => r.CreatedAt);

            // Get total count
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

            // Get paginated results
            var reports = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(r => new AdminFraudReportDto
                {
                    Id = r.Id,
                    ReportedInstituteName = r.ReportedInstituteName,
                    ReportedInstituteAddress = r.ReportedInstituteAddress,
                    ReportedInstitutePhone = r.ReportedInstitutePhone,
                    Description = r.Description,
                    Status = r.Status.ToString(),
                    Severity = r.Severity.ToString(),
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    Reporter = r.StudentId != null && r.Student != null ? new ReporterDto
                    {
                        StudentId = r.Student.Id,
                        FullName = $"{r.Student.User.FirstName} {r.Student.User.LastName}",
                        Email = r.Student.User.Email ?? string.Empty
                    } : null
                })
                .ToListAsync();

            _logger.LogInformation(
                "Admin retrieved fraud reports. TotalCount: {TotalCount}, Page: {Page}, PageSize: {PageSize}",
                totalCount, filter.Page, filter.PageSize);

            return new AdminFraudReportsResponse
            {
                Success = true,
                Message = totalCount > 0
                    ? $"{totalCount} report(s) found"
                    : "No reports found",
                Data = new AdminFraudReportsData
                {
                    Reports = reports,
                    Pagination = new AdminPaginationDto
                    {
                        CurrentPage = filter.Page,
                        PageSize = filter.PageSize,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        HasNextPage = filter.Page < totalPages,
                        HasPreviousPage = filter.Page > 1
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fraud reports for admin");

            return new AdminFraudReportsResponse
            {
                Success = false,
                Message = "An error occurred while retrieving reports",
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }

    /// <inheritdoc />
    public async Task<AdminFraudReportResponse> GetReportByIdAsync(Guid reportId)
    {
        try
        {
            var report = await _context.FraudReports
                .AsNoTracking()
                .Include(r => r.Student)
                    .ThenInclude(s => s!.User)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
            {
                _logger.LogWarning("Admin get fraud report - not found. ReportId: {ReportId}", reportId);

                return new AdminFraudReportResponse
                {
                    Success = false,
                    Message = "Report not found",
                    Errors = new List<string> { "The requested report was not found." }
                };
            }

            _logger.LogInformation("Admin retrieved fraud report. ReportId: {ReportId}", reportId);

            return new AdminFraudReportResponse
            {
                Success = true,
                Message = "Report retrieved successfully",
                Data = new AdminFraudReportDto
                {
                    Id = report.Id,
                    ReportedInstituteName = report.ReportedInstituteName,
                    ReportedInstituteAddress = report.ReportedInstituteAddress,
                    ReportedInstitutePhone = report.ReportedInstitutePhone,
                    Description = report.Description,
                    Status = report.Status.ToString(),
                    Severity = report.Severity.ToString(),
                    CreatedAt = report.CreatedAt,
                    UpdatedAt = report.UpdatedAt,
                    Reporter = report.StudentId != null && report.Student != null ? new ReporterDto
                    {
                        StudentId = report.Student.Id,
                        FullName = $"{report.Student.User.FirstName} {report.Student.User.LastName}",
                        Email = report.Student.User.Email ?? string.Empty
                    } : null
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fraud report for admin. ReportId: {ReportId}", reportId);

            return new AdminFraudReportResponse
            {
                Success = false,
                Message = "An error occurred while retrieving the report",
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }

    /// <inheritdoc />
    public async Task<FraudReportStatisticsResponse> GetStatisticsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var weekStart = todayStart.AddDays(-(int)todayStart.DayOfWeek);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var stats = await _context.FraudReports
                .AsNoTracking()
                .GroupBy(r => 1) // Group all into one
                .Select(g => new FraudReportStatisticsDto
                {
                    TotalReports = g.Count(),
                    SubmittedCount = g.Count(r => r.Status == FraudReportStatus.Submitted),
                    UnderReviewCount = g.Count(r => r.Status == FraudReportStatus.UnderReview),
                    VerifiedCount = g.Count(r => r.Status == FraudReportStatus.Verified),
                    DismissedCount = g.Count(r => r.Status == FraudReportStatus.Dismissed),
                    ActionTakenCount = g.Count(r => r.Status == FraudReportStatus.ActionTaken),
                    ReportsToday = g.Count(r => r.CreatedAt >= todayStart),
                    ReportsThisWeek = g.Count(r => r.CreatedAt >= weekStart),
                    ReportsThisMonth = g.Count(r => r.CreatedAt >= monthStart)
                })
                .FirstOrDefaultAsync();

            // If no reports exist, return zeros
            stats ??= new FraudReportStatisticsDto();

            _logger.LogInformation("Admin retrieved fraud report statistics. TotalReports: {TotalReports}", stats.TotalReports);

            return new FraudReportStatisticsResponse
            {
                Success = true,
                Message = "Statistics retrieved successfully",
                Data = stats
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fraud report statistics");

            return new FraudReportStatisticsResponse
            {
                Success = false,
                Message = "An error occurred while retrieving statistics",
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }
}