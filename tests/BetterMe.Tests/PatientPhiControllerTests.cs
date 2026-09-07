using Microsoft.AspNetCore.Mvc;
using BetterMe.API.Controllers;
using BetterMe.Infrastructure.Entities;
using BetterMe.Shared.DTOs.CheckIns;
using BetterMe.Shared.Enums;

namespace BetterMe.Tests;

public class PatientPhiControllerTests
{
    [Fact]
    public async Task GetPatientMood_forbids_unrelated_psychologist()
    {
        await using var db = TestHarness.CreateDb();
        await SeedCareTeam(db);
        db.MoodEntries.Add(new MoodEntry
        {
            Id = Guid.NewGuid(),
            UserId = "patient-1",
            Level = MoodLevel.Good,
            Note = "secret",
            RecordedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = new MoodController(db);
        TestHarness.SetUser(controller, "psych-2", "Psychologist");

        var result = await controller.GetPatientMood("patient-1");

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetPatientMood_allows_in_relationship_psychologist()
    {
        await using var db = TestHarness.CreateDb();
        await SeedCareTeam(db);
        db.MoodEntries.Add(new MoodEntry
        {
            Id = Guid.NewGuid(),
            UserId = "patient-1",
            Level = MoodLevel.Good,
            Note = "doing ok",
            RecordedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = new MoodController(db);
        TestHarness.SetUser(controller, "psych-1", "Psychologist");

        var result = await controller.GetPatientMood("patient-1");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task SendCheckIn_forbids_unrelated_psychologist()
    {
        await using var db = TestHarness.CreateDb();
        await SeedCareTeam(db);

        var controller = new CheckInsController(db);
        TestHarness.SetUser(controller, "psych-2", "Psychologist");

        var result = await controller.SendCheckIn(new SendCheckInRequest
        {
            PatientId = "patient-1",
            Message = "How are you?"
        });

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(db.CheckIns);
    }

    [Fact]
    public async Task SendCheckIn_allows_in_relationship_psychologist()
    {
        await using var db = TestHarness.CreateDb();
        await SeedCareTeam(db);

        var controller = new CheckInsController(db);
        TestHarness.SetUser(controller, "psych-1", "Psychologist");

        var result = await controller.SendCheckIn(new SendCheckInRequest
        {
            PatientId = "patient-1",
            Message = "How are you?"
        });

        Assert.IsType<CreatedAtActionResult>(result);
        Assert.Single(db.CheckIns);
    }

    [Fact]
    public async Task SendCheckIn_rejects_missing_patient()
    {
        await using var db = TestHarness.CreateDb();
        db.Users.Add(TestHarness.User("psych-1", UserRole.Psychologist));
        await db.SaveChangesAsync();

        var controller = new CheckInsController(db);
        TestHarness.SetUser(controller, "psych-1", "Psychologist");

        var result = await controller.SendCheckIn(new SendCheckInRequest
        {
            PatientId = "nobody",
            Message = "Hello"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetPatient_forbids_unrelated_psychologist()
    {
        await using var db = TestHarness.CreateDb();
        await SeedCareTeam(db);

        var controller = new UsersController(null!, db);
        TestHarness.SetUser(controller, "psych-2", "Psychologist");

        var result = await controller.GetPatient("patient-1");

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetPatient_allows_in_relationship_psychologist()
    {
        await using var db = TestHarness.CreateDb();
        await SeedCareTeam(db);

        var controller = new UsersController(null!, db);
        TestHarness.SetUser(controller, "psych-1", "Psychologist");

        var result = await controller.GetPatient("patient-1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetPatient_hides_non_patient_users()
    {
        await using var db = TestHarness.CreateDb();
        db.Users.Add(TestHarness.User("psych-1", UserRole.Psychologist));
        await db.SaveChangesAsync();

        var controller = new UsersController(null!, db);
        TestHarness.SetUser(controller, "admin-1", "Admin");

        var result = await controller.GetPatient("psych-1");

        Assert.IsType<NotFoundResult>(result);
    }

    private static async Task SeedCareTeam(BetterMe.Infrastructure.Data.AppDbContext db)
    {
        db.Users.AddRange(
            TestHarness.User("patient-1", UserRole.Patient),
            TestHarness.User("psych-1", UserRole.Psychologist),
            TestHarness.User("psych-2", UserRole.Psychologist));
        db.Sessions.Add(TestHarness.Session("patient-1", "psych-1"));
        await db.SaveChangesAsync();
    }
}
