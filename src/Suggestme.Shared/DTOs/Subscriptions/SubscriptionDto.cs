using Suggestme.Shared.Enums;

namespace Suggestme.Shared.DTOs.Subscriptions;

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public SubscriptionTier Tier { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool IsActive => Tier == SubscriptionTier.Premium && Status == "active";
}
