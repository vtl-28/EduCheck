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
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EduCheck.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _context = context;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterStudentAsync(StudentRegistrationRequest request)
    {
        _logger.LogInformation("Attempting to register student with email: {Email}", request.Email);


        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed - email already exists: {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "Registration failed",
                Errors = new List<string> { "An account with this email already exists" }
            };
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Role = UserRole.Student,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Registration failed for {Email}: {Errors}",
                request.Email, string.Join(", ", result.Errors.Select(e => e.Description)));

            return new AuthResponse
            {
                Success = false,
                Message = "Registration failed",
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Province = request.Province?.Trim(),
            City = request.City?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Students.AddAsync(student);
        await _context.SaveChangesAsync();

        await _userManager.AddToRoleAsync(user, UserRole.Student.ToString());

        _logger.LogInformation("Student registered successfully: {Email}", request.Email);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            User = MapToUserDto(user, student, null)
        };
    }

    public async Task<AuthResponse> RegisterAdminAsync(AdminRegistrationRequest request)
    {
        _logger.LogInformation("Attempting to register admin with email: {Email}", request.Email);

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed - email already exists: {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "Registration failed",
                Errors = new List<string> { "An account with this email already exists" }
            };
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Registration failed for {Email}: {Errors}",
                request.Email, string.Join(", ", result.Errors.Select(e => e.Description)));

            return new AuthResponse
            {
                Success = false,
                Message = "Registration failed",
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        var admin = new Admin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Department = request.Department?.Trim(),
            EmployeeId = request.EmployeeId?.Trim(),
            AdminLevel = AdminLevel.Standard,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Admins.AddAsync(admin);
        await _context.SaveChangesAsync();

        await _userManager.AddToRoleAsync(user, UserRole.Admin.ToString());

        _logger.LogInformation("Admin registered successfully: {Email}", request.Email);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            User = MapToUserDto(user, null, admin)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Login attempt for: {Email}", request.Email);

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            _logger.LogWarning("Login failed - user not found: {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid credentials",
                Errors = new List<string> { "Invalid email or password" }
            };
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed - account deactivated: {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "Account deactivated",
                Errors = new List<string> { "Your account has been deactivated. Please contact support." }
            };
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!isValidPassword)
        {
            _logger.LogWarning("Login failed - invalid password: {Email}", request.Email);

            await _userManager.AccessFailedAsync(user);

            return new AuthResponse
            {
                Success = false,
                Message = "Invalid credentials",
                Errors = new List<string> { "Invalid email or password" }
            };
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login failed - account locked: {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "Account locked",
                Errors = new List<string> { "Your account is locked due to multiple failed login attempts. Please try again later." }
            };
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        Student? student = null;
        Admin? admin = null;

        if (user.Role == UserRole.Student)
        {
            student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
        }
        else if (user.Role == UserRole.Admin)
        {
            admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == user.Id);
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        _logger.LogInformation("Login successful for: {Email}", request.Email);

        return new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            User = MapToUserDto(user, student, admin)
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        _logger.LogInformation("Attempting to refresh token");

        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);

        if (principal == null)
        {
            _logger.LogWarning("Token refresh failed - invalid access token");
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid token",
                Errors = new List<string> { "Invalid access token" }
            };
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Token refresh failed - invalid user ID in token");
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid token",
                Errors = new List<string> { "Invalid token claims" }
            };
        }

        var refreshTokenHash = HashToken(request.RefreshToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.TokenHash == refreshTokenHash);

        if (storedToken == null)
        {
            _logger.LogWarning("Token refresh failed - refresh token not found for user: {UserId}", userId);
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid token",
                Errors = new List<string> { "Invalid refresh token" }
            };
        }

        if (storedToken.IsRevoked)
        {
            _logger.LogWarning("Token refresh failed - refresh token revoked for user: {UserId}", userId);
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid token",
                Errors = new List<string> { "Refresh token has been revoked" }
            };
        }

        if (storedToken.IsExpired)
        {
            _logger.LogWarning("Token refresh failed - refresh token expired for user: {UserId}", userId);
            return new AuthResponse
            {
                Success = false,
                Message = "Token expired",
                Errors = new List<string> { "Refresh token has expired. Please login again." }
            };
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Token refresh failed - user not found or inactive: {UserId}", userId);
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid user",
                Errors = new List<string> { "User not found or account deactivated" }
            };
        }

        storedToken.IsRevoked = true;
        _context.RefreshTokens.Update(storedToken);


        Student? student = null;
        Admin? admin = null;

        if (user.Role == UserRole.Student)
        {
            student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
        }
        else if (user.Role == UserRole.Admin)
        {
            admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == user.Id);
        }


        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = await CreateRefreshTokenAsync(user.Id);

        _logger.LogInformation("Token refreshed successfully for user: {UserId}", userId);

        return new AuthResponse
        {
            Success = true,
            Message = "Token refreshed successfully",
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            User = MapToUserDto(user, student, admin)
        };
    }

    public Task<AuthResponse> ExternalLoginAsync(ExternalAuthRequest request)
    {
        _logger.LogInformation("External login attempt with provider: {Provider}", request.Provider);

        return Task.FromResult(new AuthResponse
        {
            Success = false,
            Message = "Not implemented",
            Errors = new List<string> { "External authentication is not yet implemented" }
        });
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        var refreshTokenHash = HashToken(refreshToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash);

        if (storedToken == null)
        {
            return false;
        }

        storedToken.IsRevoked = true;
        _context.RefreshTokens.Update(storedToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Refresh token revoked for user: {UserId}", storedToken.UserId);
        return true;
    }

    public async Task<bool> RevokeAllUserTokensAsync(Guid userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        if (!tokens.Any())
        {
            return false;
        }

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }

        _context.RefreshTokens.UpdateRange(tokens);
        await _context.SaveChangesAsync();

        _logger.LogInformation("All refresh tokens revoked for user: {UserId}", userId);
        return true;
    }

    #region Private Helper Methods

    private async Task<string> CreateRefreshTokenAsync(Guid userId, string? deviceInfo = null, string? ipAddress = null)
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
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        await _context.RefreshTokens.AddAsync(tokenEntity);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static UserDto MapToUserDto(ApplicationUser user, Student? student, Admin? admin)
    {
        return new UserDto
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
            City = student?.City,
            Department = admin?.Department,
            EmployeeId = admin?.EmployeeId,
            AdminLevel = admin?.AdminLevel
        };
    }

    #endregion
}