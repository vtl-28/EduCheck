using EduCheck.Application.DTOs.Institute;
using EduCheck.Application.DTOs.SearchHistory;
using EduCheck.Application.Interfaces;
using EduCheck.Domain.Entities;
using EduCheck.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduCheck.Infrastructure.Services;

public class SearchHistoryService : ISearchHistoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<SearchHistoryService> _logger;

    private const string CacheKeyPrefix = "search_history_";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

    public SearchHistoryService(
        ApplicationDbContext context,
        ICacheService cacheService,
        ILogger<SearchHistoryService> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<SearchHistoryResponse> GetUserSearchHistoryAsync(Guid userId, int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Getting search history for user: {UserId}", userId);

        try
        {

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;


            var cacheKey = GetCacheKey(userId);
            var cachedHistory = await _cacheService.GetAsync<List<SearchHistoryDto>>(cacheKey);

            List<SearchHistoryDto> allHistory;

            if (cachedHistory != null)
            {
                _logger.LogInformation("Retrieved search history from cache for user: {UserId}", userId);
                allHistory = cachedHistory;
            }
            else
            {

                _logger.LogInformation("Cache miss - fetching search history from database for user: {UserId}", userId);

                allHistory = await _context.InstituteSearchHistory
                    .AsNoTracking()
                    .Where(h => h.UserId == userId)
                    .Include(h => h.Institute)
                    .OrderByDescending(h => h.SearchedAt)
                    .Select(h => new SearchHistoryDto
                    {
                        Id = h.Id,
                        SearchedAt = h.SearchedAt,
                        Institute = new InstituteDto
                        {
                            Id = h.Institute.Id,
                            InstitutionName = h.Institute.InstitutionName,
                            AccreditationNumber = h.Institute.AccreditationNumber,
                            AccreditationPeriod = h.Institute.AccreditationPeriod,
                            ProviderType = h.Institute.ProviderType,
                            PostalAddress = h.Institute.PostalAddress,
                            PhysicalAddress = h.Institute.PhysicalAddress,
                            Telephone = h.Institute.Telephone,
                            Province = h.Institute.Province,
                            City = h.Institute.City,
                            IsAccredited = h.Institute.ProviderType != null &&
                                           h.Institute.ProviderType.ToLower() == "accredited"
                        }
                    })
                    .ToListAsync();


                await _cacheService.SetAsync(cacheKey, allHistory, CacheExpiration);
                _logger.LogInformation("Cached {Count} search history entries for user: {UserId}",
                    allHistory.Count, userId);
            }


            var totalCount = allHistory.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var skip = (page - 1) * pageSize;

            var pagedHistory = allHistory
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            return new SearchHistoryResponse
            {
                Success = true,
                Message = totalCount > 0
                    ? $"{totalCount} search history entries found"
                    : "No search history found",
                Data = new SearchHistoryData
                {
                    History = pagedHistory,
                    Pagination = new PaginationDto
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        HasNextPage = page < totalPages,
                        HasPreviousPage = page > 1
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search history for user: {UserId}", userId);

            return new SearchHistoryResponse
            {
                Success = false,
                Message = "An error occurred while retrieving search history",
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }

    public async Task RecordSearchAsync(Guid userId, int instituteId)
    {
        _logger.LogInformation("Recording search for user: {UserId}, institute: {InstituteId}",
            userId, instituteId);

        try
        {
            var existingEntry = await _context.InstituteSearchHistory
                .Where(h => h.UserId == userId &&
                            h.InstituteId == instituteId &&
                            h.SearchedAt > DateTime.UtcNow.AddHours(-24))
                .FirstOrDefaultAsync();

            if (existingEntry != null)
            {
                existingEntry.SearchedAt = DateTime.UtcNow;
                _context.InstituteSearchHistory.Update(existingEntry);
                _logger.LogInformation("Updated existing search history entry: {Id}", existingEntry.Id);
            }
            else
            {
                var historyEntry = new InstituteSearchHistory
                {
                    UserId = userId,
                    InstituteId = instituteId,
                    SearchedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.InstituteSearchHistory.AddAsync(historyEntry);
                _logger.LogInformation("Created new search history entry for user: {UserId}", userId);
            }

            await _context.SaveChangesAsync();

            await InvalidateUserCacheAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording search for user: {UserId}, institute: {InstituteId}",
                userId, instituteId);
        }
    }

    public async Task<DeleteSearchHistoryResponse> DeleteSearchHistoryEntryAsync(Guid userId, int historyId)
    {
        _logger.LogInformation("Deleting search history entry: {HistoryId} for user: {UserId}",
            historyId, userId);

        try
        {
            var entry = await _context.InstituteSearchHistory
                .Where(h => h.Id == historyId && h.UserId == userId)
                .FirstOrDefaultAsync();

            if (entry == null)
            {
                _logger.LogWarning("Search history entry not found: {HistoryId} for user: {UserId}",
                    historyId, userId);

                return new DeleteSearchHistoryResponse
                {
                    Success = false,
                    Message = "Search history entry not found",
                    DeletedCount = 0,
                    Errors = new List<string> { "The specified search history entry was not found or does not belong to you" }
                };
            }

            _context.InstituteSearchHistory.Remove(entry);
            await _context.SaveChangesAsync();

            await InvalidateUserCacheAsync(userId);

            _logger.LogInformation("Deleted search history entry: {HistoryId}", historyId);

            return new DeleteSearchHistoryResponse
            {
                Success = true,
                Message = "Search history entry deleted successfully",
                DeletedCount = 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting search history entry: {HistoryId} for user: {UserId}",
                historyId, userId);

            return new DeleteSearchHistoryResponse
            {
                Success = false,
                Message = "An error occurred while deleting search history entry",
                DeletedCount = 0,
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }

    public async Task<DeleteSearchHistoryResponse> ClearUserSearchHistoryAsync(Guid userId)
    {
        _logger.LogInformation("Clearing all search history for user: {UserId}", userId);

        try
        {
            var entries = await _context.InstituteSearchHistory
                .Where(h => h.UserId == userId)
                .ToListAsync();

            if (entries.Count == 0)
            {
                return new DeleteSearchHistoryResponse
                {
                    Success = true,
                    Message = "No search history to clear",
                    DeletedCount = 0
                };
            }

            _context.InstituteSearchHistory.RemoveRange(entries);
            await _context.SaveChangesAsync();

            await InvalidateUserCacheAsync(userId);

            _logger.LogInformation("Cleared {Count} search history entries for user: {UserId}",
                entries.Count, userId);

            return new DeleteSearchHistoryResponse
            {
                Success = true,
                Message = $"Successfully cleared {entries.Count} search history entries",
                DeletedCount = entries.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing search history for user: {UserId}", userId);

            return new DeleteSearchHistoryResponse
            {
                Success = false,
                Message = "An error occurred while clearing search history",
                DeletedCount = 0,
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }

    private static string GetCacheKey(Guid userId) => $"{CacheKeyPrefix}{userId}";

    private async Task InvalidateUserCacheAsync(Guid userId)
    {
        var cacheKey = GetCacheKey(userId);
        await _cacheService.RemoveAsync(cacheKey);
        _logger.LogDebug("Invalidated cache for user: {UserId}", userId);
    }
}