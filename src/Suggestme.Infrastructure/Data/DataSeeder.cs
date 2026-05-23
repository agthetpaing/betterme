using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Suggestme.Infrastructure.Entities;
using Suggestme.Shared.Enums;

namespace Suggestme.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILogger<AppDbContext>>();

        string[] roles = { "Patient", "Psychologist", "Admin" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    public static async Task SeedResourcesAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Skip if already seeded
        if (await db.Resources.AnyAsync()) return;

        // Ensure a system content author exists
        const string systemEmail = "content@betterme.local";
        var author = await userManager.FindByEmailAsync(systemEmail);
        if (author == null)
        {
            author = new ApplicationUser
            {
                UserName = systemEmail,
                Email = systemEmail,
                FirstName = "BetterMe",
                LastName = "Team",
                Role = UserRole.Admin,
                IsOnboarded = true,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(author, "BetterMeTeam1!");
            await userManager.AddToRoleAsync(author, "Admin");
        }

        // Seed tags
        var tagDefs = new[]
        {
            ("Anxiety",       "anxiety"),
            ("Depression",    "depression"),
            ("Stress",        "stress"),
            ("Sleep",         "sleep"),
            ("Mindfulness",   "mindfulness"),
            ("Self-Care",     "self-care"),
            ("Relationships", "relationships"),
            ("Coping Skills", "coping-skills"),
            ("Therapy",       "therapy"),
            ("Breathing",     "breathing"),
        };

        var tags = new Dictionary<string, ResourceTag>();
        foreach (var (name, slug) in tagDefs)
        {
            var existing = await db.ResourceTags.FirstOrDefaultAsync(t => t.Slug == slug);
            if (existing == null)
            {
                existing = new ResourceTag { Name = name, Slug = slug };
                db.ResourceTags.Add(existing);
            }
            tags[slug] = existing;
        }
        await db.SaveChangesAsync();

        // Helper
        async Task AddResource(
            string title, string description, string contentUrl,
            ResourceType type, bool isPublished, string[] tagSlugs)
        {
            var resource = new Resource
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                ContentUrl = contentUrl,
                Type = type,
                IsPublished = isPublished,
                AuthorId = author.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.Resources.Add(resource);
            await db.SaveChangesAsync();

            foreach (var slug in tagSlugs)
            {
                if (tags.TryGetValue(slug, out var tag))
                    db.ResourceTagMappings.Add(new ResourceTagMapping { ResourceId = resource.Id, TagId = tag.Id });
            }
            await db.SaveChangesAsync();
        }

        // ── Articles ──────────────────────────────────────────────────────────
        await AddResource(
            "Understanding Anxiety",
            "A comprehensive guide to understanding anxiety disorders, their symptoms, causes, and how they affect daily life. Learn when anxiety becomes a problem and what you can do about it.",
            "https://www.mind.org.uk/information-support/types-of-mental-health-problems/anxiety-and-panic-attacks/about-anxiety/",
            ResourceType.Article, true,
            new[] { "anxiety", "coping-skills" });

        await AddResource(
            "Managing Depression: A Practical Guide",
            "Depression is more than feeling sad. This NHS guide covers recognising signs of depression, self-help strategies, and when to seek professional support.",
            "https://www.nhs.uk/mental-health/conditions/depression-in-adults/overview/",
            ResourceType.Article, true,
            new[] { "depression", "self-care", "therapy" });

        await AddResource(
            "10 Stress Management Techniques That Actually Work",
            "Practical, evidence-based strategies to manage stress in everyday life — from breathing exercises and time management to physical activity and social connection.",
            "https://www.nhs.uk/mental-health/self-help/guides-tools-and-activities/tips-to-reduce-stress/",
            ResourceType.Article, true,
            new[] { "stress", "coping-skills", "self-care" });

        await AddResource(
            "Sleep and Mental Health",
            "Poor sleep and mental health problems are closely linked. This guide from Mind explains the relationship between sleep and conditions like anxiety and depression, with tips to improve your sleep.",
            "https://www.mind.org.uk/information-support/types-of-mental-health-problems/sleep-problems/about-sleep-problems/",
            ResourceType.Article, true,
            new[] { "sleep", "anxiety", "depression" });

        await AddResource(
            "An Introduction to Mindfulness",
            "Mindfulness means paying full attention to the present moment. The NHS explains what mindfulness is, the evidence behind it, and how to start a simple practice today.",
            "https://www.nhs.uk/mental-health/self-help/guides-tools-and-activities/mindfulness/",
            ResourceType.Article, true,
            new[] { "mindfulness", "stress", "anxiety" });

        await AddResource(
            "Building Resilience",
            "Resilience is the ability to adapt in the face of adversity. The American Psychological Association outlines research-backed ways to build mental resilience and bounce back from difficult experiences.",
            "https://www.apa.org/topics/resilience",
            ResourceType.Article, true,
            new[] { "coping-skills", "self-care", "stress" });

        await AddResource(
            "Cognitive Behavioural Therapy (CBT) Explained",
            "CBT is one of the most effective talking therapies. This NHS overview explains how CBT works, what it involves, and which conditions it can help — including anxiety, depression, and OCD.",
            "https://www.nhs.uk/mental-health/talking-therapies-medicine-treatments/talking-therapies-and-counselling/cognitive-behavioural-therapy-cbt/overview/",
            ResourceType.Article, true,
            new[] { "therapy", "anxiety", "depression" });

        await AddResource(
            "Self-Care: Why It Matters and How to Start",
            "Self-care isn't selfish — it's essential. Mind's guide covers what self-care really means, how to build a personalised self-care plan, and ways to look after yourself during tough times.",
            "https://www.mind.org.uk/information-support/tips-for-everyday-living/self-care/about-self-care/",
            ResourceType.Article, true,
            new[] { "self-care", "stress", "depression" });

        await AddResource(
            "Healthy Relationships and Mental Health",
            "Our relationships have a huge impact on our mental wellbeing. This guide explores how to build supportive connections, recognise unhealthy patterns, and set healthy boundaries.",
            "https://www.mind.org.uk/information-support/tips-for-everyday-living/relationships/",
            ResourceType.Article, true,
            new[] { "relationships", "self-care" });

        // ── Videos ────────────────────────────────────────────────────────────
        await AddResource(
            "Guided Box Breathing Exercise for Anxiety",
            "A calming 8-minute guided breathing session using the box breathing technique — inhale, hold, exhale, hold. Used by Navy SEALs and therapists alike to quickly reduce anxiety and stress.",
            "https://www.youtube.com/watch?v=odADwWzHR24",
            ResourceType.Video, true,
            new[] { "breathing", "anxiety", "stress" });

        await AddResource(
            "5-Minute Meditation You Can Do Anywhere",
            "A short, accessible guided meditation for beginners and busy people. No experience needed — just five minutes to reset your mind and reduce stress wherever you are.",
            "https://www.youtube.com/watch?v=inpok4MKVLM",
            ResourceType.Video, true,
            new[] { "mindfulness", "stress", "anxiety" });

        await AddResource(
            "Progressive Muscle Relaxation — Full Body",
            "A guided progressive muscle relaxation session (20 minutes) that systematically tenses and releases muscle groups throughout the body to relieve physical tension and anxiety.",
            "https://www.youtube.com/watch?v=1nZEdqcGVzo",
            ResourceType.Video, true,
            new[] { "anxiety", "stress", "coping-skills" });

        await AddResource(
            "Understanding Depression — Animated Explainer",
            "A clear, compassionate animated video from TED-Ed explaining what happens in the brain during depression, why it's more than sadness, and how it can be treated.",
            "https://www.youtube.com/watch?v=z-IR48Mb3W0",
            ResourceType.Video, true,
            new[] { "depression", "therapy" });

        await AddResource(
            "What Is Cognitive Behavioural Therapy? (CBT Explained)",
            "An easy-to-follow video explanation of how CBT works — the connection between thoughts, feelings, and behaviours — and how changing thought patterns can improve mental health.",
            "https://www.youtube.com/watch?v=0ViaCs0k2jM",
            ResourceType.Video, true,
            new[] { "therapy", "anxiety", "depression" });

        await AddResource(
            "Sleep Hygiene — How to Get a Better Night's Sleep",
            "Evidence-based tips to improve sleep quality, based on sleep science. Covers bedtime routines, screen time, caffeine, environment, and relaxation techniques.",
            "https://www.youtube.com/watch?v=nm1TxQj9IsQ",
            ResourceType.Video, true,
            new[] { "sleep", "self-care" });

        // ── Exercises ─────────────────────────────────────────────────────────
        await AddResource(
            "The 5-4-3-2-1 Grounding Technique",
            "A powerful grounding exercise for anxiety and panic: name 5 things you can see, 4 you can touch, 3 you can hear, 2 you can smell, and 1 you can taste. Anchors you firmly in the present moment.",
            "https://www.therapistaid.com/therapy-worksheet/5-4-3-2-1-grounding-technique",
            ResourceType.Exercise, true,
            new[] { "anxiety", "coping-skills", "mindfulness" });

        await AddResource(
            "Journaling Prompts for Mental Health",
            "A collection of 30 therapeutic journaling prompts designed to help you process emotions, explore thought patterns, build gratitude, and gain self-awareness. Great for daily reflection.",
            "https://www.therapistaid.com/therapy-guide/benefits-of-journaling",
            ResourceType.Exercise, true,
            new[] { "self-care", "depression", "coping-skills" });

        await AddResource(
            "Thought Record — Challenge Negative Thinking",
            "A structured CBT thought record worksheet to help you identify negative automatic thoughts, examine the evidence for and against them, and develop a more balanced perspective.",
            "https://www.therapistaid.com/therapy-worksheet/thought-record",
            ResourceType.Exercise, true,
            new[] { "therapy", "anxiety", "depression", "coping-skills" });

        await AddResource(
            "Worry Time — A Technique to Contain Anxiety",
            "Instead of worrying all day, set aside a dedicated 20-minute 'worry time'. This CBT technique helps contain anxious thoughts and prevents them from dominating your day.",
            "https://www.getselfhelp.co.uk/worry-time/",
            ResourceType.Exercise, true,
            new[] { "anxiety", "coping-skills", "stress" });

        await AddResource(
            "Self-Compassion Break Exercise",
            "A short, powerful exercise from Dr Kristin Neff's self-compassion programme. When you're struggling, offer yourself the same kindness you'd give a good friend — in three simple steps.",
            "https://self-compassion.org/exercise-2-self-compassion-break/",
            ResourceType.Exercise, true,
            new[] { "self-care", "depression", "mindfulness" });
    }
}
