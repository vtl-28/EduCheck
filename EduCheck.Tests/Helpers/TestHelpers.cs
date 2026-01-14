using EduCheck.Application.Configuration;
using EduCheck.Domain.Entities;
using EduCheck.Domain.Enums;
using EduCheck.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace EduCheck.Tests.Helpers;

public static class TestHelpers
{
    public static JwtSettings CreateJwtSettings()
    {
        return new JwtSettings
        {
            SecretKey = "ThisIsAVeryLongSecretKeyForTestingPurposes123!",
            Issuer = "EduCheck.Tests",
            Audience = "EduCheck.Tests",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };
    }

    public static IOptions<JwtSettings> CreateJwtSettingsOptions()
    {
        return Options.Create(CreateJwtSettings());
    }

    public static OAuthSettings CreateOAuthSettings()
    {
        return new OAuthSettings
        {
            Google = new GoogleSettings
            {
                ClientId = "test-client-id",
                ClientSecret = "test-client-secret"
            },
            Facebook = new FacebookSettings
            {
                AppId = "test-app-id",
                AppSecret = "test-app-secret"
            }
        };
    }

    public static IOptions<OAuthSettings> CreateOAuthSettingsOptions()
    {
        return Options.Create(CreateOAuthSettings());
    }

    public static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    public static ApplicationUser CreateTestUser(
        string email = "test@example.com",
        string firstName = "Test",
        string lastName = "User",
        UserRole role = UserRole.Student)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpper(),
            NormalizedUserName = email.ToUpper(),
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            IsActive = true,
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Student CreateTestStudent(Guid userId)
    {
        return new Student
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Province = "Gauteng",
            City = "Johannesburg",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Admin CreateTestAdmin(Guid userId)
    {
        return new Admin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Department = "DHET",
            EmployeeId = "EMP001",
            AdminLevel = AdminLevel.Standard,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        return userManager;
    }

    public static RefreshToken CreateTestRefreshToken(
        Guid userId,
        string tokenHash = "test-token-hash",
        bool isRevoked = false,
        bool isExpired = false)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            IsRevoked = isRevoked,
            ExpiresAt = isExpired ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
    }
}