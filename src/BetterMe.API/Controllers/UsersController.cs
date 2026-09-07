using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BetterMe.Infrastructure.Data;
using BetterMe.Infrastructure.Entities;
using BetterMe.Shared.DTOs.Users;
using BetterMe.Shared.Enums;
using System.Security.Claims;
using System.Text.Json;

namespace BetterMe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public UsersController(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetUserId();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
        return Ok(MapToDto(user, sub?.Tier));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.Bio != null) user.Bio = request.Bio;
        if (request.ProfileImageUrl != null) user.ProfileImageUrl = request.ProfileImageUrl;

        await _userManager.UpdateAsync(user);
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
        return Ok(MapToDto(user, sub?.Tier));
    }

    [HttpPost("me/onboarding")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> CompleteOnboarding([FromBody] OnboardingRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetUserId();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (user.IsOnboarded)
            return BadRequest("Onboarding already completed.");

        var existing = await _db.OnboardingResponses.FirstOrDefaultAsync(o => o.UserId == userId);
        if (existing != null) _db.OnboardingResponses.Remove(existing);

        _db.OnboardingResponses.Add(new OnboardingResponse
        {
            UserId = userId,
            PrimaryGoals = JsonSerializer.Serialize(request.PrimaryGoals),
            CurrentChallenges = JsonSerializer.Serialize(request.CurrentChallenges),
            PreferredResourceTypes = JsonSerializer.Serialize(request.PreferredResourceTypes),
            SleepQuality = request.SleepQuality,
            StressLevel = request.StressLevel,
            HasPreviousTherapy = request.HasPreviousTherapy
        });

        user.IsOnboarded = true;
        await _userManager.UpdateAsync(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet]
    [Authorize(Roles = "Psychologist,Admin")]
    public async Task<IActionResult> GetPatients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var query = _userManager.Users
            .Where(u => u.Role == UserRole.Patient)
            .Include(u => u.Subscription)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FirstName.Contains(search) || u.LastName.Contains(search) || u.Email!.Contains(search));

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = users.Select(u => MapToDto(u, u.Subscription?.Tier)).ToList();
        return Ok(new { items = dtos, totalCount = total, page, pageSize });
    }

    [HttpGet("psychologists")]
    [Authorize(Roles = "Patient,Admin")]
    public async Task<IActionResult> GetPsychologists()
    {
        var psychologists = await _userManager.Users
            .Where(u => u.Role == UserRole.Psychologist)
            .OrderBy(u => u.FirstName)
            .ToListAsync();

        return Ok(psychologists.Select(u => MapToDto(u, null)));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Psychologist,Admin")]
    public async Task<IActionResult> GetPatient(string id)
    {
        var user = await _userManager.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();
        return Ok(MapToDto(user, user.Subscription?.Tier));
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException();

    private static UserDto MapToDto(ApplicationUser user, SubscriptionTier? tier) => new()
    {
        Id = user.Id,
        Email = user.Email!,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Role = user.Role,
        Bio = user.Bio,
        ProfileImageUrl = user.ProfileImageUrl,
        IsOnboarded = user.IsOnboarded,
        CreatedAt = user.CreatedAt,
        SubscriptionTier = tier
    };
}
