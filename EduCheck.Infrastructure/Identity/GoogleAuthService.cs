using EduCheck.Application.Configuration;
using EduCheck.Application.DTOs.Auth;
using EduCheck.Application.Interfaces;
using EduCheck.Domain.Entities;
using EduCheck.Domain.Enums;
using EduCheck.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Web;

namespace EduCheck.Infrastructure.Identity;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly OAuthSettings _oAuthSettings;
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleAuthService> _logger;

    private const string GoogleAuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string GoogleTokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string GoogleUserInfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";

    public GoogleAuthService(
        IOptions<OAuthSettings> oAuthSettings,
        IOptions<JwtSettings> jwtSettings,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ITokenService tokenService,
        IHttpClientFactory httpClientFactory,
        ILogger<GoogleAuthService> logger)
    {
        _oAuthSettings = oAuthSettings.Value;
        _jwtSettings = jwtSettings.Value;
        _userManager = userManager;
        _context = context;
        _tokenService = tokenService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string GetAuthorizationUrl(string redirectUri)
    {
        var queryParams = HttpUtility.ParseQueryString(string.Empty);
        queryParams["client_id"] = _oAuthSettings.Google.ClientId;
        queryParams["redirect_uri"] = redirectUri;
        queryParams["response_type"] = "code";
        queryParams["scope"] = "openid email profile";
        queryParams["access_type"] = "offline";
        queryParams["prompt"] = "consent";

        return $"{GoogleAuthEndpoint}?{queryParams}";
    }

    public async Task<AuthResponse> AuthenticateAsync(string code, string redirectUri)
    {
        try
        {
            _logger.LogInformation("Starting Google authentication");

            var tokenResponse = await ExchangeCodeForTokensAsync(code, redirectUri);

            if (tokenResponse == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Failed to authenticate with Google",
                    Errors = new List<string> { "Could not exchange authorization code for tokens" }
                };
            }

            var googleUser = await GetUserInfoAsync(tokenResponse.AccessToken);

            if (googleUser == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Failed to get user info from Google",
                    Errors = new List<string> { "Could not retrieve user information" }
                };
            }

            _logger.LogInformation("Google user retrieved: {Email}", googleUser.Email);

            var user = await _userManager.FindByEmailAsync(googleUser.Email);
            Student? student = null;
            bool isNewUser = false;

            if (user == null)
            {
                isNewUser = true;
                user = new ApplicationUser
                {
                    UserName = googleUser.Email,
                    Email = googleUser.Email,
                    EmailConfirmed = googleUser.EmailVerified,
                    FirstName = googleUser.GivenName ?? googleUser.Name.Split(' ').FirstOrDefault() ?? "User",
                    LastName = googleUser.FamilyName ?? googleUser.Name.Split(' ').LastOrDefault() ?? "",
                    Role = UserRole.Student,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    _logger.LogError("Failed to create user: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));

                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Failed to create user account",
                        Errors = createResult.Errors.Select(e => e.Description).ToList()
                    };
                }

               
                student = new Student
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Students.AddAsync(student);
                await _context.SaveChangesAsync();

          
                await _userManager.AddToRoleAsync(user, UserRole.Student.ToString());

                _logger.LogInformation("Created new user from Google OAuth: {Email}", user.Email);
            }
            else
            {
             
                if (!user.IsActive)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Account deactivated",
                        Errors = new List<string> { "Your account has been deactivated. Please contact support." }
                    };
                }

                if (!user.EmailConfirmed && googleUser.EmailVerified)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }

                student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);

                _logger.LogInformation("Existing user logged in via Google: {Email}", user.Email);
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = await CreateRefreshTokenAsync(user.Id);

            return new AuthResponse
            {
                Success = true,
                Message = isNewUser ? "Account created successfully" : "Login successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    CreatedAt = user.CreatedAt,
                    Province = student?.Province,
                    City = student?.City
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google authentication");
            return new AuthResponse
            {
                Success = false,
                Message = "Authentication failed",
                Errors = new List<string> { "An unexpected error occurred during authentication" }
            };
        }
    }

    private async Task<GoogleTokenResponse?> ExchangeCodeForTokensAsync(string code, string redirectUri)
    {
        var client = _httpClientFactory.CreateClient();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _oAuthSettings.Google.ClientId,
            ["client_secret"] = _oAuthSettings.Google.ClientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        });

        var response = await client.PostAsync(GoogleTokenEndpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Google token exchange failed: {Error}", errorContent);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GoogleTokenResponse>(json);
    }

    private async Task<GoogleUserInfo?> GetUserInfoAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(GoogleUserInfoEndpoint);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Google user info request failed: {Error}", errorContent);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GoogleUserInfo>(json);
    }

    private async Task<string> CreateRefreshTokenAsync(Guid userId)
    {
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);

        var tokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.RefreshTokens.AddAsync(tokenEntity);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    private static string HashToken(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}