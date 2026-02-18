using EduCheck.API.Controllers;
using EduCheck.Application.DTOs.Admin;
using EduCheck.Application.Interfaces;
using EduCheck.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Drawing;
using System.Security.Claims;
using Xunit;

namespace EduCheck.Tests.Controllers;

public class AdminFraudReportsControllerTests
{
    private readonly Mock<IAdminFraudReportService> _serviceMock;
    private readonly Mock<ILogger<AdminFraudReportsController>> _loggerMock;
    private readonly AdminFraudReportsController _controller;

    public AdminFraudReportsControllerTests()
    {
        _serviceMock = new Mock<IAdminFraudReportService>();
        _loggerMock = new Mock<ILogger<AdminFraudReportsController>>();
        _controller = new AdminFraudReportsController(_serviceMock.Object, _loggerMock.Object);

        SetupAdminUser();
    }

    private void SetupAdminUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "admin@example.com"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #region GetAllReports Tests

    [Fact]
    public async Task GetAllReports_ReturnsOk_WithReports()
    {
        // Arrange
        var response = new AdminFraudReportsResponse
        {
            Success = true,
            Message = "2 report(s) found",
            Data = new AdminFraudReportsData
            {
                Reports = new List<AdminFraudReportDto>
                {
                    new() { Id = Guid.NewGuid(), ReportedInstituteName = "Fake University A" },
                    new() { Id = Guid.NewGuid(), ReportedInstituteName = "Fake University B" }
                },
                Pagination = new AdminPaginationDto { TotalCount = 2 }
            }
        };

        _serviceMock.Setup(s => s.GetAllReportsAsync(It.IsAny<AdminFraudReportFilterRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetAllReports();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<AdminFraudReportsResponse>().Subject;
        returnedResponse.Data!.Reports.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllReports_PassesFiltersToService()
    {
        // Arrange
        AdminFraudReportFilterRequest? capturedFilter = null;

        _serviceMock.Setup(s => s.GetAllReportsAsync(It.IsAny<AdminFraudReportFilterRequest>()))
            .Callback<AdminFraudReportFilterRequest>(f => capturedFilter = f)
            .ReturnsAsync(new AdminFraudReportsResponse { Success = true, Data = new AdminFraudReportsData() });

        // Act
        await _controller.GetAllReports(
            status: FraudReportStatus.Submitted,
            severity: FraudSeverity.High,
            province: "Gauteng",
            page: 2,
            pageSize: 25);

        // Assert
        capturedFilter.Should().NotBeNull();
        capturedFilter!.Status.Should().Be(FraudReportStatus.Submitted);
        capturedFilter.Severity.Should().Be(FraudSeverity.High);
        capturedFilter.Province.Should().Be("Gauteng");
        capturedFilter.Page.Should().Be(2);
        capturedFilter.PageSize.Should().Be(25);
    }

    #endregion

    #region GetReportById Tests

    [Fact]
    public async Task GetReportById_ReturnsOk_WhenFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var response = new AdminFraudReportResponse
        {
            Success = true,
            Data = new AdminFraudReportDto { Id = reportId, ReportedInstituteName = "Fake University" }
        };

        _serviceMock.Setup(s => s.GetReportByIdAsync(reportId))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetReportById(reportId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<AdminFraudReportResponse>().Subject;
        returnedResponse.Data!.Id.Should().Be(reportId);
    }

    [Fact]
    public async Task GetReportById_ReturnsNotFound_WhenNotFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var response = new AdminFraudReportResponse
        {
            Success = false,
            Message = "Report not found"
        };

        _serviceMock.Setup(s => s.GetReportByIdAsync(reportId))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetReportById(reportId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetReportById_ReturnsBadRequest_WhenIdIsEmpty()
    {
        // Act
        var result = await _controller.GetReportById(Guid.Empty);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetStatistics Tests

    [Fact]
    public async Task GetStatistics_ReturnsOk_WithStatistics()
    {
        // Arrange
        var response = new FraudReportStatisticsResponse
        {
            Success = true,
            Data = new FraudReportStatisticsDto
            {
                TotalReports = 100,
                SubmittedCount = 50,
                UnderReviewCount = 20,
                VerifiedCount = 15,
                DismissedCount = 10,
                ActionTakenCount = 5
            }
        };

        _serviceMock.Setup(s => s.GetStatisticsAsync())
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<FraudReportStatisticsResponse>().Subject;
        returnedResponse.Data!.TotalReports.Should().Be(100);
    }

    #endregion
}