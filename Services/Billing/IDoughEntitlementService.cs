using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DoughDoodle.Services.Billing;

public interface IDoughEntitlementService
{
    DoughTier GetTier(ApplicationUser? user);
    Task<DoughTier> GetTierAsync(string userId, CancellationToken cancellationToken = default);
    DoughTier ResolveTierFromPriceId(string? priceId);
    bool HasMinimumTier(ApplicationUser? user, DoughTier minimum);
    bool HasMinimumTier(DoughTier tier, DoughTier minimum);

    int GetMaxEnvelopes(DoughTier tier);
    int GetMaxAccounts(DoughTier tier);
    int GetMaxGoals(DoughTier tier);
    int GetHistoryMonths(DoughTier tier);
    int GetSubAccountLimit(DoughTier tier);
    bool CanUseAiInsights(DoughTier tier);
    bool CanUseAdvancedReports(DoughTier tier);
    bool CanUseInvestments(DoughTier tier);
    bool CanUseCsvImport(DoughTier tier);
    bool CanUseUnlimitedCsvHistory(DoughTier tier);
    bool CanUseHouseholdSharing(DoughTier tier);
    string GetPackDisplayName(DoughTier tier);
    string GetPackTagline(DoughTier tier);
    string GetPackPriceLabel(DoughTier tier);
    string GetTierImagePath(DoughTier tier);
}

public class DoughEntitlementService : IDoughEntitlementService
{
    private readonly BudgetDbContext _db;
    private readonly StripeOptions _options;

    public static readonly HashSet<string> PaidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "active",
        "trialing"
    };

    public DoughEntitlementService(BudgetDbContext db, IOptions<StripeOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public DoughTier GetTier(ApplicationUser? user)
    {
        if (user == null) return DoughTier.Pocket;
        if (user.IsAdmin) return DoughTier.Prime;

        if (!string.IsNullOrWhiteSpace(user.SubscriptionStatus))
        {
            if (!PaidStatuses.Contains(user.SubscriptionStatus))
                return DoughTier.Pocket;
            return ResolveTierFromPriceId(user.StripePriceId);
        }

        return user.DoughTier;
    }

    public async Task<DoughTier> GetTierAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId)) return DoughTier.Pocket;
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return GetTier(user);
    }

    public DoughTier ResolveTierFromPriceId(string? priceId)
    {
        if (string.IsNullOrWhiteSpace(priceId))
            return DoughTier.Pocket;

        var p = _options.Prices;
        if (Matches(priceId, p.PrimeMonthly, p.PrimeYearly)) return DoughTier.Prime;
        if (Matches(priceId, p.VaultMonthly, p.VaultYearly)) return DoughTier.Vault;
        if (Matches(priceId, p.StackMonthly, p.StackYearly)) return DoughTier.Stack;
        if (Matches(priceId, p.BankrollMonthly, p.BankrollYearly)) return DoughTier.Bankroll;
        return DoughTier.Pocket;
    }

    private static bool Matches(string priceId, string? monthly, string? yearly) =>
        (!string.IsNullOrWhiteSpace(monthly) && string.Equals(priceId, monthly, StringComparison.Ordinal))
        || (!string.IsNullOrWhiteSpace(yearly) && string.Equals(priceId, yearly, StringComparison.Ordinal));

    public bool HasMinimumTier(ApplicationUser? user, DoughTier minimum) =>
        HasMinimumTier(GetTier(user), minimum);

    public bool HasMinimumTier(DoughTier tier, DoughTier minimum) =>
        (int)tier >= (int)minimum;

    public int GetMaxEnvelopes(DoughTier tier) => tier switch
    {
        DoughTier.Pocket => 10,
        DoughTier.Bankroll => 25,
        DoughTier.Stack => 50,
        DoughTier.Vault => 100,
        DoughTier.Prime => int.MaxValue,
        _ => 10
    };

    public int GetMaxAccounts(DoughTier tier) => tier switch
    {
        DoughTier.Pocket => 2,
        DoughTier.Bankroll => 5,
        DoughTier.Stack => 10,
        DoughTier.Vault => 25,
        DoughTier.Prime => int.MaxValue,
        _ => 2
    };

    public int GetMaxGoals(DoughTier tier) => tier switch
    {
        DoughTier.Pocket => 2,
        DoughTier.Bankroll => 5,
        DoughTier.Stack => 15,
        DoughTier.Vault => 40,
        DoughTier.Prime => int.MaxValue,
        _ => 2
    };

    public int GetHistoryMonths(DoughTier tier) => tier switch
    {
        DoughTier.Pocket => 3,
        DoughTier.Bankroll => 12,
        DoughTier.Stack => 24,
        DoughTier.Vault => 60,
        DoughTier.Prime => 120,
        _ => 3
    };

    public int GetSubAccountLimit(DoughTier tier) => tier switch
    {
        DoughTier.Pocket => 1,
        DoughTier.Bankroll => 3,
        DoughTier.Stack => 5,
        DoughTier.Vault => 10,
        DoughTier.Prime => int.MaxValue,
        _ => 1
    };

    public bool CanUseAiInsights(DoughTier tier) => HasMinimumTier(tier, DoughTier.Stack);
    public bool CanUseAdvancedReports(DoughTier tier) => HasMinimumTier(tier, DoughTier.Bankroll);
    public bool CanUseInvestments(DoughTier tier) => HasMinimumTier(tier, DoughTier.Vault);
    public bool CanUseCsvImport(DoughTier tier) => true;
    public bool CanUseUnlimitedCsvHistory(DoughTier tier) => HasMinimumTier(tier, DoughTier.Stack);
    public bool CanUseHouseholdSharing(DoughTier tier) => HasMinimumTier(tier, DoughTier.Bankroll);

    public string GetPackDisplayName(DoughTier tier) => tier switch
    {
        DoughTier.Pocket => "Pocket",
        DoughTier.Bankroll => "Bankroll",
        DoughTier.Stack => "Stack",
        DoughTier.Vault => "Vault",
        DoughTier.Prime => "Prime",
        _ => "Pocket"
    };

    public string GetPackTagline(DoughTier tier) => tier switch
    {
        DoughTier.Pocket => "Free forever — envelopes, CSV import, and dough-left-today clarity.",
        DoughTier.Bankroll => "More envelopes, household sharing, 12-month history, and pace alerts.",
        DoughTier.Stack => "AI insights, deep CSV history, advanced reports, and more goals.",
        DoughTier.Vault => "Investments, net-worth depth, larger household, and long history.",
        DoughTier.Prime => "Unlimited envelopes, accounts, goals — full access across the product.",
        _ => ""
    };

    public string GetPackPriceLabel(DoughTier tier) => tier switch
    {
        DoughTier.Pocket => "Free forever",
        DoughTier.Bankroll => "$6/mo",
        DoughTier.Stack => "$10/mo",
        DoughTier.Vault => "$16/mo",
        DoughTier.Prime => "$24/mo",
        _ => "Free"
    };

    public string GetTierImagePath(DoughTier tier) => tier switch
    {
        DoughTier.Pocket => "/images/tiers/tier-pocket.svg",
        DoughTier.Bankroll => "/images/tiers/tier-bankroll.svg",
        DoughTier.Stack => "/images/tiers/tier-stack.svg",
        DoughTier.Vault => "/images/tiers/tier-vault.svg",
        DoughTier.Prime => "/images/tiers/tier-prime.svg",
        _ => "/images/tiers/tier-pocket.svg"
    };
}
