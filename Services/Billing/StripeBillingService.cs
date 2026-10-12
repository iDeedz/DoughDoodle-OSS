using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace DoughDoodle.Services.Billing;

public class StripeBillingService : IStripeBillingService
{
    private readonly BudgetDbContext _db;
    private readonly StripeOptions _options;
    private readonly IDoughEntitlementService _entitlements;
    private readonly ILogger<StripeBillingService> _logger;

    public StripeBillingService(
        BudgetDbContext db,
        IOptions<StripeOptions> options,
        IDoughEntitlementService entitlements,
        ILogger<StripeBillingService> logger)
    {
        _db = db;
        _options = options.Value;
        _entitlements = entitlements;
        _logger = logger;
        if (!string.IsNullOrWhiteSpace(_options.SecretKey))
            StripeConfiguration.ApiKey = _options.SecretKey;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<string> CreateCheckoutSessionAsync(ApplicationUser user, string priceId, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("Stripe is not configured. Add keys in appsettings or environment variables.");

        if (!_options.IsKnownPriceId(priceId))
            throw new ArgumentException("Unknown price id for DoughDoodle dough tiers.");

        var customerId = user.StripeCustomerId;
        if (string.IsNullOrWhiteSpace(customerId))
        {
            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = user.Email,
                Name = user.FullName,
                Metadata = new Dictionary<string, string> { ["doughdoodle_user_id"] = user.Id }
            }, cancellationToken: cancellationToken);

            customerId = customer.Id;
            user.StripeCustomerId = customerId;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var domain = _options.Domain.TrimEnd('/');
        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            ClientReferenceId = user.Id,
            SuccessUrl = $"{domain}/Billing?success=1",
            CancelUrl = $"{domain}/Billing?canceled=1",
            LineItems = new List<SessionLineItemOptions>
            {
                new() { Price = priceId, Quantity = 1 }
            },
            Metadata = new Dictionary<string, string>
            {
                ["doughdoodle_user_id"] = user.Id,
                ["doughdoodle_price_id"] = priceId
            },
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["doughdoodle_user_id"] = user.Id,
                    ["doughdoodle_price_id"] = priceId
                }
            }
        }, cancellationToken: cancellationToken);

        return session.Url ?? throw new InvalidOperationException("Stripe Checkout session missing URL.");
    }

    public async Task<string> CreateCustomerPortalSessionAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("Stripe is not configured.");
        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
            throw new InvalidOperationException("No Stripe customer on this account yet. Start a Checkout first.");

        var domain = _options.Domain.TrimEnd('/');
        var portal = new Stripe.BillingPortal.SessionService();
        var session = await portal.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = user.StripeCustomerId,
            ReturnUrl = $"{domain}/Billing"
        }, cancellationToken: cancellationToken);

        return session.Url;
    }

    public async Task HandleWebhookAsync(string json, string stripeSignatureHeader, CancellationToken cancellationToken = default)
    {
        if (!_options.CanVerifyWebhooks)
            throw new InvalidOperationException("Stripe webhook secret is not configured.");

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, stripeSignatureHeader, _options.WebhookSecret);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed");
            throw;
        }

        if (await _db.ProcessedStripeEvents.AnyAsync(e => e.EventId == stripeEvent.Id, cancellationToken))
        {
            _logger.LogInformation("Ignoring duplicate Stripe event {EventId}", stripeEvent.Id);
            return;
        }

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
                await HandleCheckoutCompletedAsync(stripeEvent, cancellationToken);
                break;
            case EventTypes.CustomerSubscriptionUpdated:
            case EventTypes.CustomerSubscriptionDeleted:
                await HandleSubscriptionChangedAsync(stripeEvent, cancellationToken);
                break;
            case EventTypes.InvoicePaid:
            case EventTypes.InvoicePaymentFailed:
                await HandleInvoiceAsync(stripeEvent, cancellationToken);
                break;
            default:
                _logger.LogDebug("Unhandled Stripe event type {Type}", stripeEvent.Type);
                break;
        }

        _db.ProcessedStripeEvents.Add(new ProcessedStripeEvent
        {
            EventId = stripeEvent.Id,
            EventType = stripeEvent.Type,
            ProcessedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleCheckoutCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Session session)
            return;

        var userId = session.ClientReferenceId
            ?? session.Metadata?.GetValueOrDefault("doughdoodle_user_id");
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Checkout session {SessionId} missing doughdoodle user id", session.Id);
            return;
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return;

        if (!string.IsNullOrWhiteSpace(session.CustomerId))
            user.StripeCustomerId = session.CustomerId;

        if (!string.IsNullOrWhiteSpace(session.SubscriptionId))
        {
            user.StripeSubscriptionId = session.SubscriptionId;
            var subService = new SubscriptionService();
            var sub = await subService.GetAsync(session.SubscriptionId, cancellationToken: cancellationToken);
            ApplySubscription(user, sub);
        }
        else if (session.Metadata != null && session.Metadata.TryGetValue("doughdoodle_price_id", out var priceFromMeta))
        {
            user.StripePriceId = priceFromMeta;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleSubscriptionChangedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Subscription sub)
            return;

        var user = await FindUserForSubscriptionAsync(sub, cancellationToken);
        if (user == null) return;

        ApplySubscription(user, sub);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleInvoiceAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Invoice invoice)
            return;

        var subscriptionId = invoice.SubscriptionId;
        if (string.IsNullOrWhiteSpace(subscriptionId))
            return;

        var subService = new SubscriptionService();
        var sub = await subService.GetAsync(subscriptionId, cancellationToken: cancellationToken);
        var user = await FindUserForSubscriptionAsync(sub, cancellationToken);
        if (user == null) return;

        ApplySubscription(user, sub);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser?> FindUserForSubscriptionAsync(Subscription sub, CancellationToken cancellationToken)
    {
        ApplicationUser? user = null;
        if (sub.Metadata != null && sub.Metadata.TryGetValue("doughdoodle_user_id", out var uid) && !string.IsNullOrEmpty(uid))
            user = await _db.Users.FirstOrDefaultAsync(u => u.Id == uid, cancellationToken);

        if (user == null && !string.IsNullOrWhiteSpace(sub.Id))
            user = await _db.Users.FirstOrDefaultAsync(u => u.StripeSubscriptionId == sub.Id, cancellationToken);

        if (user == null && !string.IsNullOrWhiteSpace(sub.CustomerId))
            user = await _db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == sub.CustomerId, cancellationToken);

        return user;
    }

    private void ApplySubscription(ApplicationUser user, Subscription sub)
    {
        user.StripeSubscriptionId = sub.Id;
        if (!string.IsNullOrWhiteSpace(sub.CustomerId))
            user.StripeCustomerId = sub.CustomerId;
        user.SubscriptionStatus = sub.Status;
        user.SubscriptionCurrentPeriodEnd = sub.CurrentPeriodEnd == default
            ? null
            : DateTime.SpecifyKind(sub.CurrentPeriodEnd, DateTimeKind.Utc);
        user.SubscriptionExpiresAt = user.SubscriptionCurrentPeriodEnd;

        var priceId = ExtractPriceId(sub);
        if (!string.IsNullOrWhiteSpace(priceId))
            user.StripePriceId = priceId;

        var tier = _entitlements.ResolveTierFromPriceId(user.StripePriceId);
        if (string.Equals(sub.Status, "canceled", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sub.Status, "unpaid", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sub.Status, "incomplete_expired", StringComparison.OrdinalIgnoreCase))
        {
            tier = DoughTier.Pocket;
        }
        else if (!DoughEntitlementService.PaidStatuses.Contains(sub.Status))
        {
            // past_due etc. — keep last paid tier until status clears; GetTier demotes on non-paid
        }

        if (DoughEntitlementService.PaidStatuses.Contains(sub.Status))
        {
            user.DoughTier = tier;
            user.SubscriptionPlan = (SubscriptionPlan)(int)tier;
            user.LastPaymentDate = DateTime.UtcNow;
            user.NextPaymentDue = user.SubscriptionCurrentPeriodEnd;
        }
        else if (string.Equals(sub.Status, "canceled", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(sub.Status, "unpaid", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(sub.Status, "incomplete_expired", StringComparison.OrdinalIgnoreCase))
        {
            user.DoughTier = DoughTier.Pocket;
            user.SubscriptionPlan = SubscriptionPlan.Pocket;
        }
    }

    private static string? ExtractPriceId(Subscription sub)
    {
        var item = sub.Items?.Data?.FirstOrDefault();
        return item?.Price?.Id;
    }
}
