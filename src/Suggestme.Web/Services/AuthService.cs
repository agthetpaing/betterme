using System.Net.Http.Json;
using Blazored.LocalStorage;
using Suggestme.Shared.DTOs.Auth;
using Suggestme.Web.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

namespace Suggestme.Web.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;

    public AuthService(HttpClient http, ILocalStorageService localStorage, AuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", request);
            if (!response.IsSuccessStatusCode) return null;

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth == null) return null;

            await _localStorage.SetItemAsStringAsync("accessToken", auth.AccessToken);
            await _localStorage.SetItemAsStringAsync("refreshToken", auth.RefreshToken);

            ((JwtAuthStateProvider)_authStateProvider).NotifyAuthStateChanged();
            return auth;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            // API unreachable, timeout, or bad response
            return null;
        }
    }

    public async Task<(AuthResponse? Response, string? Error)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", request);
            if (!response.IsSuccessStatusCode)
            {
                var errors = await response.Content.ReadFromJsonAsync<List<string>>();
                var message = errors?.FirstOrDefault() ?? "Registration failed. Please try again.";
                return (null, message);
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth == null) return (null, "Unexpected server response.");

            await _localStorage.SetItemAsStringAsync("accessToken", auth.AccessToken);
            await _localStorage.SetItemAsStringAsync("refreshToken", auth.RefreshToken);

            ((JwtAuthStateProvider)_authStateProvider).NotifyAuthStateChanged();
            return (auth, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return (null, "Cannot reach the server. Please check your connection.");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var refreshToken = await _localStorage.GetItemAsStringAsync("refreshToken");
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var token = await _localStorage.GetItemAsStringAsync("accessToken");
                _http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                await _http.PostAsJsonAsync("api/auth/logout", new RefreshTokenRequest { RefreshToken = refreshToken });
            }
        }
        catch { /* best-effort logout */ }
        finally
        {
            await _localStorage.RemoveItemAsync("accessToken");
            await _localStorage.RemoveItemAsync("refreshToken");
            ((JwtAuthStateProvider)_authStateProvider).NotifyAuthStateChanged();
        }
    }

    public async Task<string?> GetAccessTokenAsync() =>
        await _localStorage.GetItemAsStringAsync("accessToken");
}
