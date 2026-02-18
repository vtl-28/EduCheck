using EduCheck.Application.DTOs.FraudReport;
using EduCheck.Application.Interfaces;
using EduCheck.Domain.Entities;
using EduCheck.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduCheck.Infrastructure.Services;

/// <summary>
/// Service for managing fraud reports.
/// Implements IDOR prevention, rate limiting check, and audit logging.
/// </summary>
public class FraudReportService : IFraudReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FraudReportService> _logger;

    private const int MAX_REPORTS_PER_DAY = 5;

    public FraudReportService(
        ApplicationDbContext context,
        ILogger<FraudReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CreateFraudReportResponse> CreateReportAsync(Guid userId, CreateFraudReportRequest request)
    {
        try
        {
            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                _logger.LogWarning("Create fraud report failed - student not found. UserId: {UserId}", userId);

                return new CreateFraudReportResponse
                {
                    Success = false,
                    Message = "Student profile not found",
                    Errors = new List<string> { "Your student profile was not found. Please contact support." }
                };
            }

            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1);

            var reportsToday = await _context.FraudReports
                .CountAsync(r => r.StudentId == student.Id &&
                                 r.CreatedAt >= todayStart &&
                                 r.CreatedAt < todayEnd);

            if (reportsToday >= MAX_REPORTS_PER_DAY)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for fraud reports. UserId: {UserId}, StudentId: {StudentId}, ReportsToday: {Count}",
                    userId, student.Id, reportsToday);

                return new CreateFraudReportResponse
                {
                    Success = false,
                    Message = "Daily report limit reached",
                    Errors = new List<string> { $"You can only submit {MAX_REPORTS_PER_DAY} reports per day. Please try again tomorrow." }
                };
            }

            var report = new FraudReport
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                InstituteId = null,
                ReportedInstituteName = request.ReportedInstituteName.Trim(),
                ReportedInstituteAddress = request.ReportedInstituteAddress?.Trim(),
                ReportedInstitutePhone = request.ReportedInstitutePhone?.Trim(),
                Description = request.Description.Trim(),
                IsAnonymous = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.FraudReports.Add(report);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "AUDIT: Fraud report submitted. UserId: {UserId}, StudentId: {StudentId}, ReportId: {ReportId}, InstituteName: {InstituteName}",
                userId, student.Id, report.Id, report.ReportedInstituteName);

            return new CreateFraudReportResponse
            {
                Success = true,
                Message = "Fraud report submitted successfully",
                Data = MapToDto(report)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fraud report. UserId: {UserId}", userId);

            return new CreateFraudReportResponse
            {
                Success = false,
                Message = "An error occurred while submitting the report",
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }

    /// <inheritdoc />
    public async Task<FraudReportsResponse> GetUserReportsAsync(Guid userId, int page = 1, int pageSize = 10)
    {
        try
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                _logger.LogWarning("Get fraud reports failed - student not found. UserId: {UserId}", userId);

                return new FraudReportsResponse
                {
                    Success = false,
                    Message = "Student profile not found",
                    Errors = new List<string> { "Your student profile was not found. Please contact support." }
                };
            }

            var query = _context.FraudReports
                .AsNoTracking()
                .Where(r => r.StudentId == student.Id)
                .OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var reports = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new FraudReportDto
                {
                    Id = r.Id,
                    ReportedInstituteName = r.ReportedInstituteName,
                    ReportedInstituteAddress = r.ReportedInstituteAddress,
                    ReportedInstitutePhone = r.ReportedInstitutePhone,
                    Description = r.Description,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return new FraudReportsResponse
            {
                Success = true,
                Message = totalCount > 0
                    ? $"{totalCount} report(s) found"
                    : "No reports found",
                Data = new FraudReportsData
                {
                    Reports = reports,
                    Pagination = new FraudReportPaginationDto
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        HasNextPage = page < totalPages,
                        HasPreviousPage = page > 1
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fraud reports. UserId: {UserId}", userId);

            return new FraudReportsResponse
            {
                Success = false,
                Message = "An error occurred while retrieving reports",
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }

    /// <inheritdoc />
    public async Task<FraudReportResponse> GetReportByIdAsync(Guid userId, Guid reportId)
    {
        try
        {
            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                _logger.LogWarning("Get fraud report failed - student not found. UserId: {UserId}", userId);

                return new FraudReportResponse
                {
                    Success = false,
                    Message = "Student profile not found",
                    Errors = new List<string> { "Your student profile was not found. Please contact support." }
                };
            }

            var report = await _context.FraudReports
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == reportId && r.StudentId == student.Id);

            if (report == null)
            {
                _logger.LogWarning(
                    "Get fraud report - not found or not owned. UserId: {UserId}, StudentId: {StudentId}, ReportId: {ReportId}",
                    userId, student.Id, reportId);

                return new FraudReportResponse
                {
                    Success = false,
                    Message = "Report not found",
                    Errors = new List<string> { "The requested report was not found or you don't have access to it." }
                };
            }

            return new FraudReportResponse
            {
                Success = true,
                Message = "Report retrieved successfully",
                Data = MapToDto(report)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fraud report. UserId: {UserId}, ReportId: {ReportId}", userId, reportId);

            return new FraudReportResponse
            {
                Success = false,
                Message = "An error occurred while retrieving the report",
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }


    private static FraudReportDto MapToDto(FraudReport report)
    {
        return new FraudReportDto
        {
            Id = report.Id,
            ReportedInstituteName = report.ReportedInstituteName,
            ReportedInstituteAddress = report.ReportedInstituteAddress,
            ReportedInstitutePhone = report.ReportedInstitutePhone,
            Description = report.Description,
            CreatedAt = report.CreatedAt
        };
    }
}