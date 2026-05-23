using Suggestme.Infrastructure.Entities;

namespace Suggestme.Infrastructure.Services;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    Task StoreRefreshTokenAsync(string userId, string token, int expiryDays);
    Task<bool> ValidateRefreshTokenAsync(string userId, string token);
    Task RevokeRefreshTokenAsync(string userId, string token);
}
