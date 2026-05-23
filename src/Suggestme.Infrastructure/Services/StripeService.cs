using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using Suggestme.Infrastructure.Data;
using Suggestme.Shared.Enums;

namespace Suggestme.Infrastructure.Services;

public class StripeService : IStripeService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly ILogger<StripeService> _logger;

    public StripeService(IConfiguration config, AppDbContext db, ILogger<StripeService> logger)
    {
        _config = config;
        _db = db;
        _logger = logger;
        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
    }

    public async Task<string> CreateCheckoutSessionAsync(string userId, string userEmail, string successUrl, string cancelUrl)
    {
        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);

        // Create or retrieve Stripe customer
        string stripeCustomerId;
        if (!string.IsNullOrEmpty(subscription?.StripeCustomerId))
        {
            stripeCustomerId = subscription.StripeCustomerId;
        }
        else
        {
            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = userEmail,
                Metadata = new Dictionary<string, string> { { "userId", userId } }
            });
            stripeCustomerId = customer.Id;

            if (subscription != null)
            {
                subscription.StripeCustomerId = stripeCustomerId;
                await _db.SaveChangesAsync();
            }
        }

        var sessionService = new SessionService();
        var sessionOptions = new SessionCreateOptions
        {
            Customer = stripeCustomerId,
            Mode = "subscription",
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = _config["Stripe:PremiumPriceId"],
                    Quantity = 1
                }
            },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
        };

        var session = await sessionService.CreateAsync(sessionOptions);
        return session.Url;
    }

    public async Task<string> CreatePortalSessionAsync(string userId, string returnUrl)
    {
        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);

        if (string.IsNullOrEmpty(subscription?.StripeCustomerId))
            throw new InvalidOperationException("No Stripe customer found for this user.");

        var portalService = new Stripe.BillingPortal.SessionService();
        var session = await portalService.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = subscription.StripeCustomerId,
            ReturnUrl = returnUrl
        });

        return session.Url;
    }

    public async Task HandleWebhookAsync(string payload, string stripeSignature)
    {
        var webhookSecret = _config["Stripe:WebhookSecret"]!;
        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, stripeSignature, webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning("Stripe webhook validation failed: {Message}", ex.Message);
            throw;
        }

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutCompleted(stripeEvent);
                break;
            case "customer.subscription.updated":
                await HandleSubscriptionUpdated(stripeEvent);
                break;
            case "customer.subscription.deleted":
                await HandleSubscriptionDeleted(stripeEvent);
                break;
        }
    }

    private async Task HandleCheckoutCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        var subscriptionService = new SubscriptionService();
        var stripeSubscription = await subscriptionService.GetAsync(session.SubscriptionId);

        // Find user by Stripe customer ID
        var localSub = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeCustomerId == session.CustomerId);

        if (localSub == null) return;

        localSub.Tier = SubscriptionTier.Premium;
        localSub.StripeSubscriptionId = stripeSubscription.Id;
        localSub.StripePriceId = stripeSubscription.Items.Data[0].Price.Id;
        localSub.Status = stripeSubscription.Status;
        localSub.CurrentPeriodStart = stripeSubscription.CurrentPeriodStart;
        localSub.CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Subscription activated for user {UserId}", localSub.UserId);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        var localSub = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

        if (localSub == null) return;

        localSub.Status = stripeSubscription.Status;
        localSub.CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd;

        await _db.SaveChangesAsync();
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        var localSub = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

        if (localSub == null) return;

        localSub.Tier = SubscriptionTier.Free;
        localSub.Status = "canceled";
        localSub.CanceledAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Subscription canceled for user {UserId}", localSub.UserId);
    }
}
