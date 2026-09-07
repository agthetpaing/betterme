using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using BetterMe.Infrastructure.Entities;
using BetterMe.Infrastructure.Services;
using BetterMe.Shared.Enums;

namespace BetterMe.Tests;

public class TokenServiceTests
{
    private const string Key = "UNIT_TEST_SIGNING_KEY_MUST_BE_32CH!!";
    private const string Issuer = "BetterMe.API";
    private const string Audience = "BetterMe.Web";

    [Fact]
    public void GetUserIdFromAccessToken_accepts_valid_expired_token()
    {
        var service = CreateService(expiryMinutes: "-1");
        var token = service.GenerateAccessToken(TestUser("user-1"), new[] { "Patient" });

        var userId = service.GetUserIdFromAccessToken(token);

        Assert.Equal("user-1", userId);
    }

    [Fact]
    public void GetUserIdFromAccessToken_rejects_unsigned_token()
    {
        var service = CreateService();
        var unsigned = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, "user-1") },
            expires: DateTime.UtcNow.AddHours(1)));

        Assert.Null(service.GetUserIdFromAccessToken(unsigned));
    }

    [Fact]
    public void GetUserIdFromAccessToken_rejects_wrong_signing_key()
    {
        var other = CreateService(key: "DIFFERENT_SIGNING_KEY_MUST_BE_32CH!!");
        var token = other.GenerateAccessToken(TestUser("user-1"), new[] { "Patient" });

        Assert.Null(CreateService().GetUserIdFromAccessToken(token));
    }

    [Fact]
    public void GetUserIdFromAccessToken_rejects_wrong_issuer()
    {
        var forged = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: "evil.example",
            audience: Audience,
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, "user-1") },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
                SecurityAlgorithms.HmacSha256)));

        Assert.Null(CreateService().GetUserIdFromAccessToken(forged));
    }

    private static TokenService CreateService(string key = Key, string expiryMinutes = "60")
    {
        IConfiguration config = new StaticConfig(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = key,
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:AccessTokenExpiryMinutes"] = expiryMinutes
        });

        return new TokenService(config, TestHarness.CreateDb());
    }

    private static ApplicationUser TestUser(string id) =>
        TestHarness.User(id, UserRole.Patient);

    private sealed class StaticConfig : IConfiguration
    {
        private readonly Dictionary<string, string?> _values;

        public StaticConfig(Dictionary<string, string?> values) => _values = values;

        public string? this[string key]
        {
            get => _values.TryGetValue(key, out var value) ? value : null;
            set => _values[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();

        public IChangeToken GetReloadToken() => new NoopChangeToken();

        public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
    }

    private sealed class NoopChangeToken : IChangeToken
    {
        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => false;
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) =>
            EmptyDisposable.Instance;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();
        public void Dispose() { }
    }
}
