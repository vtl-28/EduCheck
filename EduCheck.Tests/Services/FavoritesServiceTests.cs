using EduCheck.Application.DTOs.Favorites;
using EduCheck.Application.DTOs.Institute;
using EduCheck.Application.Interfaces;
using EduCheck.Domain.Entities;
using EduCheck.Infrastructure.Data;
using EduCheck.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EduCheck.Tests.Services;

public class FavoritesServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<FavoritesService>> _loggerMock;
    private readonly FavoritesService _service;

    // Test data
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    public FavoritesServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<FavoritesService>>();

        // Setup cache mock to return null (cache miss) by default
        _cacheServiceMock.Setup(c => c.GetAsync<List<FavoriteInstituteDto>>(It.IsAny<string>()))
            .ReturnsAsync((List<FavoriteInstituteDto>?)null);

        _service = new FavoritesService(_context, _cacheServiceMock.Object, _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add test institutes
        _context.Institutes.AddRange(new[]
        {
            new Institute
            {
                Id = 1,
                InstitutionName = "Test Institute 1",
                AccreditationNumber = "ACC001",
                ProviderType = "Accredited",
                Province = "Gauteng",
                CreatedAt = DateTime.UtcNow
            },
            new Institute
            {
                Id = 2,
                InstitutionName = "Test Institute 2",
                AccreditationNumber = "ACC002",
                ProviderType = "Accredited",
                Province = "Western Cape",
                CreatedAt = DateTime.UtcNow
            },
            new Institute
            {
                Id = 3,
                InstitutionName = "Test Institute 3",
                AccreditationNumber = "ACC003",
                ProviderType = "Provisional",
                Province = "KwaZulu-Natal",
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

    #region GetUserFavoritesAsync Tests

    [Fact]
    public async Task GetUserFavoritesAsync_ReturnsEmptyList_WhenUserHasNoFavorites()
    {
        // Act
        var result = await _service.GetUserFavoritesAsync(_testUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Favorites.Should().BeEmpty();
        result.Data.Pagination.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUserFavoritesAsync_ReturnsFavorites_SortedByMostRecentFirst()
    {
        // Arrange - Add favorites with different timestamps
        _context.FavoriteInstitutes.AddRange(new[]
        {
            new FavoriteInstitute
            {
                StudentId = _testUserId,
                InstituteId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new FavoriteInstitute
            {
                StudentId = _testUserId,
                InstituteId = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new FavoriteInstitute
            {
                StudentId = _testUserId,
                InstituteId = 3,
                CreatedAt = DateTime.UtcNow
            }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserFavoritesAsync(_testUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Favorites.Should().HaveCount(3);
        result.Data.Pagination.TotalCount.Should().Be(3);

        // Most recent first
        result.Data.Favorites[0].Institute.Id.Should().Be(3);
        result.Data.Favorites[1].Institute.Id.Should().Be(2);
        result.Data.Favorites[2].Institute.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetUserFavoritesAsync_ReturnsPaginatedResults()
    {
        // Arrange - Add 5 favorites
        for (int i = 1; i <= 3; i++)
        {
            _context.FavoriteInstitutes.Add(new FavoriteInstitute
            {
                StudentId = _testUserId,
                InstituteId = i,
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await _context.SaveChangesAsync();

        // Act - Get page 1 with size 2
        var result = await _service.GetUserFavoritesAsync(_testUserId, page: 1, pageSize: 2);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Favorites.Should().HaveCount(2);
        result.Data.Pagination.CurrentPage.Should().Be(1);
        result.Data.Pagination.PageSize.Should().Be(2);
        result.Data.Pagination.TotalCount.Should().Be(3);
        result.Data.Pagination.TotalPages.Should().Be(2);
        result.Data.Pagination.HasNextPage.Should().BeTrue();
        result.Data.Pagination.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserFavoritesAsync_UsesCachedData_WhenAvailable()
    {
        // Arrange - Setup cache to return data
        var cachedData = new List<FavoriteInstituteDto>
        {
            new FavoriteInstituteDto
            {
                Id = 1,
                FavoritedAt = DateTime.UtcNow,
                Institute = new InstituteDto { Id = 1, InstitutionName = "Cached Institute" }
            }
        };

        _cacheServiceMock.Setup(c => c.GetAsync<List<FavoriteInstituteDto>>($"favorites:{_testUserId}"))
            .ReturnsAsync(cachedData);

        // Act
        var result = await _service.GetUserFavoritesAsync(_testUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Favorites.Should().HaveCount(1);
        result.Data.Favorites[0].Institute.InstitutionName.Should().Be("Cached Institute");

        // Verify cache was checked
        _cacheServiceMock.Verify(c => c.GetAsync<List<FavoriteInstituteDto>>($"favorites:{_testUserId}"), Times.Once);
    }

    #endregion

    #region AddFavoriteAsync Tests

    [Fact]
    public async Task AddFavoriteAsync_AddsFavoriteSuccessfully()
    {
        // Act
        var result = await _service.AddFavoriteAsync(_testUserId, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Institute.Id.Should().Be(1);
        result.Data.Institute.InstitutionName.Should().Be("Test Institute 1");

        // Verify in database
        var favorite = await _context.FavoriteInstitutes
            .FirstOrDefaultAsync(f => f.StudentId == _testUserId && f.InstituteId == 1);
        favorite.Should().NotBeNull();
    }

    [Fact]
    public async Task AddFavoriteAsync_ReturnsError_WhenAlreadyFavorited()
    {
        // Arrange - Add favorite first
        await _service.AddFavoriteAsync(_testUserId, 1);

        // Act - Try to add again
        var result = await _service.AddFavoriteAsync(_testUserId, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already");
    }

    [Fact]
    public async Task AddFavoriteAsync_ReturnsError_WhenInstituteNotFound()
    {
        // Act
        var result = await _service.AddFavoriteAsync(_testUserId, 999);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task AddFavoriteAsync_InvalidatesCache()
    {
        // Act
        await _service.AddFavoriteAsync(_testUserId, 1);

        // Assert - Verify cache was invalidated
        _cacheServiceMock.Verify(c => c.RemoveAsync($"favorites:{_testUserId}"), Times.Once);
    }

    #endregion

    #region RemoveFavoriteAsync Tests

    [Fact]
    public async Task RemoveFavoriteAsync_RemovesFavoriteSuccessfully()
    {
        // Arrange
        await _service.AddFavoriteAsync(_testUserId, 1);

        // Act
        var result = await _service.RemoveFavoriteAsync(_testUserId, 1);

        // Assert
        result.Success.Should().BeTrue();

        // Verify removed from database
        var favorite = await _context.FavoriteInstitutes
            .FirstOrDefaultAsync(f => f.StudentId == _testUserId && f.InstituteId == 1);
        favorite.Should().BeNull();
    }

    [Fact]
    public async Task RemoveFavoriteAsync_ReturnsError_WhenNotFavorited()
    {
        // Act
        var result = await _service.RemoveFavoriteAsync(_testUserId, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task RemoveFavoriteAsync_InvalidatesCache()
    {
        // Arrange
        await _service.AddFavoriteAsync(_testUserId, 1);
        _cacheServiceMock.Invocations.Clear(); // Reset mock

        // Act
        await _service.RemoveFavoriteAsync(_testUserId, 1);

        // Assert - Verify cache was invalidated
        _cacheServiceMock.Verify(c => c.RemoveAsync($"favorites:{_testUserId}"), Times.Once);
    }

    #endregion

    #region GetFavoriteStatusAsync Tests

    [Fact]
    public async Task GetFavoriteStatusAsync_ReturnsTrue_WhenFavorited()
    {
        // Arrange
        await _service.AddFavoriteAsync(_testUserId, 1);

        // Act
        var result = await _service.GetFavoriteStatusAsync(_testUserId, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.IsFavorited.Should().BeTrue();
        result.Data.FavoriteId.Should().NotBeNull();
        result.Data.FavoritedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFavoriteStatusAsync_ReturnsFalse_WhenNotFavorited()
    {
        // Act
        var result = await _service.GetFavoriteStatusAsync(_testUserId, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.IsFavorited.Should().BeFalse();
        result.Data.FavoriteId.Should().BeNull();
        result.Data.FavoritedAt.Should().BeNull();
    }

    #endregion

    #region IDOR Prevention Tests

    [Fact]
    public async Task GetUserFavoritesAsync_OnlyReturnsCurrentUsersFavorites()
    {
        // Arrange - Add favorites for different users
        _context.FavoriteInstitutes.Add(new FavoriteInstitute
        {
            StudentId = _testUserId,
            InstituteId = 1,
            CreatedAt = DateTime.UtcNow
        });
        _context.FavoriteInstitutes.Add(new FavoriteInstitute
        {
            StudentId = _otherUserId,
            InstituteId = 2,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act - Get favorites for test user
        var result = await _service.GetUserFavoritesAsync(_testUserId);

        // Assert - Should only see test user's favorites
        result.Data!.Favorites.Should().HaveCount(1);
        result.Data.Favorites[0].Institute.Id.Should().Be(1);
        result.Data.Favorites.Should().NotContain(f => f.Institute.Id == 2);
    }

    [Fact]
    public async Task RemoveFavoriteAsync_CannotRemoveOtherUsersFavorite()
    {
        // Arrange - Add favorite for other user
        _context.FavoriteInstitutes.Add(new FavoriteInstitute
        {
            StudentId = _otherUserId,
            InstituteId = 1,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act - Try to remove other user's favorite using test user's ID
        var result = await _service.RemoveFavoriteAsync(_testUserId, 1);

        // Assert - Should return not found
        result.Success.Should().BeFalse();

        // Verify other user's favorite still exists
        var favorite = await _context.FavoriteInstitutes
            .FirstOrDefaultAsync(f => f.StudentId == _otherUserId && f.InstituteId == 1);
        favorite.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFavoriteStatusAsync_ReturnsFalse_ForOtherUsersFavorite()
    {
        // Arrange - Add favorite for other user
        _context.FavoriteInstitutes.Add(new FavoriteInstitute
        {
            StudentId = _otherUserId,
            InstituteId = 1,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act - Check status for test user
        var result = await _service.GetFavoriteStatusAsync(_testUserId, 1);

        // Assert - Should return false for test user
        result.Data!.IsFavorited.Should().BeFalse();
    }

    #endregion

    #region Pagination Edge Cases

    [Fact]
    public async Task GetUserFavoritesAsync_ClampsPageSizeToMax50()
    {
        // Arrange
        await _service.AddFavoriteAsync(_testUserId, 1);

        // Act - Request with pageSize > 50
        var result = await _service.GetUserFavoritesAsync(_testUserId, page: 1, pageSize: 100);

        // Assert - PageSize should be clamped to 50
        result.Data!.Pagination.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task GetUserFavoritesAsync_HandlesNegativePageNumber()
    {
        // Arrange
        await _service.AddFavoriteAsync(_testUserId, 1);

        // Act - Request with negative page
        var result = await _service.GetUserFavoritesAsync(_testUserId, page: -5, pageSize: 10);

        // Assert - Should default to page 1
        result.Data!.Pagination.CurrentPage.Should().Be(1);
    }

    #endregion
}