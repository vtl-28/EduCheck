using EduCheck.Application.DTOs.Institute;
using EduCheck.Application.DTOs.SearchHistory;
using EduCheck.Application.Interfaces;
using EduCheck.Domain.Entities;
using EduCheck.Infrastructure.Data;
using EduCheck.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EduCheck.Tests.Services;

public class SearchHistoryServiceTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<SearchHistoryService>> _loggerMock;

    public SearchHistoryServiceTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<SearchHistoryService>>();
    }

    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private SearchHistoryService CreateService(ApplicationDbContext context)
    {
        return new SearchHistoryService(context, _cacheServiceMock.Object, _loggerMock.Object);
    }

    private static async Task<(Guid userId, Institute institute)> SeedTestData(ApplicationDbContext context)
    {
        var userId = Guid.NewGuid();

        var institute = new Institute
        {
            Id = 1,
            InstitutionName = "University of Cape Town",
            AccreditationNumber = "16 UCT 00001",
            AccreditationPeriod = "01 August 2016",
            ProviderType = "Accredited",
            Province = "Western Cape",
            City = "Cape Town",
            PhysicalAddress = "Rondebosch, Cape Town",
            PostalAddress = "Private Bag X3, Rondebosch 7701",
            Telephone = "021 650 9111",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await context.Institutes.AddAsync(institute);
        await context.SaveChangesAsync();

        return (userId, institute);
    }

    private static async Task SeedMultipleInstitutes(ApplicationDbContext context)
    {
        var institutes = new List<Institute>
        {
            new()
            {
                Id = 1,
                InstitutionName = "University of Cape Town",
                AccreditationNumber = "16 UCT 00001",
                ProviderType = "Accredited",
                Province = "Western Cape",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                InstitutionName = "University of Johannesburg",
                AccreditationNumber = "16 UJ 00002",
                ProviderType = "Accredited",
                Province = "Gauteng",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                InstitutionName = "Stellenbosch University",
                AccreditationNumber = "16 SU 00003",
                ProviderType = "Accredited",
                Province = "Western Cape",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.Institutes.AddRangeAsync(institutes);
        await context.SaveChangesAsync();
    }

    #region GetUserSearchHistoryAsync Tests

    [Fact]
    public async Task GetUserSearchHistoryAsync_WithCachedData_ReturnsCachedHistory()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var cachedHistory = new List<SearchHistoryDto>
        {
            new()
            {
                Id = 1,
                SearchedAt = DateTime.UtcNow,
                Institute = new InstituteDto
                {
                    Id = 1,
                    InstitutionName = "Cached University",
                    AccreditationNumber = "CACHED001"
                }
            }
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<SearchHistoryDto>>(It.IsAny<string>()))
            .ReturnsAsync(cachedHistory);

        // Act
        var result = await service.GetUserSearchHistoryAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.History.Should().HaveCount(1);
        result.Data.History.First().Institute.InstitutionName.Should().Be("Cached University");

        // Verify cache was checked
        _cacheServiceMock.Verify(x => x.GetAsync<List<SearchHistoryDto>>(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetUserSearchHistoryAsync_WithoutCachedData_FetchesFromDatabase()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var (userId, institute) = await SeedTestData(context);

        var historyEntry = new InstituteSearchHistory
        {
            UserId = userId,
            InstituteId = institute.Id,
            SearchedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await context.InstituteSearchHistory.AddAsync(historyEntry);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<SearchHistoryDto>>(It.IsAny<string>()))
            .ReturnsAsync((List<SearchHistoryDto>?)null);

        // Act
        var result = await service.GetUserSearchHistoryAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.History.Should().HaveCount(1);
        result.Data.History.First().Institute.InstitutionName.Should().Be("University of Cape Town");

        // Verify cache was set
        _cacheServiceMock.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<List<SearchHistoryDto>>(),
            It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetUserSearchHistoryAsync_NoHistory_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<SearchHistoryDto>>(It.IsAny<string>()))
            .ReturnsAsync((List<SearchHistoryDto>?)null);

        // Act
        var result = await service.GetUserSearchHistoryAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("No search history found");
        result.Data!.History.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserSearchHistoryAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        await SeedMultipleInstitutes(context);
        var userId = Guid.NewGuid();

        // Add 3 history entries
        for (int i = 1; i <= 3; i++)
        {
            await context.InstituteSearchHistory.AddAsync(new InstituteSearchHistory
            {
                UserId = userId,
                InstituteId = i,
                SearchedAt = DateTime.UtcNow.AddMinutes(-i),
                CreatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var service = CreateService(context);

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<SearchHistoryDto>>(It.IsAny<string>()))
            .ReturnsAsync((List<SearchHistoryDto>?)null);

        // Act
        var result = await service.GetUserSearchHistoryAsync(userId, page: 1, pageSize: 2);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.History.Should().HaveCount(2);
        result.Data.Pagination.CurrentPage.Should().Be(1);
        result.Data.Pagination.PageSize.Should().Be(2);
        result.Data.Pagination.TotalCount.Should().Be(3);
        result.Data.Pagination.TotalPages.Should().Be(2);
        result.Data.Pagination.HasNextPage.Should().BeTrue();
        result.Data.Pagination.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserSearchHistoryAsync_SecondPage_ReturnsRemainingResults()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        await SeedMultipleInstitutes(context);
        var userId = Guid.NewGuid();

        for (int i = 1; i <= 3; i++)
        {
            await context.InstituteSearchHistory.AddAsync(new InstituteSearchHistory
            {
                UserId = userId,
                InstituteId = i,
                SearchedAt = DateTime.UtcNow.AddMinutes(-i),
                CreatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var service = CreateService(context);

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<SearchHistoryDto>>(It.IsAny<string>()))
            .ReturnsAsync((List<SearchHistoryDto>?)null);

        // Act
        var result = await service.GetUserSearchHistoryAsync(userId, page: 2, pageSize: 2);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.History.Should().HaveCount(1);
        result.Data.Pagination.CurrentPage.Should().Be(2);
        result.Data.Pagination.HasNextPage.Should().BeFalse();
        result.Data.Pagination.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserSearchHistoryAsync_ReturnsHistoryOrderedBySearchedAtDescending()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        await SeedMultipleInstitutes(context);
        var userId = Guid.NewGuid();

        await context.InstituteSearchHistory.AddAsync(new InstituteSearchHistory
        {
            UserId = userId,
            InstituteId = 1,
            SearchedAt = DateTime.UtcNow.AddHours(-2),
            CreatedAt = DateTime.UtcNow
        });
        await context.InstituteSearchHistory.AddAsync(new InstituteSearchHistory
        {
            UserId = userId,
            InstituteId = 2,
            SearchedAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow
        });
        await context.InstituteSearchHistory.AddAsync(new InstituteSearchHistory
        {
            UserId = userId,
            InstituteId = 3,
            SearchedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<SearchHistoryDto>>(It.IsAny<string>()))
            .ReturnsAsync((List<SearchHistoryDto>?)null);

        // Act
        var result = await service.GetUserSearchHistoryAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.History.Should().HaveCount(3);
        result.Data.History[0].Institute.Id.Should().Be(3); // Most recent
        result.Data.History[1].Institute.Id.Should().Be(2);
        result.Data.History[2].Institute.Id.Should().Be(1); // Oldest
    }

    [Fact]
    public async Task GetUserSearchHistoryAsync_OnlyReturnsCurrentUserHistory()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        await SeedMultipleInstitutes(context);
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        // User 1 history
        await context.InstituteSearchHistory.AddAsync(new InstituteSearchHistory
        {
            UserId = userId1,
            InstituteId = 1,
            SearchedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        // User 2 history
        await context.InstituteSearchHistory.AddAsync(new InstituteSearchHistory
        {
            UserId = userId2,
            InstituteId = 2,
            SearchedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<SearchHistoryDto>>(It.IsAny<string>()))
            .ReturnsAsync((List<SearchHistoryDto>?)null);

        // Act
        var result = await service.GetUserSearchHistoryAsync(userId1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.History.Should().HaveCount(1);
        result.Data.History.First().Institute.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetUserSearchHistoryAsync_ReturnsFullInstituteData()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var (userId, institute) = await SeedTestData(context);

        await context.InstituteSearchHistory.AddAsync(new InstituteSearchHistory
        {
            UserId = userId,
            InstituteId = institute.Id,
            SearchedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<SearchHistoryDto>>(It.IsAny<string>()))
            .ReturnsAsync((List<SearchHistoryDto>?)null);

        // Act
        var result = await service.GetUserSearchHistoryAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        var historyItem = result.Data!.History.First();
        historyItem.Institute.Id.Should().Be(1);
        historyItem.Institute.InstitutionName.Should().Be("University of Cape Town");
        historyItem.Institute.AccreditationNumber.Should().Be("16 UCT 00001");
        historyItem.Institute.Province.Should().Be("Western Cape");
        historyItem.Institute.City.Should().Be("Cape Town");
        historyItem.Institute.IsAccredited.Should().BeTrue();
    }

    #endregion

    #region RecordSearchAsync Tests

    [Fact]
    public async Task RecordSearchAsync_NewSearch_CreatesHistoryEntry()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var (userId, institute) = await SeedTestData(context);
        var service = CreateService(context);

        // Act
        await service.RecordSearchAsync(userId, institute.Id);

        // Assert
        var historyEntries = await context.InstituteSearchHistory.ToListAsync();
        historyEntries.Should().HaveCount(1);
        historyEntries.First().UserId.Should().Be(userId);
        historyEntries.First().InstituteId.Should().Be(institute.Id);

        // Verify cache was invalidated
        _cacheServiceMock.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RecordSearchAsync_RecentDuplicateSearch_UpdatesTimestamp()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var (userId, institute) = await SeedTestData(context);

        var originalSearchTime = DateTime.UtcNow.AddHours(-1);
        await context.InstituteSearchHistory.AddAsync(new InstituteSearchHistory
        {
            UserId = userId,
            InstituteId = institute.Id,
            SearchedAt = originalSearchTime,
            CreatedAt = originalSearchTime
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.RecordSearchAsync(userId, institute.Id);

        // Assert
        var historyEntries = await context.InstituteSearchHistory.ToListAsync();
        historyEntries.Should().HaveCount(1); // Should not create duplicate
        historyEntries.First().SearchedAt.Should().BeAfter(originalSearchTime);
    }

    [Fact]
    public async Task RecordSearchAsync_OldDuplicateSearch_CreatesNewEntry()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var (userId, institute) = await SeedTestData(context);

        var oldSearchTime = DateTime.UtcNow.AddHours(-25); // More than 24 hours ago
        await context.InstituteSearchHistory.AddAsync(new InstituteSearchHistory
        {
            UserId = userId,
            InstituteId = institute.Id,
            SearchedAt = oldSearchTime,
            CreatedAt = oldSearchTime
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.RecordSearchAsync(userId, institute.Id);

        // Assert
        var historyEntries = await context.InstituteSearchHistory.ToListAsync();
        historyEntries.Should().HaveCount(2); // Should create new entry
    }

    [Fact]
    public async Task RecordSearchAsync_InvalidatesCache()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var (userId, institute) = await SeedTestData(context);
        var service = CreateService(context);

        // Act
        await service.RecordSearchAsync(userId, institute.Id);

        // Assert
        _cacheServiceMock.Verify(x => x.RemoveAsync(It.Is<string>(s => s.Contains(userId.ToString()))), Times.Once);
    }

    #endregion

    #region DeleteSearchHistoryEntryAsync Tests

    [Fact]
    public async Task DeleteSearchHistoryEntryAsync_ValidEntry_DeletesSuccessfully()
    {

        await using var context = CreateInMemoryDbContext();
        var (userId, institute) = await SeedTestData(context);

        var historyEntry = new InstituteSearchHistory
        {
            UserId = userId,
            InstituteId = institute.Id,
            SearchedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await context.InstituteSearchHistory.AddAsync(historyEntry);
        await context.SaveChangesAsync();

        var service = CreateService(context);


        var result = await service.DeleteSearchHistoryEntryAsync(userId, historyEntry.Id);

        result.Success.Should().BeTrue();
        result.DeletedCount.Should().Be(1);
        result.Message.Should().Be("Search history entry deleted successfully");

        var remainingEntries = await context.InstituteSearchHistory.ToListAsync();
        remainingEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteSearchHistoryEntryAsync_InvalidEntry_ReturnsError()
    {

        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();


        var result = await service.DeleteSearchHistoryEntryAsync(userId, 999);

        result.Success.Should().BeFalse();
        result.DeletedCount.Should().Be(0);
        result.Message.Should().Be("Search history entry not found");
    }

    [Fact]
    public async Task DeleteSearchHistoryEntryAsync_OtherUsersEntry_ReturnsError()
    {

        await using var context = CreateInMemoryDbContext();
        var (userId1, institute) = await SeedTestData(context);
        var userId2 = Guid.NewGuid();

        var historyEntry = new InstituteSearchHistory
        {
            UserId = userId1,
            InstituteId = institute.Id,
            SearchedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await context.InstituteSearchHistory.AddAsync(historyEntry);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.DeleteSearchHistoryEntryAsync(userId2, historyEntry.Id);


        result.Success.Should().BeFalse();
        result.DeletedCount.Should().Be(0);

        var entries = await context.InstituteSearchHistory.ToListAsync();
        entries.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteSearchHistoryEntryAsync_InvalidatesCache()
    {
        await using var context = CreateInMemoryDbContext();
        var (userId, institute) = await SeedTestData(context);

        var historyEntry = new InstituteSearchHistory
        {
            UserId = userId,
            InstituteId = institute.Id,
            SearchedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await context.InstituteSearchHistory.AddAsync(historyEntry);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await service.DeleteSearchHistoryEntryAsync(userId, historyEntry.Id);

        _cacheServiceMock.Verify(x => x.RemoveAsync(It.Is<string>(s => s.Contains(userId.ToString()))), Times.Once);
    }

    #endregion

    #region ClearUserSearchHistoryAsync Tests

    [Fact]
    public async Task ClearUserSearchHistoryAsync_WithHistory_ClearsAllEntries()
    {

        await using var context = CreateInMemoryDbContext();
        await SeedMultipleInstitutes(context);
        var userId = Guid.NewGuid();

        for (int i = 1; i <= 3; i++)
        {
            await context.InstituteSearchHistory.AddAsync(new InstituteSearchHistory
            {
                UserId = userId,
                InstituteId = i,
                SearchedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.ClearUserSearchHistoryAsync(userId);

        result.Success.Should().BeTrue();
        result.DeletedCount.Should().Be(3);
        result.Message.Should().Be("Successfully cleared 3 search history entries");

        var remainingEntries = await context.InstituteSearchHistory.ToListAsync();
        remainingEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearUserSearchHistoryAsync_NoHistory_ReturnsZeroDeleted()
    {

        await using var context = CreateInMemoryDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();


        var result = await service.ClearUserSearchHistoryAsync(userId);

        result.Success.Should().BeTrue();
        result.DeletedCount.Should().Be(0);
        result.Message.Should().Be("No search history to clear");
    }

    [Fact]
    public async Task ClearUserSearchHistoryAsync_OnlyClearsCurrentUserHistory()
    {

        await using var context = CreateInMemoryDbContext();
        await SeedMultipleInstitutes(context);
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();


        await context.InstituteSearchHistory.AddAsync(new InstituteSearchHistory
        {
            UserId = userId1,
            InstituteId = 1,
            SearchedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        await context.InstituteSearchHistory.AddAsync(new InstituteSearchHistory
        {
            UserId = userId2,
            InstituteId = 2,
            SearchedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.ClearUserSearchHistoryAsync(userId1);


        result.Success.Should().BeTrue();
        result.DeletedCount.Should().Be(1);


        var remainingEntries = await context.InstituteSearchHistory.ToListAsync();
        remainingEntries.Should().HaveCount(1);
        remainingEntries.First().UserId.Should().Be(userId2);
    }

    [Fact]
    public async Task ClearUserSearchHistoryAsync_InvalidatesCache()
    {

        await using var context = CreateInMemoryDbContext();
        var (userId, institute) = await SeedTestData(context);

        await context.InstituteSearchHistory.AddAsync(new InstituteSearchHistory
        {
            UserId = userId,
            InstituteId = institute.Id,
            SearchedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await service.ClearUserSearchHistoryAsync(userId);

        _cacheServiceMock.Verify(x => x.RemoveAsync(It.Is<string>(s => s.Contains(userId.ToString()))), Times.Once);
    }

    #endregion
}