using EduCheck.API.Controllers;
using EduCheck.Application.DTOs.Favorites;
using EduCheck.Application.DTOs.Institute;
using EduCheck.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace EduCheck.Tests.Controllers;

public class FavoritesControllerTests
{
    private readonly Mock<IFavoritesService> _serviceMock;
    private readonly Mock<ILogger<FavoritesController>> _loggerMock;
    private readonly FavoritesController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public FavoritesControllerTests()
    {
        _serviceMock = new Mock<IFavoritesService>();
        _loggerMock = new Mock<ILogger<FavoritesController>>();
        _controller = new FavoritesController(_serviceMock.Object, _loggerMock.Object);

        // Setup default authenticated user
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

    #region GetFavorites Tests

    [Fact]
    public async Task GetFavorites_ReturnsOk_WithFavorites()
    {
        // Arrange
        var response = new FavoritesResponse
        {
            Success = true,
            Message = "1 favorite(s) found",
            Data = new FavoritesData
            {
                Favorites = new List<FavoriteInstituteDto>
                {
                    new() { Id = 1, Institute = new InstituteDto { Id = 1, InstitutionName = "Test" } }
                },
                Pagination = new PaginationDto { CurrentPage = 1, TotalCount = 1 }
            }
        };

        _serviceMock.Setup(s => s.GetUserFavoritesAsync(_testUserId, 1, 10))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetFavorites();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<FavoritesResponse>().Subject;
        returnedResponse.Success.Should().BeTrue();
        returnedResponse.Data!.Favorites.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFavorites_ReturnsUnauthorized_WhenNoUserInToken()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.GetFavorites();

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeOfType<FavoritesResponse>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetFavorites_PassesPaginationParameters()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetUserFavoritesAsync(_testUserId, 2, 25))
            .ReturnsAsync(new FavoritesResponse
            {
                Success = true,
                Data = new FavoritesData
                {
                    Favorites = new List<FavoriteInstituteDto>(),
                    Pagination = new PaginationDto { CurrentPage = 2, PageSize = 25 }
                }
            });

        // Act
        var result = await _controller.GetFavorites(page: 2, pageSize: 25);

        // Assert
        _serviceMock.Verify(s => s.GetUserFavoritesAsync(_testUserId, 2, 25), Times.Once);
    }

    #endregion

    #region AddFavorite Tests

    [Fact]
    public async Task AddFavorite_ReturnsCreated_WhenSuccessful()
    {
        // Arrange
        var response = new AddFavoriteResponse
        {
            Success = true,
            Message = "Institute added to favorites",
            Data = new FavoriteInstituteDto { Id = 1, Institute = new InstituteDto { Id = 1 } }
        };

        _serviceMock.Setup(s => s.AddFavoriteAsync(_testUserId, 1))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.AddFavorite(1);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(FavoritesController.GetFavoriteStatus));
    }

    [Fact]
    public async Task AddFavorite_ReturnsBadRequest_WhenInstituteIdIsInvalid()
    {
        // Act
        var result = await _controller.AddFavorite(0);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<AddFavoriteResponse>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AddFavorite_ReturnsNotFound_WhenInstituteDoesNotExist()
    {
        // Arrange
        _serviceMock.Setup(s => s.AddFavoriteAsync(_testUserId, 999))
            .ReturnsAsync(new AddFavoriteResponse
            {
                Success = false,
                Message = "Institute not found"
            });

        // Act
        var result = await _controller.AddFavorite(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddFavorite_ReturnsConflict_WhenAlreadyFavorited()
    {
        // Arrange
        _serviceMock.Setup(s => s.AddFavoriteAsync(_testUserId, 1))
            .ReturnsAsync(new AddFavoriteResponse
            {
                Success = false,
                Message = "Institute is already in favorites"
            });

        // Act
        var result = await _controller.AddFavorite(1);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task AddFavorite_ReturnsUnauthorized_WhenNoUserInToken()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.AddFavorite(1);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region RemoveFavorite Tests

    [Fact]
    public async Task RemoveFavorite_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        _serviceMock.Setup(s => s.RemoveFavoriteAsync(_testUserId, 1))
            .ReturnsAsync(new RemoveFavoriteResponse { Success = true });

        // Act
        var result = await _controller.RemoveFavorite(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RemoveFavorite_ReturnsNotFound_WhenNotFavorited()
    {
        // Arrange
        _serviceMock.Setup(s => s.RemoveFavoriteAsync(_testUserId, 1))
            .ReturnsAsync(new RemoveFavoriteResponse
            {
                Success = false,
                Message = "Favorite not found"
            });

        // Act
        var result = await _controller.RemoveFavorite(1);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RemoveFavorite_ReturnsBadRequest_WhenInstituteIdIsInvalid()
    {
        // Act
        var result = await _controller.RemoveFavorite(-1);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetFavoriteStatus Tests

    [Fact]
    public async Task GetFavoriteStatus_ReturnsOk_WhenFavorited()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetFavoriteStatusAsync(_testUserId, 1))
            .ReturnsAsync(new FavoriteStatusResponse
            {
                Success = true,
                Data = new FavoriteStatusDto { IsFavorited = true, FavoriteId = 1 }
            });

        // Act
        var result = await _controller.GetFavoriteStatus(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<FavoriteStatusResponse>().Subject;
        response.Data!.IsFavorited.Should().BeTrue();
    }

    [Fact]
    public async Task GetFavoriteStatus_ReturnsOk_WhenNotFavorited()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetFavoriteStatusAsync(_testUserId, 1))
            .ReturnsAsync(new FavoriteStatusResponse
            {
                Success = true,
                Data = new FavoriteStatusDto { IsFavorited = false }
            });

        // Act
        var result = await _controller.GetFavoriteStatus(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<FavoriteStatusResponse>().Subject;
        response.Data!.IsFavorited.Should().BeFalse();
    }

    #endregion

    #region IDOR Prevention Tests

    [Fact]
    public async Task Controller_AlwaysUsesUserIdFromToken_NeverFromRequest()
    {
        // Arrange
        var differentUserId = Guid.NewGuid();

        _serviceMock.Setup(s => s.GetUserFavoritesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new FavoritesResponse { Success = true, Data = new FavoritesData() });

        // Act
        await _controller.GetFavorites();

        // Assert - Verify service was called with the user ID from token
        _serviceMock.Verify(s => s.GetUserFavoritesAsync(_testUserId, It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        _serviceMock.Verify(s => s.GetUserFavoritesAsync(differentUserId, It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Controller_RejectsInvalidGuidInToken()
    {
        // Arrange - Set up user with invalid GUID in claim
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid"),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.GetFavorites();

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion
}