using Microsoft.AspNetCore.Mvc;
using BetterMe.API.Controllers;
using BetterMe.Infrastructure.Entities;
using BetterMe.Shared.DTOs.Resources;
using BetterMe.Shared.DTOs.Sessions;
using BetterMe.Shared.Enums;

namespace BetterMe.Tests;

public class SessionsControllerTests
{
    [Fact]
    public async Task GetSession_hides_psychologist_notes_from_patient()
    {
        await using var db = TestHarness.CreateDb();
        var session = await SeedSession(db);
        var controller = new SessionsController(db);
        TestHarness.SetUser(controller, "patient-1", "Patient");

        var result = await controller.GetSession(session.Id);

        var dto = Assert.IsType<SessionDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Null(dto.PsychologistNotes);
    }

    [Fact]
    public async Task GetSession_shows_psychologist_notes_to_psychologist()
    {
        await using var db = TestHarness.CreateDb();
        var session = await SeedSession(db);
        var controller = new SessionsController(db);
        TestHarness.SetUser(controller, "psych-1", "Psychologist");

        var result = await controller.GetSession(session.Id);

        var dto = Assert.IsType<SessionDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("private clinical note", dto.PsychologistNotes);
    }

    [Fact]
    public async Task Confirm_rejects_javascript_meeting_url()
    {
        await using var db = TestHarness.CreateDb();
        var session = await SeedSession(db);
        var controller = new SessionsController(db);
        TestHarness.SetUser(controller, "psych-1", "Psychologist");

        var result = await controller.Confirm(session.Id, new ConfirmSessionRequest
        {
            MeetingUrl = "javascript:alert(1)"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Meeting URL must be an https address.", badRequest.Value);
    }

    [Fact]
    public async Task Confirm_rejects_http_meeting_url()
    {
        await using var db = TestHarness.CreateDb();
        var session = await SeedSession(db);
        var controller = new SessionsController(db);
        TestHarness.SetUser(controller, "psych-1", "Psychologist");

        var result = await controller.Confirm(session.Id, new ConfirmSessionRequest
        {
            MeetingUrl = "http://meet.example/x"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Confirm_accepts_https_meeting_url()
    {
        await using var db = TestHarness.CreateDb();
        var session = await SeedSession(db);
        var controller = new SessionsController(db);
        TestHarness.SetUser(controller, "psych-1", "Psychologist");

        var result = await controller.Confirm(session.Id, new ConfirmSessionRequest
        {
            MeetingUrl = "https://meet.example/x"
        });

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("https://meet.example/x", db.Sessions.Single().MeetingUrl);
    }

    private static async Task<Session> SeedSession(BetterMe.Infrastructure.Data.AppDbContext db)
    {
        db.Users.AddRange(
            TestHarness.User("patient-1", UserRole.Patient),
            TestHarness.User("psych-1", UserRole.Psychologist));
        var session = TestHarness.Session("patient-1", "psych-1");
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }
}

public class ResourcesControllerTests
{
    [Fact]
    public async Task Delete_forbids_non_author_psychologist()
    {
        await using var db = TestHarness.CreateDb();
        var resource = await SeedResource(db, authorId: "psych-1");
        var controller = new ResourcesController(db);
        TestHarness.SetUser(controller, "psych-2", "Psychologist");

        var result = await controller.DeleteResource(resource.Id);

        Assert.IsType<ForbidResult>(result);
        Assert.Single(db.Resources);
    }

    [Fact]
    public async Task Delete_allows_author()
    {
        await using var db = TestHarness.CreateDb();
        var resource = await SeedResource(db, authorId: "psych-1");
        var controller = new ResourcesController(db);
        TestHarness.SetUser(controller, "psych-1", "Psychologist");

        var result = await controller.DeleteResource(resource.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.Resources);
    }

    [Fact]
    public async Task Update_forbids_non_author_psychologist()
    {
        await using var db = TestHarness.CreateDb();
        var resource = await SeedResource(db, authorId: "psych-1");
        var controller = new ResourcesController(db);
        TestHarness.SetUser(controller, "psych-2", "Psychologist");

        var result = await controller.UpdateResource(resource.Id, new CreateResourceRequest
        {
            Title = "Hijacked",
            Description = "no",
            ContentUrl = "https://example.com",
            Type = ResourceType.Article
        });

        Assert.IsType<ForbidResult>(result);
        Assert.Equal("Original", db.Resources.Single().Title);
    }

    [Fact]
    public async Task Delete_allows_admin()
    {
        await using var db = TestHarness.CreateDb();
        var resource = await SeedResource(db, authorId: "psych-1");
        var controller = new ResourcesController(db);
        TestHarness.SetUser(controller, "admin-1", "Admin");

        var result = await controller.DeleteResource(resource.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.Resources);
    }

    private static async Task<Resource> SeedResource(BetterMe.Infrastructure.Data.AppDbContext db, string authorId)
    {
        db.Users.Add(TestHarness.User(authorId, UserRole.Psychologist));
        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            Title = "Original",
            Description = "desc",
            ContentUrl = "https://example.com",
            Type = ResourceType.Article,
            AuthorId = authorId,
            IsPublished = true
        };
        db.Resources.Add(resource);
        await db.SaveChangesAsync();
        return resource;
    }
}
