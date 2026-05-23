using Suggestme.Shared.DTOs.Auth;

namespace Suggestme.Web.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<(AuthResponse? Response, string? Error)> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<string?> GetAccessTokenAsync();
}
