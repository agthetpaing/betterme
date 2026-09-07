using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BetterMe.Infrastructure.Entities;

namespace BetterMe.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<ResourceTag> ResourceTags => Set<ResourceTag>();
    public DbSet<ResourceTagMapping> ResourceTagMappings => Set<ResourceTagMapping>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<MoodEntry> MoodEntries => Set<MoodEntry>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OnboardingResponse> OnboardingResponses => Set<OnboardingResponse>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables
        builder.Entity<ApplicationUser>().ToTable("users");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("user_roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("user_claims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("user_logins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("role_claims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("user_tokens");

        // ApplicationUser
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            e.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Bio).HasMaxLength(2000);
        });

        // RefreshToken
        builder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasOne(r => r.User)
             .WithMany()
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Subscription — one-to-one with user
        builder.Entity<Subscription>(e =>
        {
            e.ToTable("subscriptions");
            e.HasIndex(s => s.UserId).IsUnique();
            e.HasOne(s => s.User)
             .WithOne(u => u.Subscription)
             .HasForeignKey<Subscription>(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Resource
        builder.Entity<Resource>(e =>
        {
            e.ToTable("resources");
            e.Property(r => r.Title).HasMaxLength(300).IsRequired();
            e.Property(r => r.Description).HasMaxLength(2000);
            e.HasOne(r => r.Author)
             .WithMany(u => u.CreatedResources)
             .HasForeignKey(r => r.AuthorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ResourceTag
        builder.Entity<ResourceTag>(e =>
        {
            e.ToTable("resource_tags");
            e.HasIndex(t => t.Slug).IsUnique();
            e.Property(t => t.Name).HasMaxLength(100).IsRequired();
            e.Property(t => t.Slug).HasMaxLength(100).IsRequired();
        });

        // ResourceTagMapping — composite PK
        builder.Entity<ResourceTagMapping>(e =>
        {
            e.ToTable("resource_tag_mappings");
            e.HasKey(m => new { m.ResourceId, m.TagId });
            e.HasOne(m => m.Resource)
             .WithMany(r => r.TagMappings)
             .HasForeignKey(m => m.ResourceId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Tag)
             .WithMany(t => t.ResourceMappings)
             .HasForeignKey(m => m.TagId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Session — two FK to ApplicationUser
        builder.Entity<Session>(e =>
        {
            e.ToTable("sessions");
            e.HasOne(s => s.Patient)
             .WithMany(u => u.PatientSessions)
             .HasForeignKey(s => s.PatientId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Psychologist)
             .WithMany(u => u.PsychologistSessions)
             .HasForeignKey(s => s.PsychologistId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // MoodEntry
        builder.Entity<MoodEntry>(e =>
        {
            e.ToTable("mood_entries");
            e.HasOne(m => m.User)
             .WithMany(u => u.MoodEntries)
             .HasForeignKey(m => m.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // JournalEntry
        builder.Entity<JournalEntry>(e =>
        {
            e.ToTable("journal_entries");
            e.Property(j => j.Body).HasMaxLength(10000).IsRequired();
            e.HasOne(j => j.User)
             .WithMany(u => u.JournalEntries)
             .HasForeignKey(j => j.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CheckIn — two FK to ApplicationUser
        builder.Entity<CheckIn>(e =>
        {
            e.ToTable("check_ins");
            e.Property(c => c.Message).HasMaxLength(2000).IsRequired();
            e.HasOne(c => c.Psychologist)
             .WithMany(u => u.SentCheckIns)
             .HasForeignKey(c => c.PsychologistId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Patient)
             .WithMany(u => u.ReceivedCheckIns)
             .HasForeignKey(c => c.PatientId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification
        builder.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.Property(n => n.Title).HasMaxLength(200).IsRequired();
            e.Property(n => n.Message).HasMaxLength(500).IsRequired();
            e.HasOne(n => n.User)
             .WithMany(u => u.Notifications)
             .HasForeignKey(n => n.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // OnboardingResponse — one-to-one with user
        builder.Entity<OnboardingResponse>(e =>
        {
            e.ToTable("onboarding_responses");
            e.HasIndex(o => o.UserId).IsUnique();
            e.HasOne(o => o.User)
             .WithOne(u => u.OnboardingResponse)
             .HasForeignKey<OnboardingResponse>(o => o.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
