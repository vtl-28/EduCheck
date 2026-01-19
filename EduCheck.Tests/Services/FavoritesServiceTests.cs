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

    // User IDs (from JWT token)
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    // Student IDs (from students table)
    private Guid _testStudentId;
    private Guid _otherStudentId;

    public FavoritesServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<FavoritesService>>();

        _cacheServiceMock.Setup(c => c.GetAsync<List<FavoriteInstituteDto>>(It.IsAny<string>()))
            .ReturnsAsync((List<FavoriteInstituteDto>?)null);

        _service = new FavoritesService(_context, _cacheServiceMock.Object, _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create Student IDs
        _testStudentId = Guid.NewGuid();
        _otherStudentId = Guid.NewGuid();

        // Add test students (linking UserId to StudentId)
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
        var result = await _service.GetUserFavoritesAsync(_testUserId);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Favorites.Should().BeEmpty();
        result.Data.Pagination.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUserFavoritesAsync_ReturnsFavorites_SortedByMostRecentFirst()
    {
        // Use StudentId (not UserId) when adding directly to database
        _context.FavoriteInstitutes.AddRange(new[]
        {
            new FavoriteInstitute
            {
                StudentId = _testStudentId,
                InstituteId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new FavoriteInstitute
            {
                StudentId = _testStudentId,
                InstituteId = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new FavoriteInstitute
            {
                StudentId = _testStudentId,
                InstituteId = 3,
                CreatedAt = DateTime.UtcNow
            }
        });
        await _context.SaveChangesAsync();

        // Pass UserId to service (it will look up StudentId internally)
        var result = await _service.GetUserFavoritesAsync(_testUserId);

        result.Success.Should().BeTrue();
        result.Data!.Favorites.Should().HaveCount(3);
        result.Data.Pagination.TotalCount.Should().Be(3);

        result.Data.Favorites[0].Institute.Id.Should().Be(3);
        result.Data.Favorites[1].Institute.Id.Should().Be(2);
        result.Data.Favorites[2].Institute.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetUserFavoritesAsync_ReturnsPaginatedResults()
    {
        for (int i = 1; i <= 3; i++)
        {
            _context.FavoriteInstitutes.Add(new FavoriteInstitute
            {
                StudentId = _testStudentId,
                InstituteId = i,
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await _context.SaveChangesAsync();

        var result = await _service.GetUserFavoritesAsync(_testUserId, page: 1, pageSize: 2);

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

        var result = await _service.GetUserFavoritesAsync(_testUserId);

        result.Success.Should().BeTrue();
        result.Data!.Favorites.Should().HaveCount(1);
        result.Data.Favorites[0].Institute.InstitutionName.Should().Be("Cached Institute");

        _cacheServiceMock.Verify(c => c.GetAsync<List<FavoriteInstituteDto>>($"favorites:{_testUserId}"), Times.Once);
    }

    [Fact]
    public async Task GetUserFavoritesAsync_ReturnsError_WhenStudentNotFound()
    {
        // Use a random UserId that has no associated Student
        var unknownUserId = Guid.NewGuid();

        var result = await _service.GetUserFavoritesAsync(unknownUserId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Student profile not found");
    }

    #endregion

    #region AddFavoriteAsync Tests

    [Fact]
    public async Task AddFavoriteAsync_AddsFavoriteSuccessfully()
    {
        var result = await _service.AddFavoriteAsync(_testUserId, 1);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Institute.Id.Should().Be(1);
        result.Data.Institute.InstitutionName.Should().Be("Test Institute 1");

        // Verify using StudentId in database
        var favorite = await _context.FavoriteInstitutes
            .FirstOrDefaultAsync(f => f.StudentId == _testStudentId && f.InstituteId == 1);
        favorite.Should().NotBeNull();
    }

    [Fact]
    public async Task AddFavoriteAsync_ReturnsError_WhenAlreadyFavorited()
    {
        await _service.AddFavoriteAsync(_testUserId, 1);

        var result = await _service.AddFavoriteAsync(_testUserId, 1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already");
    }

    [Fact]
    public async Task AddFavoriteAsync_ReturnsError_WhenInstituteNotFound()
    {
        var result = await _service.AddFavoriteAsync(_testUserId, 999);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task AddFavoriteAsync_ReturnsError_WhenStudentNotFound()
    {
        var unknownUserId = Guid.NewGuid();

        var result = await _service.AddFavoriteAsync(unknownUserId, 1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Student profile not found");
    }

    [Fact]
    public async Task AddFavoriteAsync_InvalidatesCache()
    {
        await _service.AddFavoriteAsync(_testUserId, 1);

        _cacheServiceMock.Verify(c => c.RemoveAsync($"favorites:{_testUserId}"), Times.Once);
    }

    #endregion

    #region RemoveFavoriteAsync Tests

    [Fact]
    public async Task RemoveFavoriteAsync_RemovesFavoriteSuccessfully()
    {
        await _service.AddFavoriteAsync(_testUserId, 1);

        var result = await _service.RemoveFavoriteAsync(_testUserId, 1);

        result.Success.Should().BeTrue();

        var favorite = await _context.FavoriteInstitutes
            .FirstOrDefaultAsync(f => f.StudentId == _testStudentId && f.InstituteId == 1);
        favorite.Should().BeNull();
    }

    [Fact]
    public async Task RemoveFavoriteAsync_ReturnsError_WhenNotFavorited()
    {
        var result = await _service.RemoveFavoriteAsync(_testUserId, 1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task RemoveFavoriteAsync_ReturnsError_WhenStudentNotFound()
    {
        var unknownUserId = Guid.NewGuid();

        var result = await _service.RemoveFavoriteAsync(unknownUserId, 1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Student profile not found");
    }

    [Fact]
    public async Task RemoveFavoriteAsync_InvalidatesCache()
    {
        await _service.AddFavoriteAsync(_testUserId, 1);
        _cacheServiceMock.Invocations.Clear();

        await _service.RemoveFavoriteAsync(_testUserId, 1);

        _cacheServiceMock.Verify(c => c.RemoveAsync($"favorites:{_testUserId}"), Times.Once);
    }

    #endregion

    #region GetFavoriteStatusAsync Tests

    [Fact]
    public async Task GetFavoriteStatusAsync_ReturnsTrue_WhenFavorited()
    {
        await _service.AddFavoriteAsync(_testUserId, 1);

        var result = await _service.GetFavoriteStatusAsync(_testUserId, 1);

        result.Success.Should().BeTrue();
        result.Data!.IsFavorited.Should().BeTrue();
        result.Data.FavoriteId.Should().NotBeNull();
        result.Data.FavoritedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFavoriteStatusAsync_ReturnsFalse_WhenNotFavorited()
    {
        var result = await _service.GetFavoriteStatusAsync(_testUserId, 1);

        result.Success.Should().BeTrue();
        result.Data!.IsFavorited.Should().BeFalse();
        result.Data.FavoriteId.Should().BeNull();
        result.Data.FavoritedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetFavoriteStatusAsync_ReturnsError_WhenStudentNotFound()
    {
        var unknownUserId = Guid.NewGuid();

        var result = await _service.GetFavoriteStatusAsync(unknownUserId, 1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Student profile not found");
    }

    #endregion

    #region IDOR Prevention Tests

    [Fact]
    public async Task GetUserFavoritesAsync_OnlyReturnsCurrentUsersFavorites()
    {
        // Use StudentIds when adding directly to database
        _context.FavoriteInstitutes.Add(new FavoriteInstitute
        {
            StudentId = _testStudentId,
            InstituteId = 1,
            CreatedAt = DateTime.UtcNow
        });
        _context.FavoriteInstitutes.Add(new FavoriteInstitute
        {
            StudentId = _otherStudentId,
            InstituteId = 2,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetUserFavoritesAsync(_testUserId);

        result.Data!.Favorites.Should().HaveCount(1);
        result.Data.Favorites[0].Institute.Id.Should().Be(1);
        result.Data.Favorites.Should().NotContain(f => f.Institute.Id == 2);
    }

    [Fact]
    public async Task RemoveFavoriteAsync_CannotRemoveOtherUsersFavorite()
    {
        _context.FavoriteInstitutes.Add(new FavoriteInstitute
        {
            StudentId = _otherStudentId,
            InstituteId = 1,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _service.RemoveFavoriteAsync(_testUserId, 1);

        result.Success.Should().BeFalse();

        var favorite = await _context.FavoriteInstitutes
            .FirstOrDefaultAsync(f => f.StudentId == _otherStudentId && f.InstituteId == 1);
        favorite.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFavoriteStatusAsync_ReturnsFalse_ForOtherUsersFavorite()
    {
        _context.FavoriteInstitutes.Add(new FavoriteInstitute
        {
            StudentId = _otherStudentId,
            InstituteId = 1,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetFavoriteStatusAsync(_testUserId, 1);

        result.Data!.IsFavorited.Should().BeFalse();
    }

    #endregion

    #region Pagination Edge Cases

    [Fact]
    public async Task GetUserFavoritesAsync_ClampsPageSizeToMax50()
    {
        await _service.AddFavoriteAsync(_testUserId, 1);

        var result = await _service.GetUserFavoritesAsync(_testUserId, page: 1, pageSize: 100);

        result.Data!.Pagination.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task GetUserFavoritesAsync_HandlesNegativePageNumber()
    {
        await _service.AddFavoriteAsync(_testUserId, 1);

        var result = await _service.GetUserFavoritesAsync(_testUserId, page: -5, pageSize: 10);

        result.Data!.Pagination.CurrentPage.Should().Be(1);
    }

    #endregion
}