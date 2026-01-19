using EduCheck.Application.DTOs.Institute;

namespace EduCheck.Application.DTOs.Favorites;

/// <summary>
/// DTO for a favorite institute entry.
/// Contains full institute data for display in the frontend.
/// </summary>
public class FavoriteInstituteDto
{
    /// <summary>
    /// The favorite entry ID (for deletion).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// When the institute was added to favorites.
    /// </summary>
    public DateTime FavoritedAt { get; set; }

    /// <summary>
    /// Full institute data for display.
    /// </summary>
    public InstituteDto Institute { get; set; } = null!;
}


/// <summary>
/// Response for checking if an institute is favorited.
/// </summary>
public class FavoriteStatusDto
{
    /// <summary>
    /// Whether the institute is in the user's favorites.
    /// </summary>
    public bool IsFavorited { get; set; }

    /// <summary>
    /// The favorite entry ID (null if not favorited).
    /// </summary>
    public int? FavoriteId { get; set; }

    /// <summary>
    /// When it was favorited (null if not favorited).
    /// </summary>
    public DateTime? FavoritedAt { get; set; }
}

/// <summary>
/// Pagination DTO for favorites list.
/// </summary>