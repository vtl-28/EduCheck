using EduCheck.API.Controllers;
using EduCheck.Application.DTOs.Auth;
using EduCheck.Application.Interfaces;
using EduCheck.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace EduCheck.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IGoogleAuthService> _googleAuthServiceMock;
    //private readonly Mock<IFacebookAuthService> _facebookAuthServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _googleAuthServiceMock = new Mock<IGoogleAuthService>();
        //_facebookAuthServiceMock = new Mock<IFacebookAuthService>();
        _loggerMock = new Mock<ILogger<AuthController>>();

        _controller = new AuthController(
            _authServiceMock.Object,
            _loggerMock.Object,
            _googleAuthServiceMock.Object
            //_facebookAuthServiceMock.Object,
            );

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost", 7001);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region RegisterStudent Tests

    [Fact]
    public async Task RegisterStudent_ValidRequest_ReturnsOk()
    {
        var request = new StudentRegistrationRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456"
        };

        var response = new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            AccessToken = "test-token",
            RefreshToken = "test-refresh",
            User = new UserDto { Email = request.Email }
        };

        _authServiceMock.Setup(x => x.RegisterStudentAsync(request))
            .ReturnsAsync(response);

        var result = await _controller.RegisterStudent(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var authResponse = okResult.Value.Should().BeOfType<AuthResponse>().Subject;
        authResponse.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterStudent_InvalidModel_ReturnsBadRequest()
    {
        var request = new StudentRegistrationRequest();
        _controller.ModelState.AddModelError("Email", "Email is required");

        var result = await _controller.RegisterStudent(request);


        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var authResponse = badRequestResult.Value.Should().BeOfType<AuthResponse>().Subject;
        authResponse.Success.Should().BeFalse();
        authResponse.Errors.Should().Contain("Email is required");
    }

    [Fact]
    public async Task RegisterStudent_DuplicateEmail_ReturnsBadRequest()
    {
      
        var request = new StudentRegistrationRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "existing@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456"
        };

        var response = new AuthResponse
        {
            Success = false,
            Message = "Registration failed",
            Errors = new List<string> { "An account with this email already exists" }
        };

        _authServiceMock.Setup(x => x.RegisterStudentAsync(request))
            .ReturnsAsync(response);

      
        var result = await _controller.RegisterStudent(request);

        
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var authResponse = badRequestResult.Value.Should().BeOfType<AuthResponse>().Subject;
        authResponse.Success.Should().BeFalse();
    }

    #endregion

    #region RegisterAdmin Tests

    [Fact]
    public async Task RegisterAdmin_ValidRequest_ReturnsOk()
    {
       
        var request = new AdminRegistrationRequest
        {
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@dhet.gov.za",
            Password = "Admin@123456",
            ConfirmPassword = "Admin@123456",
            Department = "DHET"
        };

        var response = new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            AccessToken = "test-token",
            User = new UserDto { Email = request.Email, Role = UserRole.Admin }
        };

        _authServiceMock.Setup(x => x.RegisterAdminAsync(request))
            .ReturnsAsync(response);

     
        var result = await _controller.RegisterAdmin(request);

       
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var authResponse = okResult.Value.Should().BeOfType<AuthResponse>().Subject;
        authResponse.Success.Should().BeTrue();
        authResponse.User!.Role.Should().Be(UserRole.Admin);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        
        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "Test@123456"
        };

        var response = new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            AccessToken = "test-token",
            RefreshToken = "test-refresh"
        };

        _authServiceMock.Setup(x => x.LoginAsync(request))
            .ReturnsAsync(response);

        var result = await _controller.Login(request);

    
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var authResponse = okResult.Value.Should().BeOfType<AuthResponse>().Subject;
        authResponse.Success.Should().BeTrue();
        authResponse.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
 
        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "WrongPassword"
        };

        var response = new AuthResponse
        {
            Success = false,
            Message = "Invalid credentials",
            Errors = new List<string> { "Invalid email or password" }
        };

        _authServiceMock.Setup(x => x.LoginAsync(request))
            .ReturnsAsync(response);

   
        var result = await _controller.Login(request);

        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var authResponse = unauthorizedResult.Value.Should().BeOfType<AuthResponse>().Subject;
        authResponse.Success.Should().BeFalse();
    }

    #endregion

    #region RefreshToken Tests

    [Fact]
    public async Task RefreshToken_ValidTokens_ReturnsOk()
    {

        var request = new RefreshTokenRequest
        {
            AccessToken = "old-access-token",
            RefreshToken = "valid-refresh-token"
        };

        var response = new AuthResponse
        {
            Success = true,
            Message = "Token refreshed successfully",
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token"
        };

        _authServiceMock.Setup(x => x.RefreshTokenAsync(request))
            .ReturnsAsync(response);

        
        var result = await _controller.RefreshToken(request);

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var authResponse = okResult.Value.Should().BeOfType<AuthResponse>().Subject;
        authResponse.Success.Should().BeTrue();
        authResponse.AccessToken.Should().Be("new-access-token");
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_ReturnsUnauthorized()
    {
       
        var request = new RefreshTokenRequest
        {
            AccessToken = "invalid-token",
            RefreshToken = "invalid-refresh"
        };

        var response = new AuthResponse
        {
            Success = false,
            Message = "Invalid token",
            Errors = new List<string> { "Invalid access token" }
        };

        _authServiceMock.Setup(x => x.RefreshTokenAsync(request))
            .ReturnsAsync(response);

        
        var result = await _controller.RefreshToken(request);

        
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_ValidToken_ReturnsOk()
    {
       
        var request = new LogoutRequest
        {
            RefreshToken = "valid-refresh-token"
        };

        _authServiceMock.Setup(x => x.RevokeRefreshTokenAsync(request.RefreshToken))
            .ReturnsAsync(true);

       
        var result = await _controller.Logout(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
    }

    [Fact]
    public async Task Logout_InvalidToken_ReturnsBadRequest()
    {
        
        var request = new LogoutRequest
        {
            RefreshToken = "invalid-token"
        };

        _authServiceMock.Setup(x => x.RevokeRefreshTokenAsync(request.RefreshToken))
            .ReturnsAsync(false);

        var result = await _controller.Logout(request);

        
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region LogoutAll Tests

    [Fact]
    public async Task LogoutAll_AuthenticatedUser_ReturnsOk()
    {
        
        var userId = Guid.NewGuid();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "Test"));

        _controller.ControllerContext.HttpContext.User = claims;

        _authServiceMock.Setup(x => x.RevokeAllUserTokensAsync(userId))
            .ReturnsAsync(true);

       
        var result = await _controller.LogoutAll();

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task LogoutAll_NoUserClaim_ReturnsUnauthorized()
    {
        
        var result = await _controller.LogoutAll();

      
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region GetCurrentUser Tests

    [Fact]
    public async Task GetCurrentUser_AuthenticatedUser_ReturnsUser()
    {
       
        var userId = Guid.NewGuid();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "john@example.com"),
            new Claim(ClaimTypes.GivenName, "John"),
            new Claim(ClaimTypes.Surname, "Doe"),
            new Claim(ClaimTypes.Role, "Student")
        }, "Test"));

        _controller.ControllerContext.HttpContext.User = claims;

        
        var result = await _controller.GetCurrentUser();

      
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
    }

    [Fact]
    public async Task GetCurrentUser_NotAuthenticated_ReturnsUnauthorized()
    {
        
        var result = await _controller.GetCurrentUser();

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region Google OAuth Tests

    [Fact]
    public void GoogleLogin_ReturnsAuthorizationUrl()
    {
        
        var expectedUrl = "https://accounts.google.com/oauth?client_id=test";
        _googleAuthServiceMock.Setup(x => x.GetAuthorizationUrl(It.IsAny<string>()))
            .Returns(expectedUrl);

      
        var result = _controller.GoogleLogin();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
    }

    #endregion

    //#region Facebook OAuth Tests

    //[Fact]
    //public void FacebookLogin_ReturnsAuthorizationUrl()
    //{
    //    // Arrange
    //    var expectedUrl = "https://www.facebook.com/dialog/oauth?client_id=test";
    //    _facebookAuthServiceMock.Setup(x => x.GetAuthorizationUrl(It.IsAny<string>()))
    //        .Returns(expectedUrl);

    //    // Act
    //    var result = _controller.FacebookLogin();

    //    // Assert
    //    var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
    //}

    //#endregion
}