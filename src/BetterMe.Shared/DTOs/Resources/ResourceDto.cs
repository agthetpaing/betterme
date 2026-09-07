using BetterMe.Shared.Enums;

namespace BetterMe.Shared.DTOs.Resources;

public class ResourceDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContentUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public ResourceType Type { get; set; }
    public SubscriptionTier RequiredTier { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}
