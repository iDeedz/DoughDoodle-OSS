using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Services;

public interface ISubscriptionHunterService
{
    Task<SubscriptionHunterResult> AnalyzeAsync(string userId, CancellationToken cancellationToken = default);
}

public class SubscriptionHunterResult
{
    public decimal MonthlyBurn { get; set; }
    public decimal AnnualBurn { get; set; }
    public int SuspectCount { get; set; }
    public IReadOnlyList<ScheduledTransaction> Suspects { get; set; } = Array.Empty<ScheduledTransaction>();
}

/// <summary>
/// Shared subscription-suspect detection and monthly burn math for Dashboard and Subscription Hunter.
/// Estimates from the user's own scheduled recurrences (CSV/manual) - not live bank sync.
/// </summary>
public class SubscriptionHunterService : ISubscriptionHunterService
{
    internal static readonly string[] Keywords =
    {
        "netflix", "spotify", "disney", "apple", "google", "amazon", "prime",
        "subscription", "membership", "gym", "icloud", "adobe", "dropbox",
        "microsoft", "youtube", "hulu", "paramount"
    };

    private readonly BudgetDbContext _db;

    public SubscriptionHunterService(BudgetDbContext db) => _db = db;

    public async Task<SubscriptionHunterResult> AnalyzeAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return new SubscriptionHunterResult();
        }

        var scheduled = await _db.ScheduledTransactions
            .AsNoTracking()
            .Where(st => st.UserId == userId && st.IsActive)
            .Include(st => st.Category)
            .Include(st => st.Account)
            .ToListAsync(cancellationToken);

        // SQLite cannot ORDER BY decimal in EF; sort money on the client.
        scheduled = scheduled.OrderByDescending(st => st.Amount).ToList();

        // Leak/burn is outflow only. Monthly income (e.g. salary) must not inflate subscription burn.
        var suspects = scheduled
            .Where(st => st.TransactionType == TransactionType.Expense)
            .Where(IsSubscriptionSuspect)
            .ToList();
        var monthlyBurn = suspects.Sum(ToMonthlyAmount);

        return new SubscriptionHunterResult
        {
            MonthlyBurn = decimal.Round(monthlyBurn, 2),
            AnnualBurn = decimal.Round(monthlyBurn * 12m, 2),
            SuspectCount = suspects.Count,
            Suspects = suspects
        };
    }

    /// <summary>
    /// Same suspect rule as the original RecurringController hunter:
    /// monthly/annual/quarterly cadence, or keyword / "Subscription" category match.
    /// </summary>
    public static bool IsSubscriptionSuspect(ScheduledTransaction st)
    {
        if (st.RecurrenceType is RecurrenceType.Monthly or RecurrenceType.Annually or RecurrenceType.Quarterly)
        {
            return true;
        }

        var description = st.Description ?? "";
        var categoryName = st.Category?.Name ?? "";
        return Keywords.Any(k =>
            description.Contains(k, StringComparison.OrdinalIgnoreCase)
            || categoryName.Contains(k, StringComparison.OrdinalIgnoreCase)
            || categoryName.Contains("Subscription", StringComparison.OrdinalIgnoreCase));
    }

    public static decimal ToMonthlyAmount(ScheduledTransaction st)
    {
        return st.RecurrenceType switch
        {
            RecurrenceType.Annually => st.Amount / 12m,
            RecurrenceType.Quarterly => st.Amount / 3m,
            RecurrenceType.SemiAnnually => st.Amount / 6m,
            RecurrenceType.Weekly => st.Amount * 52m / 12m,
            RecurrenceType.Biweekly => st.Amount * 26m / 12m,
            _ => st.Amount
        };
    }
}
