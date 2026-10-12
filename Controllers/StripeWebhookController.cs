using DoughDoodle.Services.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoughDoodle.Controllers;

/// <summary>
/// Stripe signed webhooks. Raw body + signature verify; antiforgery skipped (Stripe cannot send our token).
/// Dough entitlements update only from verified webhook state.
/// </summary>
[ApiController]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly IStripeBillingService _billing;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(IStripeBillingService billing, ILogger<StripeWebhookController> logger)
    {
        _billing = billing;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        string json;
        using (var reader = new StreamReader(HttpContext.Request.Body))
            json = await reader.ReadToEndAsync(cancellationToken);

        var signature = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(signature))
            return BadRequest(new { error = "Missing Stripe-Signature header." });

        try
        {
            await _billing.HandleWebhookAsync(json, signature, cancellationToken);
            return Ok();
        }
        catch (Stripe.StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook rejected");
            return BadRequest(new { error = "Webhook signature verification failed." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook not configured or invalid");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook processing failed");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
