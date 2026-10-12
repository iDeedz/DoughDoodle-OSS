using DoughDoodle.Data;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Services.Analytics;

public record CashflowPoint(DateTime Month, decimal Income, decimal Expense, decimal Net, decimal ProjectedBalance);

public interface ICashflowForecastService
{
    Task<IReadOnlyList<CashflowPoint>> ForecastAsync(string userId, int historyMonths = 6, int forecastMonths = 3, CancellationToken ct = default);
}

public class CashflowForecastService : ICashflowForecastService
{
    private readonly BudgetDbContext _db;

    public CashflowForecastService(BudgetDbContext db) => _db = db;

    public async Task<IReadOnlyList<CashflowPoint>> ForecastAsync(string userId, int historyMonths = 6, int forecastMonths = 3, CancellationToken ct = default)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-(historyMonths - 1));
        var accountIds = await _db.Accounts.Where(a => a.UserId == userId).Select(a => a.Id).ToListAsync(ct);
        var categoryIds = await _db.Categories.Where(c => c.UserId == userId).Select(c => c.Id).ToListAsync(ct);

        var txs = await _db.Transactions
            .Where(t => t.Date >= start &&
                        ((t.AccountId != null && accountIds.Contains(t.AccountId.Value)) ||
                         (t.CategoryId != null && categoryIds.Contains(t.CategoryId.Value))))
            .ToListAsync(ct);

        var points = new List<CashflowPoint>();
        decimal running = await _db.Accounts.Where(a => a.UserId == userId).SumAsync(a => a.Balance, ct);

        for (var i = 0; i < historyMonths; i++)
        {
            var month = start.AddMonths(i);
            var next = month.AddMonths(1);
            var slice = txs.Where(t => t.Date >= month && t.Date < next).ToList();
            var income = slice.Where(t => t.Amount > 0 && t.TransactionType == Models.TransactionType.Income).Sum(t => t.Amount)
                         + slice.Where(t => t.TransactionType == Models.TransactionType.Income).Sum(t => Math.Abs(t.Amount));
            // Prefer TransactionType
            income = slice.Where(t => t.TransactionType == Models.TransactionType.Income).Sum(t => Math.Abs(t.Amount));
            var expense = slice.Where(t => t.TransactionType == Models.TransactionType.Expense).Sum(t => Math.Abs(t.Amount));
            var net = income - expense;
            running += net;
            points.Add(new CashflowPoint(month, income, expense, net, running));
        }

        if (points.Count == 0) return points;

        var avgIncome = points.Average(p => p.Income);
        var avgExpense = points.Average(p => p.Expense);
        var last = points[^1].Month;
        for (var f = 1; f <= forecastMonths; f++)
        {
            var month = last.AddMonths(f);
            var net = avgIncome - avgExpense;
            running += net;
            points.Add(new CashflowPoint(month, Math.Round(avgIncome, 2), Math.Round(avgExpense, 2), Math.Round(net, 2), Math.Round(running, 2)));
        }

        return points;
    }
}
