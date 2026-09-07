using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BetterMe.Infrastructure.Data;
using BetterMe.Infrastructure.Entities;
using BetterMe.Shared.Enums;

namespace BetterMe.Tests;

internal static class TestHarness
{
    public static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static ApplicationUser User(string id, UserRole role, string first = "Ada", string last = "Lovelace") =>
        new()
        {
            Id = id,
            UserName = $"{id}@test.local",
            Email = $"{id}@test.local",
            FirstName = first,
            LastName = last,
            Role = role,
            EmailConfirmed = true
        };

    public static Session Session(
        string patientId,
        string psychologistId,
        SessionStatus status = SessionStatus.Pending,
        string? psychologistNotes = "private clinical note") =>
        new()
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            PsychologistId = psychologistId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 60,
            Status = status,
            PsychologistNotes = psychologistNotes
        };

    public static void SetUser(ControllerBase controller, string userId, string role)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        }, "Test");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }
}
