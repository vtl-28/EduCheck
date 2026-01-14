using EduCheck.Domain.Entities;
using EduCheck.Domain.Enums;
using EduCheck.Infrastructure.Identity;
using EduCheck.Tests.Helpers;
using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EduCheck.Tests.Services;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var jwtSettings = TestHelpers.CreateJwtSettingsOptions();
        _tokenService = new TokenService(jwtSettings);
    }

    #region GenerateAccessToken Tests

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsValidJwtToken()
    {
      
        var user = TestHelpers.CreateTestUser();

       
        var token = _tokenService.GenerateAccessToken(user);

        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var canRead = handler.CanReadToken(token);
        canRead.Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_TokenContainsCorrectClaims()
    {
        
        var user = TestHelpers.CreateTestUser(
            email: "john@example.com",
            firstName: "John",
            lastName: "Doe",
            role: UserRole.Student);

       
        var token = _tokenService.GenerateAccessToken(user);

        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "john@example.com");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.GivenName && c.Value == "John");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Surname && c.Value == "Doe");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Student");
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_TokenContainsUserId()
    {
        
        var user = TestHelpers.CreateTestUser();

        
        var token = _tokenService.GenerateAccessToken(user);

        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.NameIdentifier &&
            c.Value == user.Id.ToString());
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_TokenHasCorrectIssuerAndAudience()
    {
        
        var user = TestHelpers.CreateTestUser();

        
        var token = _tokenService.GenerateAccessToken(user);

        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("EduCheck.Tests");
        jwtToken.Audiences.Should().Contain("EduCheck.Tests");
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_TokenHasExpirationTime()
    {
       
        var user = TestHelpers.CreateTestUser();

        
        var token = _tokenService.GenerateAccessToken(user);

        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow);
        jwtToken.ValidTo.Should().BeBefore(DateTime.UtcNow.AddMinutes(61));
    }

    #endregion

    #region GenerateRefreshToken Tests

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
       
        var token = _tokenService.GenerateRefreshToken();

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
      
        var token = _tokenService.GenerateRefreshToken();

        var isBase64 = IsBase64String(token);
        isBase64.Should().BeTrue();
    }

    [Fact]
    public void GenerateRefreshToken_EachCallReturnsUniqueToken()
    {
        
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();
        var token3 = _tokenService.GenerateRefreshToken();

        
        token1.Should().NotBe(token2);
        token2.Should().NotBe(token3);
        token1.Should().NotBe(token3);
    }

    #endregion

    #region GetPrincipalFromExpiredToken Tests

    [Fact]
    public void GetPrincipalFromExpiredToken_ValidToken_ReturnsPrincipal()
    {
        
        var user = TestHelpers.CreateTestUser();
        var token = _tokenService.GenerateAccessToken(user);

       
        var principal = _tokenService.GetPrincipalFromExpiredToken(token);

        
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.Email)?.Value.Should().Be(user.Email);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_InvalidToken_ReturnsNull()
    {
        
        var invalidToken = "this.is.not.a.valid.token";

        
        var principal = _tokenService.GetPrincipalFromExpiredToken(invalidToken);

        
        principal.Should().BeNull();
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_EmptyToken_ReturnsNull()
    {
        
        var principal = _tokenService.GetPrincipalFromExpiredToken(string.Empty);

        
        principal.Should().BeNull();
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ValidToken_ContainsUserIdClaim()
    {
       
        var user = TestHelpers.CreateTestUser();
        var token = _tokenService.GenerateAccessToken(user);

       
        var principal = _tokenService.GetPrincipalFromExpiredToken(token);

       
        principal.Should().NotBeNull();
        var userIdClaim = principal!.FindFirst(ClaimTypes.NameIdentifier);
        userIdClaim.Should().NotBeNull();
        userIdClaim!.Value.Should().Be(user.Id.ToString());
    }

    #endregion

    #region Helper Methods

    private static bool IsBase64String(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}