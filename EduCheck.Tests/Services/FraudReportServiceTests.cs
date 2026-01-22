using EduCheck.Application.DTOs.FraudReport;
using EduCheck.Domain.Entities;
using EduCheck.Infrastructure.Data;
using EduCheck.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EduCheck.Tests.Services;

public class FraudReportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<FraudReportService>> _loggerMock;
    private readonly FraudReportService _service;

    // User IDs (from JWT token)
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    // Student IDs (from students table)
    private Guid _testStudentId;
    private Guid _otherStudentId;

    public FraudReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<FraudReportService>>();

        _service = new FraudReportService(_context, _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create Student IDs
        _testStudentId = Guid.NewGuid();
        _otherStudentId = Guid.NewGuid();

        // Add test students
        _context.Students.AddRange(new[]
        {
            new Student
            {
                Id = _testStudentId,
                UserId = _testUserId,
                CreatedAt = DateTime.UtcNow
            },
            new Student
            {
                Id = _otherStudentId,
                UserId = _otherUserId,
                CreatedAt = DateTime.UtcNow
            }
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreateReportAsync Tests

    [Fact]
    public async Task CreateReportAsync_CreatesReportSuccessfully()
    {
        // Arrange
        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "Fake University",
            ReportedInstituteAddress = "123 Fake Street",
            ReportedInstitutePhone = "011-123-4567",
            Description = "This institute is collecting money but has no accreditation."
        };

        // Act
        var result = await _service.CreateReportAsync(_testUserId, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ReportedInstituteName.Should().Be("Fake University");
        result.Data.Description.Should().Contain("collecting money");

        // Verify in database
        var report = await _context.FraudReports.FirstOrDefaultAsync(r => r.StudentId == _testStudentId);
        report.Should().NotBeNull();
        report!.IsAnonymous.Should().BeFalse();
        report.InstituteId.Should().BeNull();
    }

    [Fact]
    public async Task CreateReportAsync_TrimsWhitespace()
    {
        // Arrange
        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "  Fake University  ",
            ReportedInstituteAddress = "  123 Fake Street  ",
            Description = "  This is a description with whitespace.  "
        };

        // Act
        var result = await _service.CreateReportAsync(_testUserId, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.ReportedInstituteName.Should().Be("Fake University");
        result.Data.ReportedInstituteAddress.Should().Be("123 Fake Street");
        result.Data.Description.Should().Be("This is a description with whitespace.");
    }

    [Fact]
    public async Task CreateReportAsync_ReturnsError_WhenStudentNotFound()
    {
        // Arrange
        var unknownUserId = Guid.NewGuid();
        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "Fake University",
            Description = "This is a test description."
        };

        // Act
        var result = await _service.CreateReportAsync(unknownUserId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Student profile not found");
    }

    [Fact]
    public async Task CreateReportAsync_ReturnsError_WhenRateLimitExceeded()
    {
        // Arrange - Create 5 reports today
        for (int i = 0; i < 5; i++)
        {
            _context.FraudReports.Add(new FraudReport
            {
                StudentId = _testStudentId,
                ReportedInstituteName = $"Institute {i}",
                Description = "Test description for rate limit",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "One More Institute",
            Description = "This should be rejected due to rate limit."
        };

        // Act
        var result = await _service.CreateReportAsync(_testUserId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("limit");
        result.Errors.Should().Contain(e => e.Contains("5 reports per day"));
    }

    [Fact]
    public async Task CreateReportAsync_AllowsReports_FromDifferentDays()
    {
        // Arrange - Create 5 reports from yesterday
        for (int i = 0; i < 5; i++)
        {
            _context.FraudReports.Add(new FraudReport
            {
                StudentId = _testStudentId,
                ReportedInstituteName = $"Institute {i}",
                Description = "Test description",
                CreatedAt = DateTime.UtcNow.AddDays(-1) // Yesterday
            });
        }
        await _context.SaveChangesAsync();

        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "Today's Institute",
            Description = "This should be allowed since yesterday's limit doesn't affect today."
        };

        // Act
        var result = await _service.CreateReportAsync(_testUserId, request);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region GetUserReportsAsync Tests

    [Fact]
    public async Task GetUserReportsAsync_ReturnsEmptyList_WhenNoReports()
    {
        // Act
        var result = await _service.GetUserReportsAsync(_testUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Reports.Should().BeEmpty();
        result.Data.Pagination.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUserReportsAsync_ReturnsReports_SortedByMostRecentFirst()
    {
        // Arrange
        _context.FraudReports.AddRange(new[]
        {
            new FraudReport
            {
                StudentId = _testStudentId,
                ReportedInstituteName = "First Report",
                Description = "Description 1",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new FraudReport
            {
                StudentId = _testStudentId,
                ReportedInstituteName = "Second Report",
                Description = "Description 2",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new FraudReport
            {
                StudentId = _testStudentId,
                ReportedInstituteName = "Third Report",
                Description = "Description 3",
                CreatedAt = DateTime.UtcNow
            }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserReportsAsync(_testUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Reports.Should().HaveCount(3);
        result.Data.Reports[0].ReportedInstituteName.Should().Be("Third Report");
        result.Data.Reports[1].ReportedInstituteName.Should().Be("Second Report");
        result.Data.Reports[2].ReportedInstituteName.Should().Be("First Report");
    }

    [Fact]
    public async Task GetUserReportsAsync_ReturnsPaginatedResults()
    {
        // Arrange - Add 5 reports
        for (int i = 1; i <= 5; i++)
        {
            _context.FraudReports.Add(new FraudReport
            {
                StudentId = _testStudentId,
                ReportedInstituteName = $"Institute {i}",
                Description = $"Description {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserReportsAsync(_testUserId, page: 1, pageSize: 2);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Reports.Should().HaveCount(2);
        result.Data.Pagination.CurrentPage.Should().Be(1);
        result.Data.Pagination.PageSize.Should().Be(2);
        result.Data.Pagination.TotalCount.Should().Be(5);
        result.Data.Pagination.TotalPages.Should().Be(3);
        result.Data.Pagination.HasNextPage.Should().BeTrue();
        result.Data.Pagination.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserReportsAsync_ReturnsError_WhenStudentNotFound()
    {
        // Arrange
        var unknownUserId = Guid.NewGuid();

        // Act
        var result = await _service.GetUserReportsAsync(unknownUserId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Student profile not found");
    }

    #endregion

    #region GetReportByIdAsync Tests

    [Fact]
    public async Task GetReportByIdAsync_ReturnsReport_WhenOwnedByUser()
    {
        // Arrange
        var report = new FraudReport
        {
            StudentId = _testStudentId,
            ReportedInstituteName = "Test Institute",
            Description = "Test description",
            CreatedAt = DateTime.UtcNow
        };
        _context.FraudReports.Add(report);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetReportByIdAsync(_testUserId, report.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ReportedInstituteName.Should().Be("Test Institute");
    }

    [Fact]
    public async Task GetReportByIdAsync_ReturnsError_WhenReportNotFound()
    {
        // Act
        var result = await _service.GetReportByIdAsync(_testUserId, Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task GetReportByIdAsync_ReturnsError_WhenStudentNotFound()
    {
        // Arrange
        var unknownUserId = Guid.NewGuid();

        // Act
        var result = await _service.GetReportByIdAsync(unknownUserId, Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Student profile not found");
    }

    #endregion

    #region IDOR Prevention Tests

    [Fact]
    public async Task GetUserReportsAsync_OnlyReturnsCurrentUsersReports()
    {
        // Arrange - Add reports for different users
        _context.FraudReports.Add(new FraudReport
        {
            StudentId = _testStudentId,
            ReportedInstituteName = "My Report",
            Description = "My description",
            CreatedAt = DateTime.UtcNow
        });
        _context.FraudReports.Add(new FraudReport
        {
            StudentId = _otherStudentId,
            ReportedInstituteName = "Other User's Report",
            Description = "Other description",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserReportsAsync(_testUserId);

        // Assert
        result.Data!.Reports.Should().HaveCount(1);
        result.Data.Reports[0].ReportedInstituteName.Should().Be("My Report");
        result.Data.Reports.Should().NotContain(r => r.ReportedInstituteName == "Other User's Report");
    }

    [Fact]
    public async Task GetReportByIdAsync_CannotAccessOtherUsersReport()
    {
        // Arrange - Add report for other user
        var report = new FraudReport
        {
            StudentId = _otherStudentId,
            ReportedInstituteName = "Other User's Report",
            Description = "Other description",
            CreatedAt = DateTime.UtcNow
        };
        _context.FraudReports.Add(report);
        await _context.SaveChangesAsync();

        // Act - Try to access other user's report
        var result = await _service.GetReportByIdAsync(_testUserId, report.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    #endregion

    #region Pagination Edge Cases

    [Fact]
    public async Task GetUserReportsAsync_ClampsPageSizeToMax50()
    {
        // Arrange
        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "Test",
            Description = "Test description for pagination"
        };
        await _service.CreateReportAsync(_testUserId, request);

        // Act
        var result = await _service.GetUserReportsAsync(_testUserId, page: 1, pageSize: 100);

        // Assert
        result.Data!.Pagination.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task GetUserReportsAsync_HandlesNegativePageNumber()
    {
        // Arrange
        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "Test",
            Description = "Test description for pagination"
        };
        await _service.CreateReportAsync(_testUserId, request);

        // Act
        var result = await _service.GetUserReportsAsync(_testUserId, page: -5, pageSize: 10);

        // Assert
        result.Data!.Pagination.CurrentPage.Should().Be(1);
    }

    #endregion
}