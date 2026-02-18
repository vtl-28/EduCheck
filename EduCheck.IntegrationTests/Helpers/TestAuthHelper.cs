using EduCheck.Application.DTOs.Auth;
using System.Net.Http.Json;

namespace EduCheck.IntegrationTests.Helpers;

public static class TestAuthHelper
{
    /// <summary>
    /// Register a new student user and return JWT token
    /// </summary>
    public static async Task<(string AccessToken, string RefreshToken, Guid UserId)> RegisterStudentAndLoginAsync(
        HttpClient client,
        string email = null,
        string password = "Test@123",
        string firstName = "Test",
        string lastName = "Student",
        string? phoneNumber = "0123456789",
        string? province = "Gauteng",
        string? city = "Johannesburg")
    {
        email ??= $"student-{Guid.NewGuid():N}@test.com";
        // Register student
        var registerRequest = new StudentRegistrationRequest
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            Province = province,
            City = city,

        };

        var registerResponse = await client.PostAsJsonAsync("/api/Auth/register/student", registerRequest);

        if (!registerResponse.IsSuccessStatusCode)
        {
            var error = await registerResponse.Content.ReadAsStringAsync();
            throw new Exception($"Student registration failed: {error}");
        }

        var registerResult = await registerResponse.ReadAsJsonAsync<AuthResponse>();

        if (registerResult == null || !registerResult.Success)
        {
            throw new Exception($"Registration failed: {registerResult?.Message ?? "Unknown error"}");
        }

        if (string.IsNullOrEmpty(registerResult.AccessToken))
        {
            throw new Exception("Registration succeeded but no access token returned");
        }

        if (string.IsNullOrEmpty(registerResult.RefreshToken))
        {
            throw new Exception("Registration succeeded but no refresh token returned");
        }

        if (registerResult.User == null)
        {
            throw new Exception("Registration succeeded but no user data returned");
        }

        return (registerResult.AccessToken, registerResult.RefreshToken, registerResult.User.Id);
    }

    /// <summary>
    /// Register a new admin user and return JWT token
    /// </summary>
    public static async Task<(string AccessToken, string RefreshToken, Guid UserId)> RegisterAdminAndLoginAsync(
        HttpClient client,
        string email = "admin@test.com",
        string password = "Admin@123",
        string firstName = "Test",
        string lastName = "Admin",
        string? phoneNumber = "0123456789",
        string? department = "Testing Department",
        string? employeeId = "TEST001")
    {
        // Register admin
        var registerRequest = new AdminRegistrationRequest
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            Department = department,
            EmployeeId = employeeId
        };

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register/admin", registerRequest);

        if (!registerResponse.IsSuccessStatusCode)
        {
            var error = await registerResponse.Content.ReadAsStringAsync();
            throw new Exception($"Admin registration failed: {error}");
        }

        var registerResult = await registerResponse.ReadAsJsonAsync<AuthResponse>();

        if (registerResult == null || !registerResult.Success)
        {
            throw new Exception($"Registration failed: {registerResult?.Message ?? "Unknown error"}");
        }

        if (string.IsNullOrEmpty(registerResult.AccessToken))
        {
            throw new Exception("Registration succeeded but no access token returned");
        }

        if (string.IsNullOrEmpty(registerResult.RefreshToken))
        {
            throw new Exception("Registration succeeded but no refresh token returned");
        }

        if (registerResult.User == null)
        {
            throw new Exception("Registration succeeded but no user data returned");
        }

        return (registerResult.AccessToken, registerResult.RefreshToken, registerResult.User.Id);
    }

    /// <summary>
    /// Login with existing credentials
    /// </summary>
    public static async Task<(string AccessToken, string RefreshToken, Guid UserId)> LoginAsync(
        HttpClient client,
        string email,
        string password)
    {
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        if (!loginResponse.IsSuccessStatusCode)
        {
            var error = await loginResponse.Content.ReadAsStringAsync();
            throw new Exception($"Login failed: {error}");
        }

        var loginResult = await loginResponse.ReadAsJsonAsync<AuthResponse>();

        if (loginResult == null || !loginResult.Success)
        {
            throw new Exception($"Login failed: {loginResult?.Message ?? "Unknown error"}");
        }

        if (string.IsNullOrEmpty(loginResult.AccessToken))
        {
            throw new Exception("Login succeeded but no access token returned");
        }

        if (string.IsNullOrEmpty(loginResult.RefreshToken))
        {
            throw new Exception("Login succeeded but no refresh token returned");
        }

        if (loginResult.User == null)
        {
            throw new Exception("Login succeeded but no user data returned");
        }

        return (loginResult.AccessToken, loginResult.RefreshToken, loginResult.User.Id);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    public static async Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(
        HttpClient client,
        string accessToken,
        string refreshToken)
    {
        var refreshRequest = new RefreshTokenRequest
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh-token", refreshRequest);

        if (!refreshResponse.IsSuccessStatusCode)
        {
            var error = await refreshResponse.Content.ReadAsStringAsync();
            throw new Exception($"Token refresh failed: {error}");
        }

        var refreshResult = await refreshResponse.ReadAsJsonAsync<AuthResponse>();

        if (refreshResult == null || !refreshResult.Success)
        {
            throw new Exception($"Token refresh failed: {refreshResult?.Message ?? "Unknown error"}");
        }

        if (string.IsNullOrEmpty(refreshResult.AccessToken))
        {
            throw new Exception("Token refresh succeeded but no access token returned");
        }

        if (string.IsNullOrEmpty(refreshResult.RefreshToken))
        {
            throw new Exception("Token refresh succeeded but no refresh token returned");
        }

        return (refreshResult.AccessToken, refreshResult.RefreshToken);
    }

    /// <summary>
    /// Logout and revoke tokens
    /// </summary>
    public static async Task LogoutAsync(HttpClient client, string refreshToken)
    {
        var logoutResponse = await client.PostAsJsonAsync("/api/Auth/logout", new LogoutRequest
        {
            RefreshToken = refreshToken
        });
        logoutResponse.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Create multiple test students at once
    /// </summary>
    public static async Task<List<(string Email, string AccessToken, string RefreshToken, Guid UserId)>> CreateMultipleStudentsAsync(
        HttpClient client,
        int count = 3)
    {
        var users = new List<(string Email, string AccessToken, string RefreshToken, Guid UserId)>();

        for (int i = 1; i <= count; i++)
        {
            var email = $"student{i}@test.com";
            var (accessToken, refreshToken, userId) = await RegisterStudentAndLoginAsync(
                client,
                email: email,
                firstName: $"Student{i}",
                lastName: "Test",
                province: i % 2 == 0 ? "Western Cape" : "Gauteng",
                city: i % 2 == 0 ? "Cape Town" : "Johannesburg"
            );

            users.Add((email, accessToken, refreshToken, userId));
        }

        return users;
    }

    /// <summary>
    /// Create multiple test admins at once
    /// </summary>
    public static async Task<List<(string Email, string AccessToken, string RefreshToken, Guid UserId)>> CreateMultipleAdminsAsync(
        HttpClient client,
        int count = 2)
    {
        var admins = new List<(string Email, string AccessToken, string RefreshToken, Guid UserId)>();

        for (int i = 1; i <= count; i++)
        {
            var email = $"admin{i}@test.com";
            var (accessToken, refreshToken, userId) = await RegisterAdminAndLoginAsync(
                client,
                email: email,
                firstName: $"Admin{i}",
                lastName: "Test",
                department: "Testing Department",
                employeeId: $"TEST{i:D3}"
            );

            admins.Add((email, accessToken, refreshToken, userId));
        }

        return admins;
    }
}