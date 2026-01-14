using EduCheck.Application.DTOs.Auth;

namespace EduCheck.Application.Interfaces;

public interface IGoogleAuthService
{
    string GetAuthorizationUrl(string redirectUri);
    Task<AuthResponse> AuthenticateAsync(string code, string redirectUri);
}