namespace EduCheck.Application.DTOs;

/// <summary>
/// Generic paginated response wrapper
/// Contains the data plus pagination metadata
/// </summary>
public class PaginatedResponse<T>
{
    /// <summary>
    /// The actual data items for current page
    /// </summary>
    public List<T> Data { get; set; } = new();

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there are more pages after this one
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there are pages before this one
    /// </summary>
    public bool HasPreviousPage { get; set; }
}