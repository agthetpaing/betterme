using Suggestme.Shared.Enums;

namespace Suggestme.Infrastructure.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripePriceId { get; set; }
    public string Status { get; set; } = "free";
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}
