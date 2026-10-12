using DoughDoodle.Models;

namespace DoughDoodle.Services.Billing;

public interface IStripeBillingService
{
    bool IsConfigured { get; }
    Task<string> CreateCheckoutSessionAsync(ApplicationUser user, string priceId, CancellationToken cancellationToken = default);
    Task<string> CreateCustomerPortalSessionAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task HandleWebhookAsync(string json, string stripeSignatureHeader, CancellationToken cancellationToken = default);
}
