using EduCheck.Application.DTOs.Admin;
using EduCheck.Domain.Entities;
using EduCheck.Domain.Enums;
using EduCheck.Infrastructure.Data;
using EduCheck.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EduCheck.Tests.Services;

public class AdminFraudReportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<AdminFraudReportService>> _loggerMock;
    private readonly AdminFraudReportService _service;

    private Guid _studentId;
    private Guid _userId;

    public AdminFraudReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<AdminFraudReportService>>();

        _service = new AdminFraudReportService(_context, _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        _userId = Guid.NewGuid();
        _studentId = Guid.NewGuid();

        // Create user
        var user = new ApplicationUser
        {
            Id = _userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            UserName = "john.doe@example.com",
            Role = UserRole.Student
        };
        _context.Users.Add(user);

        // Create student
        var student = new Student
        {
            Id = _studentId,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Students.Add(student);

        // Create fraud reports with different statuses and dates
        var reports = new List<FraudReport>
        {
            new FraudReport
            {
                Id = Guid.NewGuid(),
                StudentId = _studentId,
                ReportedInstituteName = "Fake University A",
                ReportedInstituteAddress = "123 Main St, Johannesburg, Gauteng",
                Description = "Fraudulent activity A",
                Status = FraudReportStatus.Submitted,
                Severity = FraudSeverity.High,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new FraudReport
            {
                Id = Guid.NewGuid(),
                StudentId = _studentId,
                ReportedInstituteName = "Fake University B",
                ReportedInstituteAddress = "456 Oak Ave, Cape Town, Western Cape",
                Description = "Fraudulent activity B",
                Status = FraudReportStatus.UnderReview,
                Severity = FraudSeverity.Medium,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new FraudReport
            {
                Id = Guid.NewGuid(),
                StudentId = _studentId,
                ReportedInstituteName = "Fake College C",
                ReportedInstituteAddress = "789 Pine Rd, Durban, KwaZulu-Natal",
                Description = "Fraudulent activity C",
                Status = FraudReportStatus.Verified,
                Severity = FraudSeverity.Critical,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new FraudReport
            {
                Id = Guid.NewGuid(),
                StudentId = _studentId,
                ReportedInstituteName = "Scam Institute D",
                ReportedInstituteAddress = "321 Elm St, Pretoria, Gauteng",
                Description = "Fraudulent activity D",
                Status = FraudReportStatus.Dismissed,
                Severity = FraudSeverity.Low,
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                UpdatedAt = DateTime.UtcNow.AddDays(-55)
            }
        };

        _context.FraudReports.AddRange(reports);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetAllReportsAsync Tests

    [Fact]
    public async Task GetAllReportsAsync_ReturnsAllReports_WhenNoFilters()
    {
        // Arrange
        var filter = new AdminFraudReportFilterRequest();

        // Act
        var result = await _service.GetAllReportsAsync(filter);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Reports.Should().HaveCount(4);
        result.Data.Pagination.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task GetAllReportsAsync_ReturnsReports_SortedByMostRecentFirst()
    {
        // Arrange
        var filter = new AdminFraudReportFilterRequest();

        // Act
        var result = await _service.GetAllReportsAsync(filter);

        // Assert
        result.Data!.Reports[0].ReportedInstituteName.Should().Be("Fake University A");
        result.Data.Reports[1].ReportedInstituteName.Should().Be("Fake University B");
    }

    [Fact]
    public async Task GetAllReportsAsync_FiltersBy_Status()
    {
        // Arrange
        var filter = new AdminFraudReportFilterRequest
        {
            Status = FraudReportStatus.Submitted
        };

        // Act
        var result = await _service.GetAllReportsAsync(filter);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Reports.Should().HaveCount(1);
        result.Data.Reports[0].Status.Should().Be("Submitted");
    }

    [Fact]
    public async Task GetAllReportsAsync_FiltersBy_Severity()
    {
        // Arrange
        var filter = new AdminFraudReportFilterRequest
        {
            Severity = FraudSeverity.Critical
        };

        // Act
        var result = await _service.GetAllReportsAsync(filter);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Reports.Should().HaveCount(1);
        result.Data.Reports[0].Severity.Should().Be("Critical");
    }

    [Fact]
    public async Task GetAllReportsAsync_FiltersBy_DateRange()
    {
        // Arrange
        var filter = new AdminFraudReportFilterRequest
        {
            FromDate = DateTime.UtcNow.AddDays(-10),
            ToDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GetAllReportsAsync(filter);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Reports.Should().HaveCount(2); // Reports from last 10 days
    }

    [Fact]
    public async Task GetAllReportsAsync_FiltersBy_Province()
    {
        // Arrange
        var filter = new AdminFraudReportFilterRequest
        {
            Province = "Gauteng"
        };

        // Act
        var result = await _service.GetAllReportsAsync(filter);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Reports.Should().HaveCount(2); // Johannesburg and Pretoria
    }

    [Fact]
    public async Task GetAllReportsAsync_FiltersBy_City()
    {
        // Arrange
        var filter = new AdminFraudReportFilterRequest
        {
            City = "Cape Town"
        };

        // Act
        var result = await _service.GetAllReportsAsync(filter);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Reports.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllReportsAsync_FiltersBy_SearchTerm()
    {
        // Arrange
        var filter = new AdminFraudReportFilterRequest
        {
            SearchTerm = "College"
        };

        // Act
        var result = await _service.GetAllReportsAsync(filter);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Reports.Should().HaveCount(1);
        result.Data.Reports[0].ReportedInstituteName.Should().Contain("College");
    }

    [Fact]
    public async Task GetAllReportsAsync_ReturnsPaginatedResults()
    {
        // Arrange
        var filter = new AdminFraudReportFilterRequest
        {
            Page = 1,
            PageSize = 2
        };

        // Act
        var result = await _service.GetAllReportsAsync(filter);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Reports.Should().HaveCount(2);
        result.Data.Pagination.CurrentPage.Should().Be(1);
        result.Data.Pagination.PageSize.Should().Be(2);
        result.Data.Pagination.TotalCount.Should().Be(4);
        result.Data.Pagination.TotalPages.Should().Be(2);
        result.Data.Pagination.HasNextPage.Should().BeTrue();
        result.Data.Pagination.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllReportsAsync_IncludesReporterInfo()
    {
        // Arrange
        var filter = new AdminFraudReportFilterRequest();

        // Act
        var result = await _service.GetAllReportsAsync(filter);

        // Assert
        result.Data!.Reports[0].Reporter.Should().NotBeNull();
        result.Data.Reports[0].Reporter!.FullName.Should().Be("John Doe");
        result.Data.Reports[0].Reporter.Email.Should().Be("john.doe@example.com");
    }

    [Fact]
    public async Task GetAllReportsAsync_ClampsPageSizeToMax100()
    {
        // Arrange
        var filter = new AdminFraudReportFilterRequest
        {
            PageSize = 200
        };

        // Act
        var result = await _service.GetAllReportsAsync(filter);

        // Assert
        result.Data!.Pagination.PageSize.Should().Be(100);
    }

    #endregion

    #region GetReportByIdAsync Tests

    [Fact]
    public async Task GetReportByIdAsync_ReturnsReport_WhenFound()
    {
        // Arrange
        var existingReport = await _context.FraudReports.FirstAsync();

        // Act
        var result = await _service.GetReportByIdAsync(existingReport.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(existingReport.Id);
    }

    [Fact]
    public async Task GetReportByIdAsync_ReturnsError_WhenNotFound()
    {
        // Act
        var result = await _service.GetReportByIdAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task GetReportByIdAsync_IncludesReporterInfo()
    {
        // Arrange
        var existingReport = await _context.FraudReports.FirstAsync();

        // Act
        var result = await _service.GetReportByIdAsync(existingReport.Id);

        // Assert
        result.Data!.Reporter.Should().NotBeNull();
        result.Data.Reporter!.FullName.Should().Be("John Doe");
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_ReturnsCorrectCounts()
    {
        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalReports.Should().Be(4);
        result.Data.SubmittedCount.Should().Be(1);
        result.Data.UnderReviewCount.Should().Be(1);
        result.Data.VerifiedCount.Should().Be(1);
        result.Data.DismissedCount.Should().Be(1);
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsZeros_WhenNoReports()
    {
        // Arrange - Clear all reports
        _context.FraudReports.RemoveRange(_context.FraudReports);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.TotalReports.Should().Be(0);
        result.Data.SubmittedCount.Should().Be(0);
    }

    #endregion
}