using System.ComponentModel.DataAnnotations;
using Suggestme.Shared.Enums;

namespace Suggestme.Shared.DTOs.Resources;

public class CreateResourceRequest
{
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string ContentUrl { get; set; } = string.Empty;

    public string? ThumbnailUrl { get; set; }

    [Required]
    public ResourceType Type { get; set; }

    public SubscriptionTier RequiredTier { get; set; } = SubscriptionTier.Free;

    public bool IsPublished { get; set; } = false;

    public List<string> TagSlugs { get; set; } = new();
}
