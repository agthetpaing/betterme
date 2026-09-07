using BetterMe.Shared.DTOs.Auth;

namespace BetterMe.Web.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<(AuthResponse? Response, string? Error)> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<string?> GetAccessTokenAsync();
}
