using EduCheck.Application.DTOs.FraudReport;
using EduCheck.Domain.Enums;
using EduCheck.IntegrationTests.Fixtures;
using EduCheck.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace EduCheck.IntegrationTests.Tests;

public class FraudReportIntegrationTests : IClassFixture<EduCheckWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly EduCheckWebApplicationFactory _factory;

    public FraudReportIntegrationTests(EduCheckWebApplicationFactory factory)
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

    private async Task<Guid> GetStudentIdAsync(Guid userId)
    {
        await using var dbContext = _factory.CreateDbContext();
        var student = await dbContext.Students
            .FirstOrDefaultAsync(s => s.UserId == userId);
        student.Should().NotBeNull();
        return student!.Id;
    }

    private static CreateFraudReportRequest ValidReportRequest(
        string instituteName = "Fake University SA",
        string description = "This institute is operating without accreditation and deceiving students into enrolling.")
        => new()
        {
            ReportedInstituteName = instituteName,
            ReportedInstituteAddress = "123 Fake Street, Johannesburg",
            ReportedInstitutePhone = "011-555-1234",
            Description = description
        };

    // =========================================================================
    // Authentication
    // =========================================================================

    [Fact]
    public async Task CreateReport_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _client.ClearAuthentication();

        // Act
        var response = await _client.PostAsJsonAsync("/api/fraud-reports", ValidReportRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyReports_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _client.ClearAuthentication();

        // Act
        var response = await _client.GetAsync("/api/fraud-reports");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReportById_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _client.ClearAuthentication();

        // Act
        var response = await _client.GetAsync($"/api/fraud-reports/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Create Report - Validation
    // =========================================================================

    [Fact]
    public async Task CreateReport_WithEmptyInstituteName_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateNewStudentAsync();

        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "",  // Required field - empty
            Description = "This institute is operating without accreditation and deceiving students."
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/fraud-reports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReport_WithShortDescription_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateNewStudentAsync();

        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "Fake University",
            Description = "Too short"  // Min 20 characters
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/fraud-reports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReport_WithInvalidPhoneNumber_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateNewStudentAsync();

        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "Fake University",
            ReportedInstitutePhone = "not-a-phone-number!!!", // Invalid - contains letters
            Description = "This institute is operating without accreditation and deceiving students."
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/fraud-reports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Create Report - Success
    // =========================================================================

    [Fact]
    public async Task CreateReport_WithValidData_ReturnsCreatedAndPersistsToDatabase()
    {
        // Arrange
        var (_, _, userId) = await AuthenticateNewStudentAsync();
        var studentId = await GetStudentIdAsync(userId);
        var request = ValidReportRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/fraud-reports", request);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.ReadAsJsonAsync<CreateFraudReportResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().NotBeEmpty();
        result.Data.ReportedInstituteName.Should().Be(request.ReportedInstituteName);
        result.Data.Description.Should().Be(request.Description);
        result.Data.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

        // Assert persisted to database
        await using var dbContext = _factory.CreateDbContext();
        var report = await dbContext.FraudReports
            .FirstOrDefaultAsync(r => r.StudentId == studentId
                && r.ReportedInstituteName == request.ReportedInstituteName);

        report.Should().NotBeNull();
        report!.StudentId.Should().Be(studentId);
        report.ReportedInstituteName.Should().Be(request.ReportedInstituteName);
        report.Description.Should().Be(request.Description);
        report.Status.Should().Be(FraudReportStatus.Submitted);  // Default status
        report.Severity.Should().Be(FraudSeverity.Medium);        // Default severity
        report.IsAnonymous.Should().BeFalse();                    // Default
    }

    [Fact]
    public async Task CreateReport_WithMinimalData_ReturnsCreated()
    {
        // Arrange
        await AuthenticateNewStudentAsync();

        // Only required fields - no address or phone
        var request = new CreateFraudReportRequest
        {
            ReportedInstituteName = "Minimal Fake College",
            Description = "This institute is operating without proper accreditation from DHET."
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/fraud-reports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.ReadAsJsonAsync<CreateFraudReportResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.ReportedInstituteAddress.Should().BeNull();
        result.Data.ReportedInstitutePhone.Should().BeNull();
    }

    // =========================================================================
    // Get My Reports
    // =========================================================================

    [Fact]
    public async Task GetMyReports_NewUser_ReturnsEmptyList()
    {
        // Arrange
        await AuthenticateNewStudentAsync();

        // Act
        var response = await _client.GetAsync("/api/fraud-reports");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<FraudReportsResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Reports.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyReports_AfterCreating_ReturnsReportInList()
    {
        // Arrange
        await AuthenticateNewStudentAsync();
        await _client.PostAsJsonAsync("/api/fraud-reports", ValidReportRequest());

        // Act
        var response = await _client.GetAsync("/api/fraud-reports");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<FraudReportsResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Reports.Should().HaveCount(1);
        result.Data.Reports.First().ReportedInstituteName
            .Should().Be("Fake University SA");
    }

    [Fact]
    public async Task GetMyReports_MultipleReports_ReturnsAllInList()
    {
        // Arrange
        await AuthenticateNewStudentAsync();

        await _client.PostAsJsonAsync("/api/fraud-reports",
            ValidReportRequest("Fraudulent College A",
                "This college A is operating without accreditation and deceiving students."));

        await _client.PostAsJsonAsync("/api/fraud-reports",
            ValidReportRequest("Fraudulent College B",
                "This college B is operating without accreditation and deceiving students."));

        await _client.PostAsJsonAsync("/api/fraud-reports",
            ValidReportRequest("Fraudulent College C",
                "This college C is operating without accreditation and deceiving students."));

        // Act
        var response = await _client.GetAsync("/api/fraud-reports");

        // Assert
        var result = await response.ReadAsJsonAsync<FraudReportsResponse>();
        result!.Data!.Reports.Should().HaveCount(3);
        result.Data.Pagination.TotalCount.Should().Be(3);
    }

    // =========================================================================
    // Get Report By ID
    // =========================================================================

    [Fact]
    public async Task GetReportById_OwnReport_ReturnsReport()
    {
        // Arrange
        await AuthenticateNewStudentAsync();

        // Create a report first
        var createResponse = await _client.PostAsJsonAsync(
            "/api/fraud-reports", ValidReportRequest());
        var created = await createResponse.ReadAsJsonAsync<CreateFraudReportResponse>();
        var reportId = created!.Data!.Id;

        // Act
        var response = await _client.GetAsync($"/api/fraud-reports/{reportId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<FraudReportResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(reportId);
        result.Data.ReportedInstituteName.Should().Be("Fake University SA");
    }

    [Fact]
    public async Task GetReportById_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateNewStudentAsync();

        // Act
        var response = await _client.GetAsync($"/api/fraud-reports/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // IDOR Security Tests
    // =========================================================================

    [Fact]
    public async Task GetReportById_AnotherUsersReport_ReturnsNotFound()
    {
        // Arrange - Student 1 creates a report
        var client1 = _factory.CreateClient();
        var (token1, _, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(client1);
        client1.SetBearerToken(token1);

        var createResponse = await client1.PostAsJsonAsync(
            "/api/fraud-reports", ValidReportRequest());
        var created = await createResponse.ReadAsJsonAsync<CreateFraudReportResponse>();
        var reportId = created!.Data!.Id;

        // Student 2 tries to access Student 1's report
        var client2 = _factory.CreateClient();
        var (token2, _, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(client2);
        client2.SetBearerToken(token2);

        // Act
        var response = await client2.GetAsync($"/api/fraud-reports/{reportId}");

        // Assert - Student 2 cannot see Student 1's report
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMyReports_TwoUsers_EachSeesOnlyOwnReports()
    {
        // Arrange - two separate clients
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        var (token1, _, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(client1);
        client1.SetBearerToken(token1);

        var (token2, _, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(client2);
        client2.SetBearerToken(token2);

        // Student 1 submits 2 reports
        await client1.PostAsJsonAsync("/api/fraud-reports",
            ValidReportRequest("Student1 Fake College A",
                "Student 1 college A operating without accreditation from DHET."));

        await client1.PostAsJsonAsync("/api/fraud-reports",
            ValidReportRequest("Student1 Fake College B",
                "Student 1 college B operating without accreditation from DHET."));

        // Student 2 submits 1 report
        await client2.PostAsJsonAsync("/api/fraud-reports",
            ValidReportRequest("Student2 Fake College",
                "Student 2 college operating without accreditation from DHET."));

        // Act
        var response1 = await client1.GetAsync("/api/fraud-reports");
        var response2 = await client2.GetAsync("/api/fraud-reports");

        // Assert - each student sees only their own reports
        var result1 = await response1.ReadAsJsonAsync<FraudReportsResponse>();
        var result2 = await response2.ReadAsJsonAsync<FraudReportsResponse>();

        result1!.Data!.Reports.Should().HaveCount(2);
        result2!.Data!.Reports.Should().HaveCount(1);

        // Verify no cross-contamination of report names
        result1.Data.Reports
            .Should().AllSatisfy(r =>
                r.ReportedInstituteName.Should().StartWith("Student1"));

        result2.Data.Reports
            .Should().AllSatisfy(r =>
                r.ReportedInstituteName.Should().StartWith("Student2"));
    }

    // =========================================================================
    // End-to-End Workflow
    // =========================================================================

    [Fact]
    public async Task FraudReportWorkflow_CreateVerifyRetrieve_WorksEndToEnd()
    {
        // Arrange
        var (_, _, userId) = await AuthenticateNewStudentAsync();
        var studentId = await GetStudentIdAsync(userId);

        // Step 1: Verify no reports initially
        var emptyList = await _client.GetAsync("/api/fraud-reports");
        var empty = await emptyList.ReadAsJsonAsync<FraudReportsResponse>();
        empty!.Data!.Reports.Should().BeEmpty();

        // Step 2: Submit a report
        var request = ValidReportRequest(
            "End to End Fake University",
            "This university is operating without proper accreditation and deceiving students."
        );

        var createResponse = await _client.PostAsJsonAsync("/api/fraud-reports", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.ReadAsJsonAsync<CreateFraudReportResponse>();
        var reportId = created!.Data!.Id;

        // Step 3: Verify it appears in list
        var listResponse = await _client.GetAsync("/api/fraud-reports");
        var list = await listResponse.ReadAsJsonAsync<FraudReportsResponse>();
        list!.Data!.Reports.Should().HaveCount(1);
        list.Data.Reports.First().Id.Should().Be(reportId);

        // Step 4: Retrieve by ID
        var getResponse = await _client.GetAsync($"/api/fraud-reports/{reportId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var retrieved = await getResponse.ReadAsJsonAsync<FraudReportResponse>();
        retrieved!.Data!.Id.Should().Be(reportId);
        retrieved.Data.ReportedInstituteName.Should().Be(request.ReportedInstituteName);
        retrieved.Data.Description.Should().Be(request.Description);

        // Step 5: Verify database state
        await using var dbContext = _factory.CreateDbContext();
        var dbReport = await dbContext.FraudReports
            .FirstOrDefaultAsync(r => r.Id == reportId);

        dbReport.Should().NotBeNull();
        dbReport!.StudentId.Should().Be(studentId);
        dbReport.Status.Should().Be(FraudReportStatus.Submitted);
        dbReport.IsAnonymous.Should().BeFalse();
        dbReport.ReportedInstituteName.Should().Be(request.ReportedInstituteName);
    }
}