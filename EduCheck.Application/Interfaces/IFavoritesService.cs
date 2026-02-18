using EduCheck.Application.DTOs.Favorites;

namespace EduCheck.Application.Interfaces;

/// <summary>
/// Service interface for managing user's favorite institutes.
/// All methods require the userId to be extracted from JWT token (IDOR prevention).
/// </summary>
public interface IFavoritesService
{
    /// <summary>
    /// Gets the user's paginated list of favorite institutes.
    /// Results are cached for performance.
    /// </summary>
    Task<FavoritesResponse> GetUserFavoritesAsync(Guid userId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Adds an institute to the user's favorites.
    /// Invalidates the cache after adding.
    /// </summary>
    Task<AddFavoriteResponse> AddFavoriteAsync(Guid userId, int instituteId);

    /// <summary>
    /// Removes an institute from the user's favorites.
    /// Invalidates the cache after removing.
    /// </summary>
    Task<RemoveFavoriteResponse> RemoveFavoriteAsync(Guid userId, int instituteId);

    /// <summary>
    /// Checks if an institute is in the user's favorites.
    /// </summary>
    Task<FavoriteStatusResponse> GetFavoriteStatusAsync(Guid userId, int instituteId);
}