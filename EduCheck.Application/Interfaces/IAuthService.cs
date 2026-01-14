using EduCheck.Application.DTOs.Auth;

namespace EduCheck.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterStudentAsync(StudentRegistrationRequest request);
    Task<AuthResponse> RegisterAdminAsync(AdminRegistrationRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<AuthResponse> ExternalLoginAsync(ExternalAuthRequest request);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken);
    Task<bool> RevokeAllUserTokensAsync(Guid userId);
}