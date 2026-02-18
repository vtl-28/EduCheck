using EduCheck.API.Controllers;
using EduCheck.Application.DTOs.Institute;
using EduCheck.Application.DTOs.SearchHistory;
using EduCheck.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace EduCheck.Tests.Controllers;

public class SearchHistoryControllerTests
{
    private readonly Mock<ISearchHistoryService> _searchHistoryServiceMock;
    private readonly Mock<ILogger<SearchHistoryController>> _loggerMock;
    private readonly SearchHistoryController _controller;
    private readonly Guid _userId;

    public SearchHistoryControllerTests()
    {
        _searchHistoryServiceMock = new Mock<ISearchHistoryService>();
        _loggerMock = new Mock<ILogger<SearchHistoryController>>();
        _controller = new SearchHistoryController(_searchHistoryServiceMock.Object, _loggerMock.Object);

        _userId = Guid.NewGuid();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString())
        }, "Test"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };
    }

    private void SetupUnauthenticatedUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    #region GetSearchHistory Tests

    [Fact]
    public async Task GetSearchHistory_Authenticated_ReturnsOk()
    {
        var response = new SearchHistoryResponse
        {
            Success = true,
            Message = "2 search history entries found",
            Data = new SearchHistoryData
            {
                History = new List<SearchHistoryDto>
                {
                    new()
                    {
                        Id = 1,
                        SearchedAt = DateTime.UtcNow,
                        Institute = new InstituteDto { Id = 1, InstitutionName = "Test University" }
                    },
                    new()
                    {
                        Id = 2,
                        SearchedAt = DateTime.UtcNow,
                        Institute = new InstituteDto { Id = 2, InstitutionName = "Test College" }
                    }
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

        _searchHistoryServiceMock
            .Setup(x => x.GetUserSearchHistoryAsync(_userId, 1, 10))
            .ReturnsAsync(response);


        var result = await _controller.GetSearchHistory();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<SearchHistoryResponse>().Subject;
        returnedResponse.Success.Should().BeTrue();
        returnedResponse.Data!.History.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSearchHistory_Unauthenticated_ReturnsUnauthorized()
    {

        SetupUnauthenticatedUser();


        var result = await _controller.GetSearchHistory();

        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeOfType<SearchHistoryResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("User not authenticated");
    }

    [Fact]
    public async Task GetSearchHistory_WithPagination_PassesParametersToService()
    {

        var response = new SearchHistoryResponse
        {
            Success = true,
            Message = "Found",
            Data = new SearchHistoryData
            {
                History = new List<SearchHistoryDto>(),
                Pagination = new PaginationDto { CurrentPage = 2, PageSize = 20 }
            }
        };

        _searchHistoryServiceMock
            .Setup(x => x.GetUserSearchHistoryAsync(_userId, 2, 20))
            .ReturnsAsync(response);

        var result = await _controller.GetSearchHistory(page: 2, pageSize: 20);

        result.Should().BeOfType<OkObjectResult>();
        _searchHistoryServiceMock.Verify(x => x.GetUserSearchHistoryAsync(_userId, 2, 20), Times.Once);
    }

    [Fact]
    public async Task GetSearchHistory_EmptyHistory_ReturnsOkWithEmptyList()
    {
        var response = new SearchHistoryResponse
        {
            Success = true,
            Message = "No search history found",
            Data = new SearchHistoryData
            {
                History = new List<SearchHistoryDto>(),
                Pagination = new PaginationDto { TotalCount = 0 }
            }
        };

        _searchHistoryServiceMock
            .Setup(x => x.GetUserSearchHistoryAsync(_userId, 1, 10))
            .ReturnsAsync(response);


        var result = await _controller.GetSearchHistory();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<SearchHistoryResponse>().Subject;
        returnedResponse.Success.Should().BeTrue();
        returnedResponse.Data!.History.Should().BeEmpty();
    }

    #endregion

    #region DeleteSearchHistoryEntry Tests

    [Fact]
    public async Task DeleteSearchHistoryEntry_ValidEntry_ReturnsOk()
    {
        var response = new DeleteSearchHistoryResponse
        {
            Success = true,
            Message = "Search history entry deleted successfully",
            DeletedCount = 1
        };

        _searchHistoryServiceMock
            .Setup(x => x.DeleteSearchHistoryEntryAsync(_userId, 1))
            .ReturnsAsync(response);

        var result = await _controller.DeleteSearchHistoryEntry(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<DeleteSearchHistoryResponse>().Subject;
        returnedResponse.Success.Should().BeTrue();
        returnedResponse.DeletedCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteSearchHistoryEntry_Unauthenticated_ReturnsUnauthorized()
    {
        SetupUnauthenticatedUser();

        var result = await _controller.DeleteSearchHistoryEntry(1);


        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeOfType<DeleteSearchHistoryResponse>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSearchHistoryEntry_InvalidId_ReturnsBadRequest()
    {
        var result = await _controller.DeleteSearchHistoryEntry(0);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<DeleteSearchHistoryResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Errors.Should().Contain("History entry ID must be a positive number");
    }

    [Fact]
    public async Task DeleteSearchHistoryEntry_NegativeId_ReturnsBadRequest()
    {
        var result = await _controller.DeleteSearchHistoryEntry(-1);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<DeleteSearchHistoryResponse>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSearchHistoryEntry_NotFound_ReturnsNotFound()
    {
        var response = new DeleteSearchHistoryResponse
        {
            Success = false,
            Message = "Search history entry not found",
            DeletedCount = 0,
            Errors = new List<string> { "The specified search history entry was not found or does not belong to you" }
        };

        _searchHistoryServiceMock
            .Setup(x => x.DeleteSearchHistoryEntryAsync(_userId, 999))
            .ReturnsAsync(response);


        var result = await _controller.DeleteSearchHistoryEntry(999);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var returnedResponse = notFoundResult.Value.Should().BeOfType<DeleteSearchHistoryResponse>().Subject;
        returnedResponse.Success.Should().BeFalse();
    }

    #endregion

    #region ClearSearchHistory Tests

    [Fact]
    public async Task ClearSearchHistory_WithHistory_ReturnsOk()
    {
        var response = new DeleteSearchHistoryResponse
        {
            Success = true,
            Message = "Successfully cleared 5 search history entries",
            DeletedCount = 5
        };

        _searchHistoryServiceMock
            .Setup(x => x.ClearUserSearchHistoryAsync(_userId))
            .ReturnsAsync(response);


        var result = await _controller.ClearSearchHistory();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<DeleteSearchHistoryResponse>().Subject;
        returnedResponse.Success.Should().BeTrue();
        returnedResponse.DeletedCount.Should().Be(5);
    }

    [Fact]
    public async Task ClearSearchHistory_Unauthenticated_ReturnsUnauthorized()
    {

        SetupUnauthenticatedUser();

        var result = await _controller.ClearSearchHistory();

        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeOfType<DeleteSearchHistoryResponse>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ClearSearchHistory_NoHistory_ReturnsOkWithZeroDeleted()
    {

        var response = new DeleteSearchHistoryResponse
        {
            Success = true,
            Message = "No search history to clear",
            DeletedCount = 0
        };

        _searchHistoryServiceMock
            .Setup(x => x.ClearUserSearchHistoryAsync(_userId))
            .ReturnsAsync(response);

        var result = await _controller.ClearSearchHistory();


        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<DeleteSearchHistoryResponse>().Subject;
        returnedResponse.Success.Should().BeTrue();
        returnedResponse.DeletedCount.Should().Be(0);
    }

    #endregion
}