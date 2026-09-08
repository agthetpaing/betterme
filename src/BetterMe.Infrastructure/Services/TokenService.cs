using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using BetterMe.Infrastructure.Data;
using BetterMe.Infrastructure.Entities;

namespace BetterMe.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IConfiguration config, AppDbContext db, ILogger<TokenService> logger)
    {
        _config = config;
        _db = db;
        _logger = logger;
    }

    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var expiry = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public async Task StoreRefreshTokenAsync(string userId, string token, int expiryDays)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            CreatedAt = DateTime.UtcNow
        };
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ValidateRefreshTokenAsync(string userId, string token)
    {
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token);

        return storedToken != null && storedToken.IsActive;
    }

    public async Task RevokeRefreshTokenAsync(string userId, string token)
    {
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token);

        if (storedToken != null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public string? GetUserIdFromAccessToken(string token)
    {
        try
        {
            var key = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is not configured.");

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudience = _config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse user id from access token");
            return null;
        }
    }
}
