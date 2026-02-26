using EduCheck.Application.DTOs.Auth;
using EduCheck.IntegrationTests.Fixtures;
using EduCheck.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace EduCheck.IntegrationTests.Tests;

public class AuthenticationIntegrationTests : IClassFixture<EduCheckWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly EduCheckWebApplicationFactory _factory;

    public AuthenticationIntegrationTests(EduCheckWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Student Registration Tests

    [Fact]
    public async Task RegisterStudent_WithValidData_ReturnsSuccessAndCreatesUser()
    {
        // Arrange
        var request = new StudentRegistrationRequest
        {
            Email = "newstudent@test.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "0123456789",
            Province = "Gauteng",
            City = "Johannesburg"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register/student", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(request.Email);
        result.User.FirstName.Should().Be(request.FirstName);
        result.User.LastName.Should().Be(request.LastName);

        await using var dbContext = _factory.CreateDbContext();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        user.Should().NotBeNull();
        user!.Email.Should().Be(request.Email);

        var student = await dbContext.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
        student.Should().NotBeNull();
        student!.Province.Should().Be(request.Province);
        student.City.Should().Be(request.City);
    }

    [Fact]
    public async Task RegisterStudent_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var email = "duplicate@test.com";

        await TestAuthHelper.RegisterStudentAndLoginAsync(_client, email: email);

        var request = new StudentRegistrationRequest
        {
            Email = email,
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            FirstName = "Jane",
            LastName = "Doe",
            PhoneNumber = "0987654321"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/register/student", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.ReadAsJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterStudent_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new StudentRegistrationRequest
        {
            Email = "not-an-email",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register/student", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterStudent_WithMismatchedPasswords_ReturnsBadRequest()
    {
        // Arrange
        var request = new StudentRegistrationRequest
        {
            Email = "mismatch@test.com",
            Password = "Test@123",
            ConfirmPassword = "DifferentPassword@123",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register/student", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Admin Registration Tests

    [Fact]
    public async Task RegisterAdmin_WithValidData_ReturnsSuccessAndCreatesUser()
    {
        // Arrange
        var request = new AdminRegistrationRequest
        {
            Email = "newadmin@test.com",
            Password = "Admin@123",
            ConfirmPassword = "Admin@123",
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "0123456789",
            Department = "IT Department",
            EmployeeId = "EMP001"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register/admin", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();

        await using var dbContext = _factory.CreateDbContext();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        user.Should().NotBeNull();

        var admin = await dbContext.Admins.FirstOrDefaultAsync(a => a.UserId == user!.Id);
        admin.Should().NotBeNull();
        admin!.Department.Should().Be(request.Department);
        admin.EmployeeId.Should().Be(request.EmployeeId);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessAndTokens()
    {
        // Arrange
        var email = "logintest@test.com";
        var password = "Test@123";

        await TestAuthHelper.RegisterStudentAndLoginAsync(_client, email: email, password: password);

        _client.ClearAuthentication();

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var email = "wrongpassword@test.com";
        var correctPassword = "Test@123";
        var wrongPassword = "WrongPassword@123";

        await TestAuthHelper.RegisterStudentAndLoginAsync(_client, email: email, password: correctPassword);

        _client.ClearAuthentication();

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = wrongPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@test.com",
            Password = "Test@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Token Refresh Tests

    [Fact]
    public async Task RefreshToken_WithValidTokens_ReturnsNewTokens()
    {
        // Arrange
        var (accessToken, refreshToken, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(_client);

        var refreshRequest = new RefreshTokenRequest
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();

        result.AccessToken.Should().NotBe(accessToken);
        result.RefreshToken.Should().NotBe(refreshToken);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        var (accessToken, _, _) = await TestAuthHelper.RegisterStudentAndLoginAsync(_client);

        var refreshRequest = new RefreshTokenRequest
        {
            AccessToken = accessToken,
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/refresh-token", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_WithValidToken_RevokesTokenSuccessfully()
    {
        // Arrange
        var (accessToken, refreshToken, userId) = await TestAuthHelper.RegisterStudentAndLoginAsync(_client);
        _client.SetBearerToken(accessToken);

        // Act
        var response = await _client.PostAsJsonAsync(
        "/api/auth/logout",
        new LogoutRequest
        {
            RefreshToken = refreshToken
        });
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var dbContext = _factory.CreateDbContext();
        var tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId)
            .ToListAsync();

        tokens.Should().NotBeEmpty();
        tokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
    }

    [Fact]
    public async Task Logout_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _client.ClearAuthentication();

        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region End-to-End Authentication Flow Test

    [Fact]
    public async Task CompleteAuthFlow_RegisterLoginRefreshLogout_WorksEndToEnd()
    {
        var email = "fullflow@test.com";
        var password = "Test@123";

        var (accessToken1, refreshToken1, userId) = await TestAuthHelper.RegisterStudentAndLoginAsync(
            _client,
            email: email,
            password: password
        );

        accessToken1.Should().NotBeNullOrEmpty();
        refreshToken1.Should().NotBeNullOrEmpty();

        _client.SetBearerToken(accessToken1);
        var protectedResponse = await _client.GetAsync("/api/Institutes/search?query=Alberton");
        protectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var (accessToken2, refreshToken2) = await TestAuthHelper.RefreshTokenAsync(
            _client,
            accessToken1,
            refreshToken1
        );

        accessToken2.Should().NotBe(accessToken1);
        refreshToken2.Should().NotBe(refreshToken1);

        _client.SetBearerToken(accessToken2);
        var protectedResponse2 = await _client.GetAsync("/api/Institutes/search?query=Mondeor");
        protectedResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        _client.SetBearerToken(accessToken2);
        await TestAuthHelper.LogoutAsync(_client, refreshToken2);

        await using var dbContext = _factory.CreateDbContext();
        var tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId)
            .ToListAsync();

        tokens.Should().NotBeEmpty();
        tokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());

        var refreshRequest = new RefreshTokenRequest
        {
            AccessToken = accessToken2,
            RefreshToken = refreshToken1
        };

        var failedRefresh = await _client.PostAsJsonAsync("/api/Auth/refresh-token", refreshRequest);
        failedRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}