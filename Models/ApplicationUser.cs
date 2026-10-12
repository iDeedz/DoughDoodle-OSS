using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DoughDoodle.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}";

    [Required]
    public UserType UserType { get; set; } = UserType.MainAccount;

    public string? ParentUserId { get; set; }
    public ApplicationUser? ParentUser { get; set; }
    public virtual ICollection<ApplicationUser> SubAccounts { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Trial (legacy soft gate — Pocket is free forever; trial no longer blocks access)
    public DateTime TrialStartDate { get; set; } = DateTime.UtcNow;
    public DateTime TrialEndDate { get; set; } = DateTime.UtcNow.AddDays(30);
    public bool IsTrialExpired => DateTime.UtcNow > TrialEndDate && DoughTier == DoughTier.Pocket;
    public bool HasPaidSubscription => DoughTier != DoughTier.Pocket;

    /// <summary>Legacy plan column kept in sync with DoughTier for admin reports.</summary>
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Pocket;

    /// <summary>Entitlement tier — webhook-owned when Stripe is configured.</summary>
    public DoughTier DoughTier { get; set; } = DoughTier.Pocket;

    public DateTime? SubscriptionExpiresAt { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public DateTime? NextPaymentDue { get; set; }

    // Stripe (webhook-owned; never trust browser for paid unlocks)
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripePriceId { get; set; }
    public string? SubscriptionStatus { get; set; }
    public DateTime? SubscriptionCurrentPeriodEnd { get; set; }

    public DateTime? LastTrialWarningNotification { get; set; }
    public DateTime? LastPaymentReminderNotification { get; set; }

    public bool IsAdmin { get; set; } = false;

    /// <summary>Role within parent household (sub-accounts). Main accounts act as HouseholdAdmin of their household.</summary>
    public HouseholdRole HouseholdRole { get; set; } = HouseholdRole.Member;

    /// <summary>Encrypted authenticator key when TOTP is enabled (Identity also tracks TwoFactorEnabled).</summary>
    [MaxLength(256)]
    public string? AuthenticatorKey { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? DeactivationReason { get; set; }

    /// <summary>Onboarding wizard completed.</summary>
    public bool OnboardingCompleted { get; set; } = false;
    public DateTime? OnboardingCompletedAt { get; set; }

    /// <summary>Display currency code (ISO). No live FX — display/label only.</summary>
    [MaxLength(3)]
    public string PreferredCurrency { get; set; } = "USD";

    public int SubAccountLimit => DoughTier switch
    {
        DoughTier.Pocket => 1,
        DoughTier.Bankroll => 3,
        DoughTier.Stack => 5,
        DoughTier.Vault => 10,
        DoughTier.Prime => int.MaxValue,
        _ => 1
    };

    public int CurrentSubAccountCount => SubAccounts.Count(s => s.IsActive);
    public bool CanCreateSubAccount => CurrentSubAccountCount < SubAccountLimit;

    // Free forever floor — Pocket never locks out; paid unlocks depth
    public bool CanAccessSystem => IsActive;
    public int DaysRemainingInTrial => Math.Max(0, (TrialEndDate - DateTime.UtcNow).Days);
    public string AccountStatus => !IsActive ? "Deactivated" :
        IsAdmin ? "Administrator" :
        HasPaidSubscription ? $"Paid ({DoughTier})" :
        $"Pocket (free forever)";
}

public enum UserType
{
    MainAccount,
    SubAccount
}

/// <summary>Legacy enum mirrored to DoughTier values for existing columns/admin.</summary>
public enum SubscriptionPlan
{
    Pocket = 0,
    Bankroll = 1,
    Stack = 2,
    Vault = 3,
    Prime = 4
}

/// <summary>
/// DoughDoodle membership batches. Pocket is free forever; paid dough maps from Stripe Price ids via webhook.
/// </summary>
public enum DoughTier
{
    Pocket = 0,
    Bankroll = 1,
    Stack = 2,
    Vault = 3,
    Prime = 4
}
