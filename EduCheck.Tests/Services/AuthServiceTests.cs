using EduCheck.Application.DTOs.Auth;
using EduCheck.Application.Interfaces;
using EduCheck.Domain.Entities;
using EduCheck.Domain.Enums;
using EduCheck.Infrastructure.Data;
using EduCheck.Infrastructure.Identity;
using EduCheck.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace EduCheck.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userManagerMock = TestHelpers.CreateMockUserManager();
        _tokenServiceMock = new Mock<ITokenService>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _context = TestHelpers.CreateInMemoryDbContext();

        var jwtSettings = TestHelpers.CreateJwtSettingsOptions();

        _authService = new AuthService(
            _userManagerMock.Object,
            _context,
            _tokenServiceMock.Object,
            jwtSettings,
            _loggerMock.Object);
    }

    #region RegisterStudentAsync Tests

    [Fact]
    public async Task RegisterStudentAsync_ValidRequest_ReturnsSuccess()
    {
       
        var request = new StudentRegistrationRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456",
            Province = "Gauteng",
            City = "Johannesburg"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), UserRole.Student.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>()))
            .Returns("test-access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("test-refresh-token");

       
        var result = await _authService.RegisterStudentAsync(request);

      
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Registration successful");
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(request.Email);
        result.User.FirstName.Should().Be(request.FirstName);
        result.User.Role.Should().Be(UserRole.Student);
    }

    [Fact]
    public async Task RegisterStudentAsync_DuplicateEmail_ReturnsError()
    {
       
        var existingUser = TestHelpers.CreateTestUser(email: "existing@example.com");

        var request = new StudentRegistrationRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "existing@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        
        var result = await _authService.RegisterStudentAsync(request);

        
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("An account with this email already exists");
        result.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task RegisterStudentAsync_WeakPassword_ReturnsError()
    {
        
        var request = new StudentRegistrationRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = "weak",
            ConfirmPassword = "weak"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Password must be at least 8 characters." },
                new IdentityError { Description = "Password must contain an uppercase letter." }
            ));

     
        var result = await _authService.RegisterStudentAsync(request);

     
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Password must be at least 8 characters.");
        result.Errors.Should().Contain("Password must contain an uppercase letter.");
    }

    [Fact]
    public async Task RegisterStudentAsync_ValidRequest_CreatesStudentProfile()
    {
        
        var request = new StudentRegistrationRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456",
            Province = "Gauteng",
            City = "Johannesburg"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), UserRole.Student.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>()))
            .Returns("test-access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("test-refresh-token");

        
        var result = await _authService.RegisterStudentAsync(request);

        
        result.Success.Should().BeTrue();
        result.User!.Province.Should().Be("Gauteng");
        result.User.City.Should().Be("Johannesburg");

        
        var students = _context.Students.ToList();
        students.Should().HaveCount(1);
        students.First().Province.Should().Be("Gauteng");
    }

    #endregion

    #region RegisterAdminAsync Tests

    [Fact]
    public async Task RegisterAdminAsync_ValidRequest_ReturnsSuccess()
    {
       
        var request = new AdminRegistrationRequest
        {
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@dhet.gov.za",
            Password = "Admin@123456",
            ConfirmPassword = "Admin@123456",
            Department = "DHET",
            EmployeeId = "EMP001"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), UserRole.Admin.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>()))
            .Returns("test-access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("test-refresh-token");

       
        var result = await _authService.RegisterAdminAsync(request);

        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Role.Should().Be(UserRole.Admin);
        result.User.Department.Should().Be("DHET");
        result.User.EmployeeId.Should().Be("EMP001");
    }

    [Fact]
    public async Task RegisterAdminAsync_ValidRequest_CreatesAdminProfile()
    {
        
        var request = new AdminRegistrationRequest
        {
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@dhet.gov.za",
            Password = "Admin@123456",
            ConfirmPassword = "Admin@123456",
            Department = "DHET",
            EmployeeId = "EMP001"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), UserRole.Admin.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>()))
            .Returns("test-access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("test-refresh-token");

       
        var result = await _authService.RegisterAdminAsync(request);

        result.Success.Should().BeTrue();

       
        var admins = _context.Admins.ToList();
        admins.Should().HaveCount(1);
        admins.First().Department.Should().Be("DHET");
        admins.First().AdminLevel.Should().Be(AdminLevel.Standard);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokens()
    {
        
        var user = TestHelpers.CreateTestUser(email: "john@example.com");
        var student = TestHelpers.CreateTestStudent(user.Id);

        await _context.Students.AddAsync(student);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "Test@123456"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);

        _userManagerMock.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _userManagerMock.Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user))
            .Returns("test-access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("test-refresh-token");

        
        var result = await _authService.LoginAsync(request);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Login successful");
        result.AccessToken.Should().Be("test-access-token");
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_InvalidEmail_ReturnsError()
    {
        
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Test@123456"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

       
        var result = await _authService.LoginAsync(request);

       
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid credentials");
        result.Errors.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsError()
    {
       
        var user = TestHelpers.CreateTestUser();

        var request = new LoginRequest
        {
            Email = user.Email!,
            Password = "WrongPassword123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(false);

        _userManagerMock.Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        
        var result = await _authService.LoginAsync(request);

     
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_DeactivatedAccount_ReturnsError()
    {
       
        var user = TestHelpers.CreateTestUser();
        user.IsActive = false;

        var request = new LoginRequest
        {
            Email = user.Email!,
            Password = "Test@123456"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        
        var result = await _authService.LoginAsync(request);

       
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Account deactivated");
    }

    [Fact]
    public async Task LoginAsync_LockedOutAccount_ReturnsError()
    {
       
        var user = TestHelpers.CreateTestUser();

        var request = new LoginRequest
        {
            Email = user.Email!,
            Password = "Test@123456"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);

        _userManagerMock.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(true);

        
        var result = await _authService.LoginAsync(request);

        
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Account locked");
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_ValidTokens_ReturnsNewTokens()
    {
        
        var user = TestHelpers.CreateTestUser();
        var refreshToken = "valid-refresh-token";
        var refreshTokenHash = HashToken(refreshToken);

        var storedToken = TestHelpers.CreateTestRefreshToken(user.Id, refreshTokenHash);
        await _context.RefreshTokens.AddAsync(storedToken);
        await _context.SaveChangesAsync();

        var request = new RefreshTokenRequest
        {
            AccessToken = "old-access-token",
            RefreshToken = refreshToken
        };

        var claims = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString())
            }));

        _tokenServiceMock.Setup(x => x.GetPrincipalFromExpiredToken(request.AccessToken))
            .Returns(claims);

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user))
            .Returns("new-access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("new-refresh-token");

      
        var result = await _authService.RefreshTokenAsync(request);

        
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidAccessToken_ReturnsError()
    {
       
        var request = new RefreshTokenRequest
        {
            AccessToken = "invalid-token",
            RefreshToken = "some-refresh-token"
        };

        _tokenServiceMock.Setup(x => x.GetPrincipalFromExpiredToken(request.AccessToken))
            .Returns((System.Security.Claims.ClaimsPrincipal?)null);

        
        var result = await _authService.RefreshTokenAsync(request);

       
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid access token");
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedRefreshToken_ReturnsError()
    {
        var user = TestHelpers.CreateTestUser();
        var refreshToken = "revoked-refresh-token";
        var refreshTokenHash = HashToken(refreshToken);

        var storedToken = TestHelpers.CreateTestRefreshToken(user.Id, refreshTokenHash, isRevoked: true);
        await _context.RefreshTokens.AddAsync(storedToken);
        await _context.SaveChangesAsync();

        var request = new RefreshTokenRequest
        {
            AccessToken = "old-access-token",
            RefreshToken = refreshToken
        };

        var claims = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString())
            }));

        _tokenServiceMock.Setup(x => x.GetPrincipalFromExpiredToken(request.AccessToken))
            .Returns(claims);

       
        var result = await _authService.RefreshTokenAsync(request);

       
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Refresh token has been revoked");
    }

    #endregion

    #region RevokeRefreshTokenAsync Tests

    [Fact]
    public async Task RevokeRefreshTokenAsync_ValidToken_ReturnsTrue()
    {
       
        var user = TestHelpers.CreateTestUser();
        var refreshToken = "token-to-revoke";
        var refreshTokenHash = HashToken(refreshToken);

        var storedToken = TestHelpers.CreateTestRefreshToken(user.Id, refreshTokenHash);
        await _context.RefreshTokens.AddAsync(storedToken);
        await _context.SaveChangesAsync();

        
        var result = await _authService.RevokeRefreshTokenAsync(refreshToken);

       
        result.Should().BeTrue();

       
        var updatedToken = _context.RefreshTokens.First();
        updatedToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_InvalidToken_ReturnsFalse()
    {
       
        var result = await _authService.RevokeRefreshTokenAsync("non-existent-token");

        
        result.Should().BeFalse();
    }

    #endregion

    #region RevokeAllUserTokensAsync Tests

    [Fact]
    public async Task RevokeAllUserTokensAsync_UserWithTokens_RevokesAll()
    {
        
        var user = TestHelpers.CreateTestUser();

        var token1 = TestHelpers.CreateTestRefreshToken(user.Id, "hash1");
        var token2 = TestHelpers.CreateTestRefreshToken(user.Id, "hash2");
        var token3 = TestHelpers.CreateTestRefreshToken(user.Id, "hash3");

        await _context.RefreshTokens.AddRangeAsync(token1, token2, token3);
        await _context.SaveChangesAsync();

        var result = await _authService.RevokeAllUserTokensAsync(user.Id);

        
        result.Should().BeTrue();

        var tokens = _context.RefreshTokens.Where(t => t.UserId == user.Id).ToList();
        tokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_UserWithNoTokens_ReturnsFalse()
    {
       
        var userId = Guid.NewGuid();

       
        var result = await _authService.RevokeAllUserTokensAsync(userId);

        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static string HashToken(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    #endregion
}