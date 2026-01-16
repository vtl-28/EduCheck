using EduCheck.Application.DTOs.SearchHistory;
using EduCheck.Application.Interfaces;
using EduCheck.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduCheck.API.Controllers;

[ApiController]
[Route("api/search-history")]
[Authorize]
public class SearchHistoryController : ControllerBase
{
    // adding comment to trigger ci/cd pipeline

    private readonly ISearchHistoryService _searchHistoryService;
    private readonly ILogger<SearchHistoryController> _logger;
    public SearchHistoryController(
        ISearchHistoryService searchHistoryService,
        ILogger<SearchHistoryController> logger)
    {
        _searchHistoryService = searchHistoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's search history
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SearchHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSearchHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new SearchHistoryResponse
            {
                Success = false,
                Message = "User not authenticated",
                Errors = new List<string> { "Unable to identify current user" }
            });
        }

        var result = await _searchHistoryService.GetUserSearchHistoryAsync(userId.Value, page, pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Delete a specific search history entry
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(DeleteSearchHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DeleteSearchHistoryResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteSearchHistoryEntry(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new DeleteSearchHistoryResponse
            {
                Success = false,
                Message = "User not authenticated",
                Errors = new List<string> { "Unable to identify current user" }
            });
        }

        if (id <= 0)
        {
            return BadRequest(new DeleteSearchHistoryResponse
            {
                Success = false,
                Message = "Invalid history entry ID",
                Errors = new List<string> { "History entry ID must be a positive number" }
            });
        }

        var result = await _searchHistoryService.DeleteSearchHistoryEntryAsync(userId.Value, id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Clear all search history for current user
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(DeleteSearchHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearSearchHistory()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new DeleteSearchHistoryResponse
            {
                Success = false,
                Message = "User not authenticated",
                Errors = new List<string> { "Unable to identify current user" }
            });
        }

        var result = await _searchHistoryService.ClearUserSearchHistoryAsync(userId.Value);

        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}