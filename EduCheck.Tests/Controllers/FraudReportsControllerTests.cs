using EduCheck.API.Controllers;
using EduCheck.Application.DTOs.FraudReport;
using EduCheck.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Drawing;
using System.Security.Claims;
using Xunit;

namespace EduCheck.Tests.Controllers;

public class FraudReportsControllerTests
{
    private readonly Mock<IFraudReportService> _serviceMock;
    private readonly Mock<ILogger<FraudReportsController>> _loggerMock;
    private readonly FraudReportsController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public FraudReportsControllerTests()
    {
        _serviceMock = new Mock<IFraudReportService>();
        _loggerMock = new Mock<ILogger<FraudReportsController>>();
        _controller = new FraudReportsController(_serviceMock.Object, _loggerMock.Object);

        SetupAuthenticatedUser(_testUserId);
    }

    private void SetupAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private void SetupUnauthenticatedUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    #region CreateReport Tests

    [Fact]
    public async Task CreateReport_ReturnsCreated_WhenSuccessful()
    {
        // Arrange
        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "Fake University",
            Description = "This is a fraudulent institute."
        };

        var reportId = Guid.NewGuid();

        var response = new CreateFraudReportResponse
        {
            Success = true,
            Message = "Report submitted",
            Data = new FraudReportDto { Id = reportId, ReportedInstituteName = "Fake University" }
        };

        _serviceMock.Setup(s => s.CreateReportAsync(_testUserId, request))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CreateReport(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        createdResult.Location.Should().Be($"/api/fraud-reports/{reportId}");
    }

    [Fact]
    public async Task CreateReport_ReturnsUnauthorized_WhenNoUserInToken()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "Fake University",
            Description = "This is a fraudulent institute."
        };

        // Act
        var result = await _controller.CreateReport(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task CreateReport_ReturnsTooManyRequests_WhenRateLimitExceeded()
    {
        // Arrange
        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "Fake University",
            Description = "This is a fraudulent institute."
        };

        var response = new CreateFraudReportResponse
        {
            Success = false,
            Message = "Daily report limit reached",
            Errors = new List<string> { "You can only submit 5 reports per day." }
        };

        _serviceMock.Setup(s => s.CreateReportAsync(_testUserId, request))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CreateReport(request);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public async Task CreateReport_ReturnsBadRequest_WhenServiceFails()
    {
        // Arrange
        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "Fake University",
            Description = "This is a fraudulent institute."
        };

        var response = new CreateFraudReportResponse
        {
            Success = false,
            Message = "Student profile not found",
            Errors = new List<string> { "Error" }
        };

        _serviceMock.Setup(s => s.CreateReportAsync(_testUserId, request))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CreateReport(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetUserReports Tests

    [Fact]
    public async Task GetUserReports_ReturnsOk_WithReports()
    {
        // Arrange
        var reportId = Guid.NewGuid();

        var response = new FraudReportsResponse
        {
            Success = true,
            Message = "1 report(s) found",
            Data = new FraudReportsData
            {
                Reports = new List<FraudReportDto>
                {
                    new() { Id = reportId, ReportedInstituteName = "Fake University" }
                },
                Pagination = new FraudReportPaginationDto { TotalCount = 1 }
            }
        };

        _serviceMock.Setup(s => s.GetUserReportsAsync(_testUserId, 1, 10))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetUserReports();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<FraudReportsResponse>().Subject;
        returnedResponse.Data!.Reports.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserReports_ReturnsUnauthorized_WhenNoUserInToken()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetUserReports();

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetUserReports_PassesPaginationParameters()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetUserReportsAsync(_testUserId, 2, 25))
            .ReturnsAsync(new FraudReportsResponse
            {
                Success = true,
                Data = new FraudReportsData
                {
                    Reports = new List<FraudReportDto>(),
                    Pagination = new FraudReportPaginationDto { CurrentPage = 2, PageSize = 25 }
                }
            });

        // Act
        var result = await _controller.GetUserReports(page: 2, pageSize: 25);

        // Assert
        _serviceMock.Verify(s => s.GetUserReportsAsync(_testUserId, 2, 25), Times.Once);
    }

    #endregion

    #region GetReportById Tests

    [Fact]
    public async Task GetReportById_ReturnsOk_WhenFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();

        var response = new FraudReportResponse
        {
            Success = true,
            Data = new FraudReportDto { Id = reportId, ReportedInstituteName = "Fake University" }
        };

        _serviceMock.Setup(s => s.GetReportByIdAsync(_testUserId, reportId))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetReportById(reportId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<FraudReportResponse>().Subject;
        returnedResponse.Data!.Id.Should().Be(reportId);
    }

    [Fact]
    public async Task GetReportById_ReturnsNotFound_WhenNotFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();

        var response = new FraudReportResponse
        {
            Success = false,
            Message = "Report not found"
        };

        _serviceMock.Setup(s => s.GetReportByIdAsync(_testUserId, reportId))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetReportById(reportId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetReportById_ReturnsBadRequest_WhenIdIsInvalid()
    {
        // Act
        var result = await _controller.GetReportById(Guid.Empty);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetReportById_ReturnsUnauthorized_WhenNoUserInToken()
    {
        // Arrange
        SetupUnauthenticatedUser();

        var reportId = Guid.NewGuid();

        // Act
        var result = await _controller.GetReportById(reportId);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region IDOR Prevention Tests

    [Fact]
    public async Task Controller_AlwaysUsesUserIdFromToken()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetUserReportsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new FraudReportsResponse { Success = true, Data = new FraudReportsData() });

        // Act
        await _controller.GetUserReports();

        // Assert
        _serviceMock.Verify(s => s.GetUserReportsAsync(_testUserId, It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    #endregion
}