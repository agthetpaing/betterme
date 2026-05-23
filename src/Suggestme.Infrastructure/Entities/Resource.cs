using Suggestme.Shared.Enums;

namespace Suggestme.Infrastructure.Entities;

public class Resource
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContentUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public ResourceType Type { get; set; }
    public SubscriptionTier RequiredTier { get; set; } = SubscriptionTier.Free;
    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string AuthorId { get; set; } = string.Empty;

    public ApplicationUser Author { get; set; } = null!;
    public ICollection<ResourceTagMapping> TagMappings { get; set; } = new List<ResourceTagMapping>();
}
