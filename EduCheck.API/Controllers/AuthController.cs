using EduCheck.Application.DTOs.Auth;
using EduCheck.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduCheck.Application.Interfaces;

namespace EduCheck.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IGoogleAuthService _googleAuthService;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, IGoogleAuthService googleAuthService)
    {
        _authService = authService;
        _logger = logger;
        _googleAuthService = googleAuthService;
    }

    /// <summary>
    /// Register a new student account
    /// </summary>
    [HttpPost("register/student")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterStudent([FromBody] StudentRegistrationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Validation failed",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var result = await _authService.RegisterStudentAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Register a new admin account
    /// </summary>
    [HttpPost("register/admin")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterAdmin([FromBody] AdminRegistrationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Validation failed",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var result = await _authService.RegisterAdminAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Validation failed",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var result = await _authService.LoginAsync(request);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Validation failed",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var result = await _authService.RefreshTokenAsync(request);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// External login with Google or Facebook
    /// </summary>
    [HttpPost("external-login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalAuthRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Validation failed",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var result = await _authService.ExternalLoginAsync(request);

        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Logout - revoke current refresh token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var result = await _authService.RevokeRefreshTokenAsync(request.RefreshToken);

        if (!result)
        {
            return BadRequest(new { Success = false, Message = "Invalid refresh token" });
        }

        return Ok(new { Success = true, Message = "Logged out successfully" });
    }

    /// <summary>
    /// Logout from all devices - revoke all refresh tokens
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized(new { Success = false, Message = "Invalid user" });
        }

        await _authService.RevokeAllUserTokensAsync(userGuid);

        return Ok(new { Success = true, Message = "Logged out from all devices successfully" });
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Success = false, Message = "Invalid user" });
        }

        var userDto = new UserDto
        {
            Id = Guid.Parse(userId),
            Email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            FirstName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty,
            LastName = User.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty,
            Role = Enum.Parse<Domain.Enums.UserRole>(User.FindFirst(ClaimTypes.Role)?.Value ?? "Student")
        };

        return Ok(new { Success = true, User = userDto });
    }


    /// <summary>
    /// Get Google OAuth authorization URL
    /// </summary>
    [HttpGet("google-login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GoogleLogin()
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/Auth/google-callback";
        var authUrl = _googleAuthService.GetAuthorizationUrl(redirectUri);

        return Ok(new { AuthorizationUrl = authUrl });
    }

    /// <summary>
    /// Google OAuth callback - exchange code for tokens
    /// </summary>
    [HttpGet("google-callback")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("Google OAuth error: {Error}", error);
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Google authentication failed",
                Errors = new List<string> { error }
            });
        }

        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Authorization code is required",
                Errors = new List<string> { "No authorization code provided" }
            });
        }

        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/Auth/google-callback";
        var result = await _googleAuthService.AuthenticateAsync(code, redirectUri);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Google OAuth - authenticate with code (for mobile/SPA apps)
    /// </summary>
    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Validation failed",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/Auth/google-callback";
        var result = await _googleAuthService.AuthenticateAsync(request.Code, redirectUri);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}