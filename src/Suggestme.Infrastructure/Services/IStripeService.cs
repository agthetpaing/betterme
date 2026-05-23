namespace Suggestme.Infrastructure.Services;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(string userId, string userEmail, string successUrl, string cancelUrl);
    Task<string> CreatePortalSessionAsync(string userId, string returnUrl);
    Task HandleWebhookAsync(string payload, string stripeSignature);
}
