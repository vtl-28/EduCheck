using EduCheck.Application.DTOs.Institute;
using EduCheck.IntegrationTests.Fixtures;
using EduCheck.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Xunit;

namespace EduCheck.IntegrationTests.Tests;

public class InstituteIntegrationTests : IClassFixture<EduCheckWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly EduCheckWebApplicationFactory _factory;

    public InstituteIntegrationTests(EduCheckWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // =========================================================================
    // Helper: authenticate client
    // =========================================================================

    private async Task AuthenticateAsync()
    {
        var (accessToken, _, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(_client);
        _client.SetBearerToken(accessToken);
    }

    // =========================================================================
    // Search - Authentication
    // =========================================================================

    [Fact]
    public async Task Search_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _client.ClearAuthentication();

        // Act
        var response = await _client.GetAsync("/api/Institutes/search?query=Test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Search - Validation
    // =========================================================================

    [Fact]
    public async Task Search_WithEmptyQuery_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/Institutes/search?query=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_WithSingleCharacterQuery_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/Institutes/search?query=A");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.ReadAsJsonAsync<InstituteSearchResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Search_WithQueryExceeding255Characters_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();
        var longQuery = new string('A', 256); // 256 characters

        // Act
        var response = await _client.GetAsync($"/api/Institutes/search?query={longQuery}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Search - Results
    // =========================================================================

    [Fact]
    public async Task Search_WithValidQuery_ReturnsMatchingInstitutes()
    {
        // Arrange
        await AuthenticateAsync();

        // "Test" matches all 20 seeded institutes ("Test Institute 001" etc.)

        // Act
        var response = await _client.GetAsync("/api/Institutes/search?query=Test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<InstituteSearchResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Institutes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Search_WithNoMatchingQuery_ReturnsEmptyResults()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/Institutes/search?query=ZZZNOMATCH");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<InstituteSearchResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Institutes.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_WithProvinceFilter_ReturnsFilteredResults()
    {
        // Arrange
        await AuthenticateAsync();

        // Seeded data has institutes with Province = "Gauteng"

        // Act
        var response = await _client.GetAsync(
            "/api/Institutes/search?query=Test&province=Gauteng");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<InstituteSearchResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Institutes.Should().NotBeEmpty();
        result.Data.Institutes.Should().AllSatisfy(i =>
            i.Province.Should().Be("Gauteng"));
    }

    [Fact]
    public async Task Search_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await AuthenticateAsync();

        // Act - request page 1 with pageSize 5
        var page1Response = await _client.GetAsync(
            "/api/Institutes/search?query=Test&page=1&pageSize=5");

        // Act - request page 2 with pageSize 5
        var page2Response = await _client.GetAsync(
            "/api/Institutes/search?query=Test&page=2&pageSize=5");

        // Assert
        page1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        page2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page1 = await page1Response.ReadAsJsonAsync<InstituteSearchResponse>();
        var page2 = await page2Response.ReadAsJsonAsync<InstituteSearchResponse>();

        page1!.Data!.Institutes.Should().HaveCount(5);
        page2!.Data!.Institutes.Should().NotBeEmpty();

        // Pages should contain different institutes
        var page1Ids = page1.Data.Institutes.Select(i => i.Id).ToList();
        var page2Ids = page2.Data.Institutes.Select(i => i.Id).ToList();
        page1Ids.Should().NotIntersectWith(page2Ids);
    }

    [Fact]
    public async Task Search_RecordsSearchHistoryInDatabase()
    {
        // Arrange
        var (accessToken, _, userId) = await TestAuthHelper.RegisterStudentAndLoginAsync(_client);
        _client.SetBearerToken(accessToken);

        // Act
        var response = await _client.GetAsync("/api/Institutes/search?query=Test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify search history recorded in database
        await using var dbContext = _factory.CreateDbContext();
        var history = await dbContext.InstituteSearchHistory
            .Where(h => h.UserId == userId)
            .ToListAsync();

        history.Should().NotBeEmpty();
    }

    // =========================================================================
    // GetById
    // =========================================================================

    [Fact]
    public async Task GetById_WithValidId_ReturnsInstituteDetails()
    {
        // Arrange
        await AuthenticateAsync();

        // Act - Institute with Id = 1 was seeded in DatabaseFixture
        var response = await _client.GetAsync("/api/Institutes/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<InstituteDetailResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(1);
        result.Data.InstitutionName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync();

        // Act - ID 99999 doesn't exist in seeded data
        var response = await _client.GetAsync("/api/Institutes/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        // Act - negative ID is invalid
        var response = await _client.GetAsync("/api/Institutes/-1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _client.ClearAuthentication();

        // Act
        var response = await _client.GetAsync("/api/Institutes/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}