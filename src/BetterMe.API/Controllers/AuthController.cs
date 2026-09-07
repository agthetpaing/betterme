using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BetterMe.Infrastructure.Entities;
using BetterMe.Infrastructure.Services;
using BetterMe.Shared.Auth;
using BetterMe.Shared.DTOs.Auth;
using BetterMe.Shared.DTOs.Users;
using BetterMe.Shared.Enums;
using BetterMe.Infrastructure.Data;

namespace BetterMe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IConfiguration config,
        AppDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _config = config;
        _db = db;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!RegistrationRoles.IsSelfAssignable(request.Role))
            return BadRequest("Invalid role.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        var roleName = request.Role.ToString();
        var roleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return BadRequest(roleResult.Errors.Select(e => e.Description));
        }

        // Seed a Free subscription for patients
        if (request.Role == UserRole.Patient)
        {
            _db.Subscriptions.Add(new Subscription
            {
                UserId = user.Id,
                Tier = SubscriptionTier.Free,
                Status = "free"
            });
            await _db.SaveChangesAsync();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var expiryDays = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "30");
        await _tokenService.StoreRefreshTokenAsync(user.Id, refreshToken, expiryDays);

        return CreatedAtAction(nameof(Register), BuildAuthResponse(user, accessToken, refreshToken));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) return Unauthorized("Invalid credentials.");

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded) return Unauthorized("Invalid credentials.");

        user.LastActiveAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var expiryDays = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "30");
        await _tokenService.StoreRefreshTokenAsync(user.Id, refreshToken, expiryDays);

        return Ok(BuildAuthResponse(user, accessToken, refreshToken));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        // Extract userId from the (expired) access token in the Authorization header
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Bearer "))
            return Unauthorized("Missing access token.");

        var oldAccessToken = authHeader["Bearer ".Length..];
        var userId = _tokenService.GetUserIdFromAccessToken(oldAccessToken);
        if (userId == null) return Unauthorized("Invalid access token.");

        var isValid = await _tokenService.ValidateRefreshTokenAsync(userId, request.RefreshToken);
        if (!isValid) return Unauthorized("Invalid or expired refresh token.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        await _tokenService.RevokeRefreshTokenAsync(userId, request.RefreshToken);

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var expiryDays = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "30");
        await _tokenService.StoreRefreshTokenAsync(user.Id, newRefreshToken, expiryDays);

        return Ok(BuildAuthResponse(user, newAccessToken, newRefreshToken));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        if (userId != null)
            await _tokenService.RevokeRefreshTokenAsync(userId, request.RefreshToken);

        return NoContent();
    }

    private AuthResponse BuildAuthResponse(ApplicationUser user, string accessToken, string refreshToken)
    {
        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                IsOnboarded = user.IsOnboarded,
                CreatedAt = user.CreatedAt
            }
        };
    }
}
