using DoughDoodle.Data;
using DoughDoodle.Helpers;
using DoughDoodle.Models;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Services;

public interface ISafeToSpendService
{
    Task<SafeToSpendResult> CalculateAsync(string userId, string? monthYear = null, string? currencyCode = null, CancellationToken cancellationToken = default, DateTime? asOf = null);
}

public class SafeToSpendResult
{
    public decimal DoughLeftToday { get; set; }
    public decimal TotalBudgeted { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal RemainingBudget { get; set; }
    public decimal DailyBurnRate { get; set; }
    public decimal ExpectedPaceSpent { get; set; }
    public decimal PaceVariance { get; set; }
    public string PaceStatus { get; set; } = "OnPace"; // OnPace, Ahead, Behind
    public int DayOfMonth { get; set; }
    public int DaysInMonth { get; set; }
    public int DaysRemaining { get; set; }
    public List<PaceAlert> Alerts { get; set; } = new();
}

public class PaceAlert
{
    public string Severity { get; set; } = "info"; // info, warning, danger
    public string Message { get; set; } = "";
    public string? CategoryName { get; set; }
}

/// <summary>
/// PocketGuard-class "dough left today" — remaining envelope budget divided by days left,
/// adjusted for mid-month burn pace. Uses decimal money math.
/// </summary>
public class SafeToSpendService : ISafeToSpendService
{
    private readonly BudgetDbContext _db;

    public SafeToSpendService(BudgetDbContext db) => _db = db;

    public async Task<SafeToSpendResult> CalculateAsync(string userId, string? monthYear = null, string? currencyCode = null, CancellationToken cancellationToken = default, DateTime? asOf = null)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            currencyCode = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.PreferredCurrency)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var currency = CurrencyDisplay.Normalize(currencyCode);
        monthYear ??= DateTime.Today.ToString("yyyy-MM");
        var monthDate = DateTime.ParseExact(monthYear, "yyyy-MM", null);
        var today = (asOf ?? DateTime.Today).Date;
        var dayOfMonth = (monthDate.Year == today.Year && monthDate.Month == today.Month)
            ? today.Day
            : DateTime.DaysInMonth(monthDate.Year, monthDate.Month);
        var daysInMonth = DateTime.DaysInMonth(monthDate.Year, monthDate.Month);
        var daysRemaining = Math.Max(1, daysInMonth - dayOfMonth + 1);

        var categories = await _db.Categories
            .Where(c => c.UserId == userId)
            .Include(c => c.Budgets)
            .Include(c => c.Transactions)
            .ToListAsync(cancellationToken);

        var budgets = categories.SelectMany(c => c.Budgets).Where(b => b.MonthYear == monthYear).ToList();
        var expenses = categories
            .SelectMany(c => c.Transactions)
            .Where(t => t.Date.Year == monthDate.Year && t.Date.Month == monthDate.Month
                        && t.TransactionType == TransactionType.Expense)
            .ToList();

        var totalBudgeted = budgets.Sum(b => b.Amount);
        var totalSpent = expenses.Sum(t => t.Amount);
        var remaining = totalBudgeted - totalSpent;
        var doughLeftToday = remaining <= 0 ? 0m : decimal.Round(remaining / daysRemaining, 2);

        var expectedPace = daysInMonth == 0 ? 0m : decimal.Round(totalBudgeted * dayOfMonth / daysInMonth, 2);
        var paceVariance = expectedPace - totalSpent; // positive = under pace (good)
        var dailyBurn = dayOfMonth <= 0 ? 0m : decimal.Round(totalSpent / dayOfMonth, 2);

        var paceStatus = paceVariance >= 0 ? "OnPace" : (Math.Abs(paceVariance) > totalBudgeted * 0.1m ? "Behind" : "Ahead");
        // Clarify: if spent more than expected pace → Behind (burning too fast)
        if (totalSpent > expectedPace * 1.05m) paceStatus = "Behind";
        else if (totalSpent < expectedPace * 0.85m) paceStatus = "Ahead"; // underspending
        else paceStatus = "OnPace";

        var alerts = new List<PaceAlert>();
        if (paceStatus == "Behind" && dayOfMonth >= 7)
        {
            alerts.Add(new PaceAlert
            {
                Severity = "warning",
                Message = $"You're burning dough faster than the month allows — about {CurrencyDisplay.Format(Math.Abs(paceVariance), currency)} ahead of a flat pace. Trim envelopes or transfer."
            });
        }
        if (doughLeftToday == 0 && remaining <= 0)
        {
            alerts.Add(new PaceAlert
            {
                Severity = "danger",
                Message = "No dough left in envelopes for the rest of this month. Move money between envelopes or pause discretionary spend."
            });
        }

        foreach (var cat in categories)
        {
            var budget = cat.Budgets.FirstOrDefault(b => b.MonthYear == monthYear)?.Amount ?? 0m;
            if (budget <= 0) continue;
            var spent = cat.Transactions
                .Where(t => t.Date.Year == monthDate.Year && t.Date.Month == monthDate.Month
                            && t.TransactionType == TransactionType.Expense)
                .Sum(t => t.Amount);
            var catExpected = decimal.Round(budget * dayOfMonth / daysInMonth, 2);
            if (spent > budget)
            {
                alerts.Add(new PaceAlert
                {
                    Severity = "danger",
                    CategoryName = cat.Name,
                    Message = $"{cat.Name} is over budget by {CurrencyDisplay.Format(spent - budget, currency)}."
                });
            }
            else if (dayOfMonth >= 10 && spent > catExpected * 1.25m)
            {
                alerts.Add(new PaceAlert
                {
                    Severity = "warning",
                    CategoryName = cat.Name,
                    Message = $"{cat.Name} is pacing hot — {CurrencyDisplay.Format(spent, currency)} spent vs ~{CurrencyDisplay.Format(catExpected, currency)} expected by today."
                });
            }
        }

        return new SafeToSpendResult
        {
            DoughLeftToday = doughLeftToday,
            TotalBudgeted = totalBudgeted,
            TotalSpent = totalSpent,
            RemainingBudget = remaining,
            DailyBurnRate = dailyBurn,
            ExpectedPaceSpent = expectedPace,
            PaceVariance = paceVariance,
            PaceStatus = paceStatus,
            DayOfMonth = dayOfMonth,
            DaysInMonth = daysInMonth,
            DaysRemaining = daysRemaining,
            Alerts = alerts.Take(8).ToList()
        };
    }
}
