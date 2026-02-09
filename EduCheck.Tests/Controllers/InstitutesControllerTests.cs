using EduCheck.API.Controllers;
using EduCheck.Application.DTOs.Institute;
using EduCheck.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace EduCheck.Tests.Controllers;

public class InstitutesControllerTests
{
    private readonly Mock<IInstituteService> _instituteServiceMock;
    private readonly Mock<ILogger<InstitutesController>> _loggerMock;
    private readonly InstitutesController _controller;

    public InstitutesControllerTests()
    {
        _instituteServiceMock = new Mock<IInstituteService>();
        _loggerMock = new Mock<ILogger<InstitutesController>>();
        _controller = new InstitutesController(_instituteServiceMock.Object, _loggerMock.Object);

        var userId = Guid.NewGuid();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "Test"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };
    }

    #region Search Tests

    [Fact]
    public async Task Search_ValidQuery_ReturnsOk()
    {

        var response = new InstituteSearchResponse
        {
            Success = true,
            Message = "2 institute(s) found",
            Data = new InstituteSearchData
            {
                Institutes = new List<InstituteDto>
                {
                    new() { Id = 1, InstitutionName = "Test University" },
                    new() { Id = 2, InstitutionName = "Test College" }
                },
                Pagination = new PaginationDto
                {
                    CurrentPage = 1,
                    PageSize = 10,
                    TotalCount = 2,
                    TotalPages = 1
                }
            }
        };

        _instituteServiceMock
            .Setup(x => x.SearchInstitutesAsync(It.IsAny<InstituteSearchRequest>(), It.IsAny<Guid?>()))
            .ReturnsAsync(response);

        var result = await _controller.Search("Test");


        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<InstituteSearchResponse>().Subject;
        returnedResponse.Success.Should().BeTrue();
        returnedResponse.Data!.Institutes.Should().HaveCount(2);
    }

    [Fact]
    public async Task Search_EmptyQuery_ReturnsBadRequest()
    {
        var result = await _controller.Search("");

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<InstituteSearchResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Errors.Should().Contain("Please provide a search query");
    }

    [Fact]
    public async Task Search_NullQuery_ReturnsBadRequest()
    {

        var result = await _controller.Search(null!);


        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<InstituteSearchResponse>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Search_QueryTooShort_ReturnsBadRequest()
    {

        var result = await _controller.Search("A");


        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<InstituteSearchResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Errors.Should().Contain("Search query must be at least 2 characters");
    }

    [Fact]
    public async Task Search_QueryTooLong_ReturnsBadRequest()
    {

        var longQuery = new string('A', 256);

        var result = await _controller.Search(longQuery);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<InstituteSearchResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Errors.Should().Contain("Search query cannot exceed 255 characters");
    }

    [Fact]
    public async Task Search_WithProvinceFilter_PassesFilterToService()
    {

        var response = new InstituteSearchResponse
        {
            Success = true,
            Message = "1 institute(s) found",
            Data = new InstituteSearchData
            {
                Institutes = new List<InstituteDto>
                {
                    new() { Id = 1, InstitutionName = "Test University", Province = "Gauteng" }
                },
                Pagination = new PaginationDto()
            }
        };

        _instituteServiceMock
            .Setup(x => x.SearchInstitutesAsync(
                It.Is<InstituteSearchRequest>(r => r.Province == "Gauteng"),
                It.IsAny<Guid?>()))
            .ReturnsAsync(response);

        var result = await _controller.Search("Test", "Gauteng");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _instituteServiceMock.Verify(x => x.SearchInstitutesAsync(
            It.Is<InstituteSearchRequest>(r => r.Province == "Gauteng"),
            It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task Search_WithPagination_PassesPaginationToService()
    {

        var response = new InstituteSearchResponse
        {
            Success = true,
            Message = "Found",
            Data = new InstituteSearchData
            {
                Institutes = new List<InstituteDto>(),
                Pagination = new PaginationDto
                {
                    CurrentPage = 2,
                    PageSize = 20
                }
            }
        };

        _instituteServiceMock
            .Setup(x => x.SearchInstitutesAsync(
                It.Is<InstituteSearchRequest>(r => r.Page == 2 && r.PageSize == 20),
                It.IsAny<Guid?>()))
            .ReturnsAsync(response);


        var result = await _controller.Search("Test", null, 2, 20);

        result.Should().BeOfType<OkObjectResult>();
        _instituteServiceMock.Verify(x => x.SearchInstitutesAsync(
            It.Is<InstituteSearchRequest>(r => r.Page == 2 && r.PageSize == 20),
            It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task Search_PageSizeExceedsMax_ClampedTo50()
    {

        var response = new InstituteSearchResponse
        {
            Success = true,
            Message = "Found",
            Data = new InstituteSearchData
            {
                Institutes = new List<InstituteDto>(),
                Pagination = new PaginationDto()
            }
        };

        _instituteServiceMock
            .Setup(x => x.SearchInstitutesAsync(
                It.Is<InstituteSearchRequest>(r => r.PageSize == 50),
                It.IsAny<Guid?>()))
            .ReturnsAsync(response);

        var result = await _controller.Search("Test", null, 1, 100);

        result.Should().BeOfType<OkObjectResult>();
        _instituteServiceMock.Verify(x => x.SearchInstitutesAsync(
            It.Is<InstituteSearchRequest>(r => r.PageSize == 50),
            It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task Search_NegativePage_DefaultsTo1()
    {

        var response = new InstituteSearchResponse
        {
            Success = true,
            Message = "Found",
            Data = new InstituteSearchData
            {
                Institutes = new List<InstituteDto>(),
                Pagination = new PaginationDto()
            }
        };

        _instituteServiceMock
            .Setup(x => x.SearchInstitutesAsync(
                It.Is<InstituteSearchRequest>(r => r.Page == 1),
                It.IsAny<Guid?>()))
            .ReturnsAsync(response);


        var result = await _controller.Search("Test", null, -5, 10);


        result.Should().BeOfType<OkObjectResult>();
        _instituteServiceMock.Verify(x => x.SearchInstitutesAsync(
            It.Is<InstituteSearchRequest>(r => r.Page == 1),
            It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task Search_NoResults_ReturnsOkWithEmptyList()
    {

        var response = new InstituteSearchResponse
        {
            Success = true,
            Message = "No institutes found matching your search",
            Data = new InstituteSearchData
            {
                Institutes = new List<InstituteDto>(),
                Pagination = new PaginationDto { TotalCount = 0 },
                Suggestions = new SearchSuggestions
                {
                    ReportFraud = true,
                    FindNearbyInstitutes = true
                }
            }
        };

        _instituteServiceMock
            .Setup(x => x.SearchInstitutesAsync(It.IsAny<InstituteSearchRequest>(), It.IsAny<Guid?>()))
            .ReturnsAsync(response);


        var result = await _controller.Search("NonExistent");


        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<InstituteSearchResponse>().Subject;
        returnedResponse.Success.Should().BeTrue();
        returnedResponse.Data!.Institutes.Should().BeEmpty();
        returnedResponse.Data.Suggestions.Should().NotBeNull();
        returnedResponse.Data.Suggestions!.ReportFraud.Should().BeTrue();
    }

    [Fact]
    public async Task Search_PassesUserIdToService()
    {

        var response = new InstituteSearchResponse
        {
            Success = true,
            Message = "Found",
            Data = new InstituteSearchData
            {
                Institutes = new List<InstituteDto>(),
                Pagination = new PaginationDto()
            }
        };

        _instituteServiceMock
            .Setup(x => x.SearchInstitutesAsync(
                It.IsAny<InstituteSearchRequest>(),
                It.Is<Guid?>(id => id.HasValue)))
            .ReturnsAsync(response);

        var result = await _controller.Search("Test");

        result.Should().BeOfType<OkObjectResult>();
        _instituteServiceMock.Verify(x => x.SearchInstitutesAsync(
            It.IsAny<InstituteSearchRequest>(),
            It.Is<Guid?>(id => id.HasValue)), Times.Once);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ValidId_ReturnsOk()
    {

        var response = new InstituteDetailResponse
        {
            Success = true,
            Message = "Institute found",
            Data = new InstituteDto
            {
                Id = 1,
                InstitutionName = "Test University",
                AccreditationNumber = "16 TEST 00001",
                IsAccredited = true
            }
        };

        _instituteServiceMock
            .Setup(x => x.GetInstituteByIdAsync(1))
            .ReturnsAsync(response);


        var result = await _controller.GetById(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<InstituteDetailResponse>().Subject;
        returnedResponse.Success.Should().BeTrue();
        returnedResponse.Data!.Id.Should().Be(1);
        returnedResponse.Data.InstitutionName.Should().Be("Test University");
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsBadRequest()
    {

        var result = await _controller.GetById(0);


        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<InstituteDetailResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Errors.Should().Contain("Institute ID must be a positive number");
    }

    [Fact]
    public async Task GetById_NegativeId_ReturnsBadRequest()
    {
        var result = await _controller.GetById(-1);


        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<InstituteDetailResponse>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {

        var response = new InstituteDetailResponse
        {
            Success = false,
            Message = "Institute not found",
            Errors = new List<string> { "No institute found with ID 999" }
        };

        _instituteServiceMock
            .Setup(x => x.GetInstituteByIdAsync(999))
            .ReturnsAsync(response);

        var result = await _controller.GetById(999);


        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var returnedResponse = notFoundResult.Value.Should().BeOfType<InstituteDetailResponse>().Subject;
        returnedResponse.Success.Should().BeFalse();
        returnedResponse.Message.Should().Be("Institute not found");
    }

    #endregion
}