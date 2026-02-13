using EduCheck.Application.DTOs.SearchHistory;
using EduCheck.IntegrationTests.Fixtures;
using EduCheck.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Xunit;

namespace EduCheck.IntegrationTests.Tests;

public class SearchHistoryIntegrationTests : IClassFixture<EduCheckWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly EduCheckWebApplicationFactory _factory;

    public SearchHistoryIntegrationTests(EduCheckWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private async Task<(string AccessToken, string RefreshToken, Guid UserId)>
        AuthenticateNewStudentAsync()
    {
        var result = await TestAuthHelper.RegisterStudentAndLoginAsync(_client);
        _client.SetBearerToken(result.AccessToken);
        return result;
    }

    /// <summary>
    /// Generates search history by searching institutes using accreditation numbers.
    /// Seeded institutes have accreditation numbers "16 TEST 00001" through "16 TEST 00020".
    /// Using accreditation numbers triggers exact match, finding exactly 1 institute
    /// per search = 1 history entry per search.
    ///
    /// IMPORTANT: Do NOT use queries like "Test Institute 001" — the
    /// IsAccreditationNumber() method detects digits + spaces and routes to exact
    /// AccreditationNumber match. Must use the exact format from TestDataSeeder.
    /// </summary>
    private async Task GenerateSearchHistoryAsync(HttpClient client, int count = 3)
    {
        for (var i = 1; i <= count; i++)
        {
            // "16 TEST 00001", "16 TEST 00002", etc. - exact format from seeder
            await client.GetAsync($"/api/Institutes/search?query=16 TEST {i:D5}");
        }
    }

    // =========================================================================
    // Authentication
    // =========================================================================

    [Fact]
    public async Task GetHistory_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _client.ClearAuthentication();

        // Act
        var response = await _client.GetAsync("/api/search-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteHistoryEntry_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _client.ClearAuthentication();

        // Act
        var response = await _client.DeleteAsync("/api/search-history/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAllHistory_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _client.ClearAuthentication();

        // Act
        var response = await _client.DeleteAsync("/api/search-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Get History
    // =========================================================================

    [Fact]
    public async Task GetHistory_NewUser_ReturnsEmptyList()
    {
        // Arrange
        await AuthenticateNewStudentAsync();

        // Act
        var response = await _client.GetAsync("/api/search-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<SearchHistoryResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.History.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHistory_AfterSearchingInstitutes_ReturnsHistoryEntries()
    {
        // Arrange
        var (_, _, userId) = await AuthenticateNewStudentAsync();

        // Use actual accreditation numbers from seeder - each finds exactly 1 institute
        await _client.GetAsync("/api/Institutes/search?query=16 TEST 00001");
        await _client.GetAsync("/api/Institutes/search?query=16 TEST 00002");
        await _client.GetAsync("/api/Institutes/search?query=16 TEST 00003");

        // Act
        var response = await _client.GetAsync("/api/search-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<SearchHistoryResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.History.Should().HaveCount(3);

        // Verify database matches
        await using var dbContext = _factory.CreateDbContext();
        var dbHistory = await dbContext.InstituteSearchHistory
            .Where(h => h.UserId == userId)
            .ToListAsync();

        dbHistory.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetHistory_ReturnsCorrectInstituteData()
    {
        // Arrange
        await AuthenticateNewStudentAsync();

        // Search by accreditation number - finds exactly 1 institute
        await _client.GetAsync("/api/Institutes/search?query=16 TEST 00001");

        // Act
        var response = await _client.GetAsync("/api/search-history");

        // Assert
        var result = await response.ReadAsJsonAsync<SearchHistoryResponse>();
        result.Should().NotBeNull();
        result!.Data!.History.Should().HaveCount(1);

        var entry = result.Data.History.First();
        entry.Id.Should().BeGreaterThan(0);
        entry.SearchedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30));

        // SearchHistoryDto nests institute data inside Institute property
        entry.Institute.Should().NotBeNull();
        entry.Institute.InstitutionName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetHistory_ReturnsPaginationMetadata()
    {
        // Arrange
        await AuthenticateNewStudentAsync();
        await GenerateSearchHistoryAsync(_client, 5);

        // Act - request page 1 with pageSize 2
        var response = await _client.GetAsync("/api/search-history?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<SearchHistoryResponse>();
        result.Should().NotBeNull();
        result!.Data!.History.Should().HaveCount(2);
        result.Data.Pagination.Should().NotBeNull();
        result.Data.Pagination.TotalCount.Should().Be(5);
        result.Data.Pagination.TotalPages.Should().Be(3);
        result.Data.Pagination.HasNextPage.Should().BeTrue();
        result.Data.Pagination.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetHistory_SecondPage_ReturnsCorrectEntries()
    {
        // Arrange
        await AuthenticateNewStudentAsync();
        await GenerateSearchHistoryAsync(_client, 4);

        // Act
        var page1Response = await _client.GetAsync("/api/search-history?page=1&pageSize=2");
        var page2Response = await _client.GetAsync("/api/search-history?page=2&pageSize=2");

        // Assert
        var page1 = await page1Response.ReadAsJsonAsync<SearchHistoryResponse>();
        var page2 = await page2Response.ReadAsJsonAsync<SearchHistoryResponse>();

        page1!.Data!.History.Should().HaveCount(2);
        page2!.Data!.History.Should().HaveCount(2);

        // Pages should contain different entries
        var page1Ids = page1.Data.History.Select(h => h.Id).ToList();
        var page2Ids = page2.Data.History.Select(h => h.Id).ToList();
        page1Ids.Should().NotIntersectWith(page2Ids);
    }

    // =========================================================================
    // Delete Single Entry - DELETE /api/search-history/{id}
    // =========================================================================

    [Fact]
    public async Task DeleteHistoryEntry_OwnEntry_ReturnsSuccessAndRemovesFromDatabase()
    {
        // Arrange
        var (_, _, userId) = await AuthenticateNewStudentAsync();

        // Search by accreditation number records exactly 1 history entry
        await _client.GetAsync("/api/Institutes/search?query=16 TEST 00001");

        // Get the history entry ID from the list
        var listResponse = await _client.GetAsync("/api/search-history");
        var list = await listResponse.ReadAsJsonAsync<SearchHistoryResponse>();

        list.Should().NotBeNull();
        list!.Data!.History.Should().NotBeEmpty("search should have recorded a history entry");

        var entryId = list.Data.History.First().Id;

        // Act
        var response = await _client.DeleteAsync($"/api/search-history/{entryId}");

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert removed from database
        await using var dbContext = _factory.CreateDbContext();
        var entry = await dbContext.InstituteSearchHistory
            .FirstOrDefaultAsync(h => h.Id == entryId && h.UserId == userId);

        entry.Should().BeNull();
    }

    [Fact]
    public async Task DeleteHistoryEntry_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateNewStudentAsync();

        // Act
        var response = await _client.DeleteAsync("/api/search-history/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteHistoryEntry_AnotherUsersEntry_ReturnsNotFound()
    {
        // Arrange - User 1 generates a history entry via search
        var client1 = _factory.CreateClient();
        var (token1, _, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(client1);
        client1.SetBearerToken(token1);
        await client1.GetAsync("/api/Institutes/search?query=16 TEST 00001");

        // Get User 1's entry ID
        var listResponse = await client1.GetAsync("/api/search-history");
        var list = await listResponse.ReadAsJsonAsync<SearchHistoryResponse>();
        list!.Data!.History.Should().NotBeEmpty();
        var entryId = list.Data.History.First().Id;

        // User 2 tries to delete User 1's entry
        var client2 = _factory.CreateClient();
        var (token2, _, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(client2);
        client2.SetBearerToken(token2);

        // Act
        var response = await client2.DeleteAsync($"/api/search-history/{entryId}");

        // Assert - User 2 cannot delete User 1's entry
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify User 1's entry still exists
        await using var dbContext = _factory.CreateDbContext();
        var entry = await dbContext.InstituteSearchHistory
            .FirstOrDefaultAsync(h => h.Id == entryId);

        entry.Should().NotBeNull();
    }

    // =========================================================================
    // Delete All History - DELETE /api/search-history
    // =========================================================================

    [Fact]
    public async Task DeleteAllHistory_WithEntries_ReturnsSuccessAndClearsDatabase()
    {
        // Arrange
        var (_, _, userId) = await AuthenticateNewStudentAsync();
        await GenerateSearchHistoryAsync(_client, 5);

        // Verify entries exist before delete
        await using var dbContextBefore = _factory.CreateDbContext();
        var beforeCount = await dbContextBefore.InstituteSearchHistory
            .CountAsync(h => h.UserId == userId);
        beforeCount.Should().Be(5);

        // Act
        var response = await _client.DeleteAsync("/api/search-history");

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<DeleteSearchHistoryResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.DeletedCount.Should().Be(5);

        // Assert all entries removed from database
        await using var dbContextAfter = _factory.CreateDbContext();
        var afterCount = await dbContextAfter.InstituteSearchHistory
            .CountAsync(h => h.UserId == userId);
        afterCount.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAllHistory_WithNoEntries_ReturnsSuccessWithZeroDeleted()
    {
        // Arrange
        await AuthenticateNewStudentAsync();

        // Act - delete when nothing exists
        var response = await _client.DeleteAsync("/api/search-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<DeleteSearchHistoryResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.DeletedCount.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAllHistory_OnlyDeletesOwnEntries()
    {
        // Arrange - two users both generate history using different accreditation numbers
        // to avoid the 24-hour deduplication in RecordSearchAsync
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        var (token1, _, userId1) = await TestAuthHelper.RegisterStudentAndLoginAsync(client1);
        client1.SetBearerToken(token1);
        await GenerateSearchHistoryAsync(client1, 3);  // 16 TEST 00001-00003

        var (token2, _, userId2) = await TestAuthHelper.RegisterStudentAndLoginAsync(client2);
        client2.SetBearerToken(token2);
        // Use different accreditation numbers so deduplication doesn't interfere
        await client2.GetAsync("/api/Institutes/search?query=16 TEST 00004");
        await client2.GetAsync("/api/Institutes/search?query=16 TEST 00005");
        await client2.GetAsync("/api/Institutes/search?query=16 TEST 00006");

        // Act - User 1 deletes all their history
        await client1.DeleteAsync("/api/search-history");

        // Assert - User 1's history is gone
        await using var dbContext = _factory.CreateDbContext();

        var user1History = await dbContext.InstituteSearchHistory
            .CountAsync(h => h.UserId == userId1);
        user1History.Should().Be(0);

        // Assert - User 2's history is untouched
        var user2History = await dbContext.InstituteSearchHistory
            .CountAsync(h => h.UserId == userId2);
        user2History.Should().Be(3);
    }

    // =========================================================================
    // End-to-End Workflow
    // =========================================================================

    [Fact]
    public async Task SearchHistoryWorkflow_SearchDeleteSingleDeleteAll_WorksEndToEnd()
    {
        // Arrange
        var (_, _, userId) = await AuthenticateNewStudentAsync();

        // Step 1: Verify empty history
        var emptyResponse = await _client.GetAsync("/api/search-history");
        var empty = await emptyResponse.ReadAsJsonAsync<SearchHistoryResponse>();
        empty!.Data!.History.Should().BeEmpty();

        // Step 2: Search 4 institutes using accreditation numbers to generate history
        await _client.GetAsync("/api/Institutes/search?query=16 TEST 00001");
        await _client.GetAsync("/api/Institutes/search?query=16 TEST 00002");
        await _client.GetAsync("/api/Institutes/search?query=16 TEST 00003");
        await _client.GetAsync("/api/Institutes/search?query=16 TEST 00004");

        // Step 3: Verify 4 entries in history
        var listResponse = await _client.GetAsync("/api/search-history");
        var list = await listResponse.ReadAsJsonAsync<SearchHistoryResponse>();
        list!.Data!.History.Should().HaveCount(4);

        // Step 4: Delete one entry
        var entryToDelete = list.Data.History.First().Id;
        var deleteOneResponse = await _client.DeleteAsync($"/api/search-history/{entryToDelete}");
        deleteOneResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: Verify 3 entries remain
        var afterDeleteOne = await _client.GetAsync("/api/search-history");
        var remaining = await afterDeleteOne.ReadAsJsonAsync<SearchHistoryResponse>();
        remaining!.Data!.History.Should().HaveCount(3);
        remaining.Data.History.Select(h => h.Id).Should().NotContain(entryToDelete);

        // Step 6: Delete all remaining
        var deleteAllResponse = await _client.DeleteAsync("/api/search-history");
        var deleteAllResult = await deleteAllResponse.ReadAsJsonAsync<DeleteSearchHistoryResponse>();
        deleteAllResult!.DeletedCount.Should().Be(3);

        // Step 7: Verify database is empty for this user
        await using var dbContext = _factory.CreateDbContext();
        var dbCount = await dbContext.InstituteSearchHistory
            .CountAsync(h => h.UserId == userId);
        dbCount.Should().Be(0);
    }
}