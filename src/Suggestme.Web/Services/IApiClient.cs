using Suggestme.Shared.DTOs.CheckIns;
using Suggestme.Shared.DTOs.Mood;
using Suggestme.Shared.DTOs.Resources;
using Suggestme.Shared.DTOs.Sessions;
using Suggestme.Shared.DTOs.Users;
using Suggestme.Shared.Enums;

namespace Suggestme.Web.Services;

public interface IApiClient
{
    // Users
    Task<UserDto?> GetCurrentUserAsync();
    Task<UserDto?> UpdateProfileAsync(UpdateProfileRequest request);
    Task CompleteOnboardingAsync(OnboardingRequest request);
    Task<List<UserDto>> GetPatientsAsync(int page = 1, int pageSize = 20, string? search = null);
    Task<UserDto?> GetPatientAsync(string id);
    Task<List<UserDto>> GetPsychologistsAsync();

    // Resources
    Task<PagedResult<ResourceDto>> GetResourcesAsync(ResourceType? type = null, string? tag = null, int page = 1, int pageSize = 20);
    Task<ResourceDto?> GetResourceAsync(Guid id);
    Task<ResourceDto?> CreateResourceAsync(CreateResourceRequest request);
    Task UpdateResourceAsync(Guid id, CreateResourceRequest request);
    Task DeleteResourceAsync(Guid id);
    Task<List<ResourceTagDto>> GetTagsAsync();

    // Sessions
    Task<List<SessionDto>> GetSessionsAsync(SessionStatus? status = null);
    Task<SessionDto?> BookSessionAsync(BookSessionRequest request);
    Task ConfirmSessionAsync(Guid id, ConfirmSessionRequest request);
    Task DeclineSessionAsync(Guid id);
    Task CancelSessionAsync(Guid id);
    Task CompleteSessionAsync(Guid id, string? notes = null);

    // Mood
    Task<List<MoodEntryDto>> GetMyMoodAsync(int days = 60);
    Task LogMoodAsync(LogMoodRequest request);
    Task<List<MoodEntryDto>> GetPatientMoodAsync(string patientId, int days = 60);

    // Check-ins
    Task<List<CheckInDto>> GetMyCheckInsAsync();
    Task SendCheckInAsync(SendCheckInRequest request);
    Task MarkCheckInReadAsync(Guid id);
}
