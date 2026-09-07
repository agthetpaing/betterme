using BetterMe.Infrastructure.Security;
using BetterMe.Shared.Enums;

namespace BetterMe.Tests;

public class PatientAccessTests
{
    [Fact]
    public async Task Admin_can_access_any_patient()
    {
        await using var db = TestHarness.CreateDb();

        var allowed = await PatientAccess.CanAccessAsync(db, "psych-other", isAdmin: true, "patient-1");

        Assert.True(allowed);
    }

    [Fact]
    public async Task Psychologist_can_access_booked_patient()
    {
        await using var db = TestHarness.CreateDb();
        db.Users.AddRange(
            TestHarness.User("patient-1", UserRole.Patient),
            TestHarness.User("psych-1", UserRole.Psychologist));
        db.Sessions.Add(TestHarness.Session("patient-1", "psych-1"));
        await db.SaveChangesAsync();

        var allowed = await PatientAccess.CanAccessAsync(db, "psych-1", isAdmin: false, "patient-1");

        Assert.True(allowed);
    }

    [Fact]
    public async Task Psychologist_cannot_access_unrelated_patient()
    {
        await using var db = TestHarness.CreateDb();
        db.Users.AddRange(
            TestHarness.User("patient-1", UserRole.Patient),
            TestHarness.User("psych-1", UserRole.Psychologist),
            TestHarness.User("psych-2", UserRole.Psychologist));
        db.Sessions.Add(TestHarness.Session("patient-1", "psych-1"));
        await db.SaveChangesAsync();

        var allowed = await PatientAccess.CanAccessAsync(db, "psych-2", isAdmin: false, "patient-1");

        Assert.False(allowed);
    }
}
