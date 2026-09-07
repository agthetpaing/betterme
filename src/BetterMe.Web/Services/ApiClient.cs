using System.Net.Http.Headers;
using System.Net.Http.Json;
using BetterMe.Shared.DTOs.CheckIns;
using BetterMe.Shared.DTOs.Mood;
using BetterMe.Shared.DTOs.Resources;
using BetterMe.Shared.DTOs.Sessions;
using BetterMe.Shared.DTOs.Users;
using BetterMe.Shared.Enums;

namespace BetterMe.Web.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private readonly IAuthService _auth;

    public ApiClient(HttpClient http, IAuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _auth.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // Users
    public async Task<UserDto?> GetCurrentUserAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<UserDto>("api/users/me");
    }

    public async Task<UserDto?> UpdateProfileAsync(UpdateProfileRequest request)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync("api/users/me", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<UserDto>() : null;
    }

    public async Task CompleteOnboardingAsync(OnboardingRequest request)
    {
        await SetAuthHeaderAsync();
        await _http.PostAsJsonAsync("api/users/me/onboarding", request);
    }

    public async Task<List<UserDto>> GetPatientsAsync(int page = 1, int pageSize = 20, string? search = null)
    {
        await SetAuthHeaderAsync();
        var url = $"api/users?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
        var result = await _http.GetFromJsonAsync<PatientsResult>(url);
        return result?.Items ?? new();
    }

    public async Task<UserDto?> GetPatientAsync(string id)
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<UserDto>($"api/users/{id}");
    }

    // Resources
    public async Task<PagedResult<ResourceDto>> GetResourcesAsync(ResourceType? type = null, string? tag = null, int page = 1, int pageSize = 20)
    {
        await SetAuthHeaderAsync();
        var url = $"api/resources?page={page}&pageSize={pageSize}";
        if (type.HasValue) url += $"&type={type.Value}";
        if (!string.IsNullOrEmpty(tag)) url += $"&tag={Uri.EscapeDataString(tag)}";
        return await _http.GetFromJsonAsync<PagedResult<ResourceDto>>(url) ?? new();
    }

    public async Task<ResourceDto?> GetResourceAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<ResourceDto>($"api/resources/{id}");
    }

    public async Task<ResourceDto?> CreateResourceAsync(CreateResourceRequest request)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/resources", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<ResourceDto>() : null;
    }

    public async Task UpdateResourceAsync(Guid id, CreateResourceRequest request)
    {
        await SetAuthHeaderAsync();
        await _http.PutAsJsonAsync($"api/resources/{id}", request);
    }

    public async Task DeleteResourceAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        await _http.DeleteAsync($"api/resources/{id}");
    }

    public async Task<List<ResourceTagDto>> GetTagsAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<ResourceTagDto>>("api/resources/tags") ?? new();
    }

    public async Task<List<UserDto>> GetPsychologistsAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<UserDto>>("api/users/psychologists") ?? new();
    }

    // Sessions
    public async Task<List<SessionDto>> GetSessionsAsync(SessionStatus? status = null)
    {
        await SetAuthHeaderAsync();
        var url = "api/sessions";
        if (status.HasValue) url += $"?status={status.Value}";
        return await _http.GetFromJsonAsync<List<SessionDto>>(url) ?? new();
    }

    public async Task<SessionDto?> BookSessionAsync(BookSessionRequest request)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/sessions", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<SessionDto>() : null;
    }

    public async Task ConfirmSessionAsync(Guid id, ConfirmSessionRequest request)
    {
        await SetAuthHeaderAsync();
        await _http.PutAsJsonAsync($"api/sessions/{id}/confirm", request);
    }

    public async Task DeclineSessionAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        await _http.PutAsJsonAsync($"api/sessions/{id}/decline", new { });
    }

    public async Task CancelSessionAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        await _http.PutAsJsonAsync($"api/sessions/{id}/cancel", new { });
    }

    public async Task CompleteSessionAsync(Guid id, string? notes = null)
    {
        await SetAuthHeaderAsync();
        await _http.PutAsJsonAsync($"api/sessions/{id}/complete", new { psychologistNotes = notes });
    }

    // Mood
    public async Task<List<MoodEntryDto>> GetMyMoodAsync(int days = 60)
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<MoodEntryDto>>($"api/mood?days={days}") ?? new();
    }

    public async Task LogMoodAsync(LogMoodRequest request)
    {
        await SetAuthHeaderAsync();
        await _http.PostAsJsonAsync("api/mood", request);
    }

    public async Task<List<MoodEntryDto>> GetPatientMoodAsync(string patientId, int days = 60)
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<MoodEntryDto>>($"api/mood/patient/{patientId}?days={days}") ?? new();
    }

    // Check-ins
    public async Task<List<CheckInDto>> GetMyCheckInsAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<CheckInDto>>("api/checkins") ?? new();
    }

    public async Task SendCheckInAsync(SendCheckInRequest request)
    {
        await SetAuthHeaderAsync();
        await _http.PostAsJsonAsync("api/checkins", request);
    }

    public async Task MarkCheckInReadAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        await _http.PutAsync($"api/checkins/{id}/read", null);
    }

    private record PatientsResult(List<UserDto> Items, int TotalCount, int Page, int PageSize);
}
