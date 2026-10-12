using System.ComponentModel.DataAnnotations;

namespace DoughDoodle.Models;

/// <summary>Idempotency ledger for Stripe webhook events.</summary>
public class ProcessedStripeEvent
{
    [Key]
    [MaxLength(255)]
    public string EventId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string EventType { get; set; } = string.Empty;

    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
