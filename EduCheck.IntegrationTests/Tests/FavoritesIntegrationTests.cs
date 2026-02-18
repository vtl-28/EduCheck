using EduCheck.Application.DTOs.Favorites;
using EduCheck.IntegrationTests.Fixtures;
using EduCheck.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Xunit;

namespace EduCheck.IntegrationTests.Tests;

public class FavoritesIntegrationTests : IClassFixture<EduCheckWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly EduCheckWebApplicationFactory _factory;

    public FavoritesIntegrationTests(EduCheckWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // =========================================================================
    // Helper: register and authenticate a fresh student per test
    // =========================================================================

    private async Task<(string AccessToken, string RefreshToken, Guid UserId)> AuthenticateNewStudentAsync()
    {
        var result = await TestAuthHelper.RegisterStudentAndLoginAsync(_client);
        _client.SetBearerToken(result.AccessToken);
        return result;
    }

    // Helper: get StudentId from UserId via database
    private async Task<Guid> GetStudentIdAsync(Guid userId)
    {
        await using var dbContext = _factory.CreateDbContext();
        var student = await dbContext.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        student.Should().NotBeNull($"Student record should exist for UserId {userId}");
        return student!.Id;
    }

    // =========================================================================
    // Authentication
    // =========================================================================

    [Fact]
    public async Task GetFavorites_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _client.ClearAuthentication();

        // Act
        var response = await _client.GetAsync("/api/Favorites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddFavorite_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _client.ClearAuthentication();

        // Act
        var response = await _client.PostAsync("/api/Favorites/1", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveFavorite_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _client.ClearAuthentication();

        // Act
        var response = await _client.DeleteAsync("/api/Favorites/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Get Favorites - Empty List
    // =========================================================================

    [Fact]
    public async Task GetFavorites_NewUser_ReturnsEmptyList()
    {
        // Arrange
        await AuthenticateNewStudentAsync();

        // Act
        var response = await _client.GetAsync("/api/Favorites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<FavoritesResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Favorites.Should().BeEmpty();
    }

    // =========================================================================
    // Add Favorite
    // =========================================================================

    [Fact]
    public async Task AddFavorite_WithValidInstituteId_ReturnsSuccessAndPersistsToDatabase()
    {
        // Arrange
        var (_, _, userId) = await AuthenticateNewStudentAsync();
        var studentId = await GetStudentIdAsync(userId);
        var instituteId = 1; // Seeded in DatabaseFixture

        // Act
        var response = await _client.PostAsync($"/api/Favorites/{instituteId}", null);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.ReadAsJsonAsync<AddFavoriteResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        // Assert database record created
        // FavoriteInstitute entity uses StudentId (not UserId directly)
        await using var dbContext = _factory.CreateDbContext();
        var favorite = await dbContext.FavoriteInstitutes
            .FirstOrDefaultAsync(f => f.StudentId == studentId && f.InstituteId == instituteId);

        favorite.Should().NotBeNull();
        favorite!.StudentId.Should().Be(studentId);
        favorite.InstituteId.Should().Be(instituteId);
        favorite.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task AddFavorite_ThenGetFavorites_ReturnsFavoriteInList()
    {
        // Arrange
        await AuthenticateNewStudentAsync();
        var instituteId = 2; // Seeded in DatabaseFixture

        // Add favorite
        await _client.PostAsync($"/api/Favorites/{instituteId}", null);

        // Act
        var response = await _client.GetAsync("/api/Favorites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<FavoritesResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Favorites.Should().HaveCount(1);

        // FavoriteInstituteDto has Institute.Id (not InstituteId directly)
        result.Data.Favorites.First().Institute.Id.Should().Be(instituteId);
    }

    [Fact]
    public async Task AddFavorite_WithNonExistentInstituteId_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateNewStudentAsync();
        var nonExistentId = 99999;

        // Act
        var response = await _client.PostAsync($"/api/Favorites/{nonExistentId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddFavorite_Duplicate_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateNewStudentAsync();
        var instituteId = 3;

        // Add first time
        await _client.PostAsync($"/api/Favorites/{instituteId}", null);

        // Act - add same institute again
        var response = await _client.PostAsync($"/api/Favorites/{instituteId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var result = await response.ReadAsJsonAsync<AddFavoriteResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    // =========================================================================
    // Remove Favorite
    // =========================================================================

    [Fact]
    public async Task RemoveFavorite_ExistingFavorite_ReturnsSuccessAndDeletesFromDatabase()
    {
        // Arrange
        var (_, _, userId) = await AuthenticateNewStudentAsync();
        var studentId = await GetStudentIdAsync(userId);
        var instituteId = 4;

        // Add then remove
        await _client.PostAsync($"/api/Favorites/{instituteId}", null);

        // Act
        var response = await _client.DeleteAsync($"/api/Favorites/{instituteId}");

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<RemoveFavoriteResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        // Assert deleted from database
        await using var dbContext = _factory.CreateDbContext();
        var favorite = await dbContext.FavoriteInstitutes
            .FirstOrDefaultAsync(f => f.StudentId == studentId && f.InstituteId == instituteId);

        favorite.Should().BeNull();
    }

    [Fact]
    public async Task RemoveFavorite_NonExistentFavorite_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateNewStudentAsync();

        // Act - try to remove institute that was never favorited
        var response = await _client.DeleteAsync("/api/Favorites/5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Favorite Status
    // =========================================================================

    [Fact]
    public async Task GetFavoriteStatus_AfterAdding_ReturnsIsFavoritedTrue()
    {
        // Arrange
        await AuthenticateNewStudentAsync();
        var instituteId = 6;

        // Add to favorites
        await _client.PostAsync($"/api/Favorites/{instituteId}", null);

        // Act
        var response = await _client.GetAsync($"/api/Favorites/{instituteId}/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<FavoriteStatusResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        // IsFavorited is nested inside Data
        result.Data!.IsFavorited.Should().BeTrue();
        result.Data.FavoritedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFavoriteStatus_WithoutAdding_ReturnsIsFavoritedFalse()
    {
        // Arrange
        await AuthenticateNewStudentAsync();
        var instituteId = 7;

        // Act - check status without adding
        var response = await _client.GetAsync($"/api/Favorites/{instituteId}/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<FavoriteStatusResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        // IsFavorited is nested inside Data
        result.Data!.IsFavorited.Should().BeFalse();
        result.Data.FavoritedAt.Should().BeNull();
    }

    // =========================================================================
    // IDOR Security Tests
    // =========================================================================

    [Fact]
    public async Task GetFavorites_TwoUsers_EachSeesOnlyOwnFavorites()
    {
        // Use separate fresh clients to avoid token/state leakage between users
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        // Register and authenticate Student 1
        var (token1, _, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(client1);
        client1.SetBearerToken(token1);

        // Register and authenticate Student 2
        var (token2, _, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(client2);
        client2.SetBearerToken(token2);

        // Student 1 adds institute 8
        var add1 = await client1.PostAsync("/api/Favorites/8", null);
        add1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Student 2 adds institute 9
        var add2 = await client2.PostAsync("/api/Favorites/9", null);
        add2.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - each student fetches their own favorites
        var student1Response = await client1.GetAsync("/api/Favorites");
        var student2Response = await client2.GetAsync("/api/Favorites");

        // Assert
        student1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        student2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        var student1Favorites = await student1Response.ReadAsJsonAsync<FavoritesResponse>();
        var student2Favorites = await student2Response.ReadAsJsonAsync<FavoritesResponse>();

        student1Favorites.Should().NotBeNull();
        student2Favorites.Should().NotBeNull();

        // Each student sees only their own favorite
        student1Favorites!.Data!.Favorites.Should().HaveCount(1);
        student1Favorites.Data.Favorites.First().Institute.Id.Should().Be(8);

        student2Favorites!.Data!.Favorites.Should().HaveCount(1);
        student2Favorites.Data.Favorites.First().Institute.Id.Should().Be(9);
    }

    [Fact]
    public async Task RemoveFavorite_AnotherUsersFavorite_ReturnsNotFound()
    {
        // Arrange - Student 1 adds a favorite
        var (token1, _, userId1) = await TestAuthHelper.RegisterStudentAndLoginAsync(_client);
        var studentId1 = await GetStudentIdAsync(userId1);

        _client.SetBearerToken(token1);
        await _client.PostAsync("/api/Favorites/10", null);

        // Student 2 tries to delete Student 1's favorite
        var (token2, _, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(_client);
        _client.SetBearerToken(token2);

        // Act
        var response = await _client.DeleteAsync("/api/Favorites/10");

        // Assert - Student 2 can't delete Student 1's favorite
        // Returns NotFound because the favorite doesn't exist for Student 2
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify Student 1's favorite still exists in the database
        await using var dbContext = _factory.CreateDbContext();
        var favorite = await dbContext.FavoriteInstitutes
            .FirstOrDefaultAsync(f => f.StudentId == studentId1 && f.InstituteId == 10);

        favorite.Should().NotBeNull(); // Still exists
    }

    // =========================================================================
    // End-to-End Favorites Workflow
    // =========================================================================

    [Fact]
    public async Task FavoritesWorkflow_AddMultipleCheckStatusRemoveVerify_WorksEndToEnd()
    {
        // Arrange
        var (_, _, userId) = await AuthenticateNewStudentAsync();
        var studentId = await GetStudentIdAsync(userId);

        // Step 1: Add 3 favorites
        await _client.PostAsync("/api/Favorites/11", null);
        await _client.PostAsync("/api/Favorites/12", null);
        await _client.PostAsync("/api/Favorites/13", null);

        // Step 2: Verify all 3 are in the list
        var listResponse = await _client.GetAsync("/api/Favorites");
        var list = await listResponse.ReadAsJsonAsync<FavoritesResponse>();
        list!.Data!.Favorites.Should().HaveCount(3);

        // Step 3: Check status of each - IsFavorited is nested in Data
        var status11 = await _client.GetAsync("/api/Favorites/11/status");
        var status12 = await _client.GetAsync("/api/Favorites/12/status");
        var status13 = await _client.GetAsync("/api/Favorites/13/status");

        (await status11.ReadAsJsonAsync<FavoriteStatusResponse>())!.Data!.IsFavorited.Should().BeTrue();
        (await status12.ReadAsJsonAsync<FavoriteStatusResponse>())!.Data!.IsFavorited.Should().BeTrue();
        (await status13.ReadAsJsonAsync<FavoriteStatusResponse>())!.Data!.IsFavorited.Should().BeTrue();

        // Step 4: Remove one favorite
        await _client.DeleteAsync("/api/Favorites/12");

        // Step 5: Verify only 2 remain
        var updatedList = await _client.GetAsync("/api/Favorites");
        var updated = await updatedList.ReadAsJsonAsync<FavoritesResponse>();
        updated!.Data!.Favorites.Should().HaveCount(2);

        // Step 6: Verify removed one shows as not favorited
        var removedStatus = await _client.GetAsync("/api/Favorites/12/status");
        (await removedStatus.ReadAsJsonAsync<FavoriteStatusResponse>())!.Data!.IsFavorited.Should().BeFalse();

        // Step 7: Verify database state matches - uses StudentId not UserId
        await using var dbContext = _factory.CreateDbContext();
        var dbFavorites = await dbContext.FavoriteInstitutes
            .Where(f => f.StudentId == studentId)
            .ToListAsync();

        dbFavorites.Should().HaveCount(2);
        dbFavorites.Select(f => f.InstituteId).Should().Contain(new[] { 11, 13 });
        dbFavorites.Select(f => f.InstituteId).Should().NotContain(12);
    }
}