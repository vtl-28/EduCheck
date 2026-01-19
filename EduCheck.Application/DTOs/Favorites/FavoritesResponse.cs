using EduCheck.Application.DTOs.Institute;

namespace EduCheck.Application.DTOs.Favorites;


/// <summary>
/// Response for getting user's favorites list.
/// </summary>
public class FavoritesResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public FavoritesData? Data { get; set; }
    public List<string>? Errors { get; set; }
}

/// <summary>
/// Data container for favorites list.
/// </summary>
public class FavoritesData
{
    public List<FavoriteInstituteDto> Favorites { get; set; } = new();
    public PaginationDto Pagination { get; set; } = null!;
}

/// <summary>
/// Response for adding a favorite.
/// </summary>
public class AddFavoriteResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public FavoriteInstituteDto? Data { get; set; }
    public List<string>? Errors { get; set; }
}

/// <summary>
/// Response for removing a favorite.
/// </summary>
public class RemoveFavoriteResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }
}

/// <summary>
/// Response for checking favorite status.
/// </summary>
public class FavoriteStatusResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public FavoriteStatusDto? Data { get; set; }
    public List<string>? Errors { get; set; }
}