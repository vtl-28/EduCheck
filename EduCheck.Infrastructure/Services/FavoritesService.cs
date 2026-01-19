using EduCheck.Application.DTOs.Favorites;
using EduCheck.Application.DTOs.Institute;
using EduCheck.Application.Interfaces;
using EduCheck.Domain.Entities;
using EduCheck.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduCheck.Infrastructure.Services;

/// <summary>
/// Service for managing user's favorite institutes.
/// Implements IDOR prevention, caching, and audit logging.
/// </summary>
public class FavoritesService : IFavoritesService
{
    private readonly ApplicationDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<FavoritesService> _logger;

    private const string CACHE_KEY_PREFIX = "favorites";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

    public FavoritesService(
        ApplicationDbContext context,
        ICacheService cacheService,
        ILogger<FavoritesService> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FavoritesResponse> GetUserFavoritesAsync(Guid userId, int page = 1, int pageSize = 10)
    {
        try
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);


            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                _logger.LogWarning("Get favorites failed - student not found for user. UserId: {UserId}", userId);

                return new FavoritesResponse
                {
                    Success = false,
                    Message = "Student profile not found",
                    Errors = new List<string> { "Your student profile was not found. Please contact support." }
                };
            }

            var cacheKey = $"{CACHE_KEY_PREFIX}:{userId}";
            var allFavorites = await _cacheService.GetAsync<List<FavoriteInstituteDto>>(cacheKey);

            if (allFavorites == null)
            {
                _logger.LogDebug("Cache miss for favorites. UserId: {UserId}, StudentId: {StudentId}", userId, student.Id);

                allFavorites = await _context.FavoriteInstitutes
                    .AsNoTracking()
                    .Where(f => f.StudentId == student.Id)
                    .OrderByDescending(f => f.CreatedAt)
                    .Include(f => f.Institute)
                    .Select(f => new FavoriteInstituteDto
                    {
                        Id = f.Id,
                        FavoritedAt = f.CreatedAt,
                        Institute = new InstituteDto
                        {
                            Id = f.Institute.Id,
                            InstitutionName = f.Institute.InstitutionName,
                            AccreditationNumber = f.Institute.AccreditationNumber,
                            AccreditationPeriod = f.Institute.AccreditationPeriod,
                            ProviderType = f.Institute.ProviderType,
                            PostalAddress = f.Institute.PostalAddress,
                            PhysicalAddress = f.Institute.PhysicalAddress,
                            Telephone = f.Institute.Telephone,
                            Province = f.Institute.Province,
                            City = f.Institute.City,
                            IsAccredited = f.Institute.ProviderType != null &&
                                           f.Institute.ProviderType.ToLower() == "accredited"
                        }
                    })
                    .ToListAsync();

                await _cacheService.SetAsync(cacheKey, allFavorites, CacheExpiration);
                _logger.LogInformation("Cached {Count} favorites for user: {UserId}, StudentId: {StudentId}",
                    allFavorites.Count, userId, student.Id);
            }

            var totalCount = allFavorites.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var skip = (page - 1) * pageSize;

            var pagedFavorites = allFavorites
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            return new FavoritesResponse
            {
                Success = true,
                Message = totalCount > 0
                    ? $"{totalCount} favorite(s) found"
                    : "No favorites found",
                Data = new FavoritesData
                {
                    Favorites = pagedFavorites,
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
            _logger.LogError(ex, "Error getting favorites for user: {UserId}", userId);

            return new FavoritesResponse
            {
                Success = false,
                Message = "An error occurred while retrieving favorites",
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }

    /// <inheritdoc />
    public async Task<AddFavoriteResponse> AddFavoriteAsync(Guid userId, int instituteId)
    {
        try
        {
            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                _logger.LogWarning("Add favorite failed - student not found for user. UserId: {UserId}", userId);

                return new AddFavoriteResponse
                {
                    Success = false,
                    Message = "Student profile not found",
                    Errors = new List<string> { "Your student profile was not found. Please contact support." }
                };
            }

            var institute = await _context.Institutes
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == instituteId);

            if (institute == null)
            {
                _logger.LogWarning("Add favorite failed - institute not found. UserId: {UserId}, InstituteId: {InstituteId}",
                    userId, instituteId);

                return new AddFavoriteResponse
                {
                    Success = false,
                    Message = "Institute not found",
                    Errors = new List<string> { $"Institute with ID {instituteId} does not exist" }
                };
            }

            var existingFavorite = await _context.FavoriteInstitutes
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.StudentId == student.Id && f.InstituteId == instituteId);

            if (existingFavorite != null)
            {
                _logger.LogInformation("Institute already favorited. UserId: {UserId}, StudentId: {StudentId}, InstituteId: {InstituteId}",
                    userId, student.Id, instituteId);

                return new AddFavoriteResponse
                {
                    Success = false,
                    Message = "Institute is already in favorites",
                    Errors = new List<string> { "This institute is already in your favorites" }
                };
            }

            var favorite = new FavoriteInstitute
            {
                StudentId = student.Id,
                InstituteId = instituteId,
                CreatedAt = DateTime.UtcNow
            };

            _context.FavoriteInstitutes.Add(favorite);
            await _context.SaveChangesAsync();

            var cacheKey = $"{CACHE_KEY_PREFIX}:{userId}";
            await _cacheService.RemoveAsync(cacheKey);

            _logger.LogInformation(
                "AUDIT: Institute added to favorites. UserId: {UserId}, StudentId: {StudentId}, InstituteId: {InstituteId}, FavoriteId: {FavoriteId}",
                userId, student.Id, instituteId, favorite.Id);

            return new AddFavoriteResponse
            {
                Success = true,
                Message = "Institute added to favorites",
                Data = new FavoriteInstituteDto
                {
                    Id = favorite.Id,
                    FavoritedAt = favorite.CreatedAt,
                    Institute = new InstituteDto
                    {
                        Id = institute.Id,
                        InstitutionName = institute.InstitutionName,
                        AccreditationNumber = institute.AccreditationNumber,
                        AccreditationPeriod = institute.AccreditationPeriod,
                        ProviderType = institute.ProviderType,
                        PostalAddress = institute.PostalAddress,
                        PhysicalAddress = institute.PhysicalAddress,
                        Telephone = institute.Telephone,
                        Province = institute.Province,
                        City = institute.City,
                        IsAccredited = institute.ProviderType != null &&
                                       institute.ProviderType.ToLower() == "accredited"
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding favorite. UserId: {UserId}, InstituteId: {InstituteId}",
                userId, instituteId);

            return new AddFavoriteResponse
            {
                Success = false,
                Message = "An error occurred while adding to favorites",
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }

    /// <inheritdoc />
    public async Task<RemoveFavoriteResponse> RemoveFavoriteAsync(Guid userId, int instituteId)
    {
        try
        {
            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                _logger.LogWarning("Remove favorite failed - student not found for user. UserId: {UserId}", userId);

                return new RemoveFavoriteResponse
                {
                    Success = false,
                    Message = "Student profile not found",
                    Errors = new List<string> { "Your student profile was not found. Please contact support." }
                };
            }

            var favorite = await _context.FavoriteInstitutes
                .FirstOrDefaultAsync(f => f.StudentId == student.Id && f.InstituteId == instituteId);

            if (favorite == null)
            {
                _logger.LogInformation("Remove favorite - not found. UserId: {UserId}, StudentId: {StudentId}, InstituteId: {InstituteId}",
                    userId, student.Id, instituteId);

                return new RemoveFavoriteResponse
                {
                    Success = false,
                    Message = "Favorite not found",
                    Errors = new List<string> { "This institute is not in your favorites" }
                };
            }

            _context.FavoriteInstitutes.Remove(favorite);
            await _context.SaveChangesAsync();

            var cacheKey = $"{CACHE_KEY_PREFIX}:{userId}";
            await _cacheService.RemoveAsync(cacheKey);

            _logger.LogInformation(
                "AUDIT: Institute removed from favorites. UserId: {UserId}, StudentId: {StudentId}, InstituteId: {InstituteId}, FavoriteId: {FavoriteId}",
                userId, student.Id, instituteId, favorite.Id);

            return new RemoveFavoriteResponse
            {
                Success = true,
                Message = "Institute removed from favorites"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing favorite. UserId: {UserId}, InstituteId: {InstituteId}",
                userId, instituteId);

            return new RemoveFavoriteResponse
            {
                Success = false,
                Message = "An error occurred while removing from favorites",
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }

    /// <inheritdoc />
    public async Task<FavoriteStatusResponse> GetFavoriteStatusAsync(Guid userId, int instituteId)
    {
        try
        {
            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                _logger.LogWarning("Get favorite status failed - student not found for user. UserId: {UserId}", userId);

                return new FavoriteStatusResponse
                {
                    Success = false,
                    Message = "Student profile not found",
                    Errors = new List<string> { "Your student profile was not found. Please contact support." }
                };
            }

            var favorite = await _context.FavoriteInstitutes
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.StudentId == student.Id && f.InstituteId == instituteId);

            return new FavoriteStatusResponse
            {
                Success = true,
                Message = favorite != null ? "Institute is favorited" : "Institute is not favorited",
                Data = new FavoriteStatusDto
                {
                    IsFavorited = favorite != null,
                    FavoriteId = favorite?.Id,
                    FavoritedAt = favorite?.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking favorite status. UserId: {UserId}, InstituteId: {InstituteId}",
                userId, instituteId);

            return new FavoriteStatusResponse
            {
                Success = false,
                Message = "An error occurred while checking favorite status",
                Errors = new List<string> { "An unexpected error occurred. Please try again." }
            };
        }
    }
}