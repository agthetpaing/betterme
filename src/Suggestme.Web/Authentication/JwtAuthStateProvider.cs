using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Suggestme.Web.Authentication;

public class JwtAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public JwtAuthStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("accessToken");
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var claims = ParseClaimsFromJwt(token);
        var expiry = claims.FirstOrDefault(c => c.Type == "exp");
        if (expiry != null && long.TryParse(expiry.Value, out var exp))
        {
            var expiryDate = DateTimeOffset.FromUnixTimeSeconds(exp);
            if (expiryDate < DateTimeOffset.UtcNow)
                return Anonymous;
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyAuthStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3) return Enumerable.Empty<Claim>();

        var payload = parts[1];
        // Pad base64
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var bytes = Convert.FromBase64String(payload);
        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(bytes);
        if (json == null) return Enumerable.Empty<Claim>();

        var claims = new List<Claim>();
        foreach (var kvp in json)
        {
            if (kvp.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in kvp.Value.EnumerateArray())
                    claims.Add(new Claim(kvp.Key, item.GetString() ?? string.Empty));
            }
            else
            {
                claims.Add(new Claim(kvp.Key, kvp.Value.ToString()));
            }
        }

        // Map "role" claim to ClaimTypes.Role so [Authorize(Roles=...)] works
        var roleClaims = claims.Where(c => c.Type == "role").ToList();
        foreach (var rc in roleClaims)
            claims.Add(new Claim(ClaimTypes.Role, rc.Value));

        // Map "sub" to NameIdentifier
        var sub = claims.FirstOrDefault(c => c.Type == "sub");
        if (sub != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, sub.Value));

        return claims;
    }
}
