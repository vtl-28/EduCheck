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


    private async Task<(string AccessToken, string RefreshToken, Guid UserId)> AuthenticateNewStudentAsync()
    {
        var result = await TestAuthHelper.RegisterStudentAndLoginAsync(_client);
        _client.SetBearerToken(result.AccessToken);
        return result;
    }

    private async Task<Guid> GetStudentIdAsync(Guid userId)
    {
        await using var dbContext = _factory.CreateDbContext();
        var student = await dbContext.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        student.Should().NotBeNull($"Student record should exist for UserId {userId}");
        return student!.Id;
    }

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


    [Fact]
    public async Task AddFavorite_WithValidInstituteId_ReturnsSuccessAndPersistsToDatabase()
    {
        // Arrange
        var (_, _, userId) = await AuthenticateNewStudentAsync();
        var studentId = await GetStudentIdAsync(userId);
        var instituteId = 1;

        // Act
        var response = await _client.PostAsync($"/api/Favorites/{instituteId}", null);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.ReadAsJsonAsync<AddFavoriteResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

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
        var instituteId = 2;

        await _client.PostAsync($"/api/Favorites/{instituteId}", null);

        // Act
        var response = await _client.GetAsync("/api/Favorites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<FavoritesResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Favorites.Should().HaveCount(1);

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

        await _client.PostAsync($"/api/Favorites/{instituteId}", null);

        // Act - add same institute again
        var response = await _client.PostAsync($"/api/Favorites/{instituteId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var result = await response.ReadAsJsonAsync<AddFavoriteResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }


    [Fact]
    public async Task RemoveFavorite_ExistingFavorite_ReturnsSuccessAndDeletesFromDatabase()
    {
        // Arrange
        var (_, _, userId) = await AuthenticateNewStudentAsync();
        var studentId = await GetStudentIdAsync(userId);
        var instituteId = 4;

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
        result.Data!.IsFavorited.Should().BeFalse();
        result.Data.FavoritedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetFavorites_TwoUsers_EachSeesOnlyOwnFavorites()
    {
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        var (token1, _, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(client1);
        client1.SetBearerToken(token1);

        var (token2, _, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(client2);
        client2.SetBearerToken(token2);

        var add1 = await client1.PostAsync("/api/Favorites/8", null);
        add1.StatusCode.Should().Be(HttpStatusCode.Created);
        var add2 = await client2.PostAsync("/api/Favorites/9", null);
        add2.StatusCode.Should().Be(HttpStatusCode.Created);

        var student1Response = await client1.GetAsync("/api/Favorites");
        var student2Response = await client2.GetAsync("/api/Favorites");

        student1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        student2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        var student1Favorites = await student1Response.ReadAsJsonAsync<FavoritesResponse>();
        var student2Favorites = await student2Response.ReadAsJsonAsync<FavoritesResponse>();

        student1Favorites.Should().NotBeNull();
        student2Favorites.Should().NotBeNull();

        student1Favorites!.Data!.Favorites.Should().HaveCount(1);
        student1Favorites.Data.Favorites.First().Institute.Id.Should().Be(8);

        student2Favorites!.Data!.Favorites.Should().HaveCount(1);
        student2Favorites.Data.Favorites.First().Institute.Id.Should().Be(9);
    }

    [Fact]
    public async Task RemoveFavorite_AnotherUsersFavorite_ReturnsNotFound()
    {
        var (token1, _, userId1) = await TestAuthHelper.RegisterStudentAndLoginAsync(_client);
        var studentId1 = await GetStudentIdAsync(userId1);

        _client.SetBearerToken(token1);
        await _client.PostAsync("/api/Favorites/10", null);

        var (token2, _, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(_client);
        _client.SetBearerToken(token2);

        var response = await _client.DeleteAsync("/api/Favorites/10");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await using var dbContext = _factory.CreateDbContext();
        var favorite = await dbContext.FavoriteInstitutes
            .FirstOrDefaultAsync(f => f.StudentId == studentId1 && f.InstituteId == 10);

        favorite.Should().NotBeNull();
    }

    [Fact]
    public async Task FavoritesWorkflow_AddMultipleCheckStatusRemoveVerify_WorksEndToEnd()
    {
        // Arrange
        var (_, _, userId) = await AuthenticateNewStudentAsync();
        var studentId = await GetStudentIdAsync(userId);

        await _client.PostAsync("/api/Favorites/11", null);
        await _client.PostAsync("/api/Favorites/12", null);
        await _client.PostAsync("/api/Favorites/13", null);

        var listResponse = await _client.GetAsync("/api/Favorites");
        var list = await listResponse.ReadAsJsonAsync<FavoritesResponse>();
        list!.Data!.Favorites.Should().HaveCount(3);

        var status11 = await _client.GetAsync("/api/Favorites/11/status");
        var status12 = await _client.GetAsync("/api/Favorites/12/status");
        var status13 = await _client.GetAsync("/api/Favorites/13/status");

        (await status11.ReadAsJsonAsync<FavoriteStatusResponse>())!.Data!.IsFavorited.Should().BeTrue();
        (await status12.ReadAsJsonAsync<FavoriteStatusResponse>())!.Data!.IsFavorited.Should().BeTrue();
        (await status13.ReadAsJsonAsync<FavoriteStatusResponse>())!.Data!.IsFavorited.Should().BeTrue();

        await _client.DeleteAsync("/api/Favorites/12");

        var updatedList = await _client.GetAsync("/api/Favorites");
        var updated = await updatedList.ReadAsJsonAsync<FavoritesResponse>();
        updated!.Data!.Favorites.Should().HaveCount(2);

        var removedStatus = await _client.GetAsync("/api/Favorites/12/status");
        (await removedStatus.ReadAsJsonAsync<FavoriteStatusResponse>())!.Data!.IsFavorited.Should().BeFalse();

        await using var dbContext = _factory.CreateDbContext();
        var dbFavorites = await dbContext.FavoriteInstitutes
            .Where(f => f.StudentId == studentId)
            .ToListAsync();

        dbFavorites.Should().HaveCount(2);
        dbFavorites.Select(f => f.InstituteId).Should().Contain(new[] { 11, 13 });
        dbFavorites.Select(f => f.InstituteId).Should().NotContain(12);
    }
}