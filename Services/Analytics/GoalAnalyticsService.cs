using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Services.Analytics;

public record GoalAnalyticsDto(
    int GoalId,
    string Name,
    decimal ProgressPct,
    decimal Remaining,
    decimal SuggestedMonthly,
    DateTime? ProjectedCompletion,
    string PaceStatus);

public interface IGoalAnalyticsService
{
    Task<IReadOnlyList<GoalAnalyticsDto>> AnalyzeAsync(string userId, CancellationToken ct = default);
}

public class GoalAnalyticsService : IGoalAnalyticsService
{
    private readonly BudgetDbContext _db;

    public GoalAnalyticsService(BudgetDbContext db) => _db = db;

    public async Task<IReadOnlyList<GoalAnalyticsDto>> AnalyzeAsync(string userId, CancellationToken ct = default)
    {
        var goals = await _db.FinancialGoals
            .Where(g => g.UserId == userId && g.IsActive)
            .ToListAsync(ct);

        var list = new List<GoalAnalyticsDto>();
        foreach (var g in goals)
        {
            var remaining = Math.Max(0, g.TargetAmount - g.CurrentAmount);
            decimal suggested = 0;
            DateTime? projected = null;
            var pace = "On track";

            if (g.TargetDate.HasValue && g.TargetDate > DateTime.UtcNow)
            {
                var monthsLeft = Math.Max(1m, (decimal)(g.TargetDate.Value - DateTime.UtcNow).TotalDays / 30m);
                suggested = Math.Round(remaining / monthsLeft, 2);
                if (g.CurrentAmount > 0 && g.CreatedAt < DateTime.UtcNow)
                {
                    var elapsedMonths = Math.Max(0.25m, (decimal)(DateTime.UtcNow - g.CreatedAt).TotalDays / 30m);
                    var velocity = g.CurrentAmount / elapsedMonths;
                    if (velocity > 0)
                        projected = DateTime.UtcNow.AddDays((double)(remaining / velocity * 30m));
                    if (projected.HasValue && g.TargetDate.HasValue && projected > g.TargetDate)
                        pace = "Behind";
                    else if (projected.HasValue && projected < g.TargetDate.Value.AddDays(-14))
                        pace = "Ahead";
                }
            }
            else if (remaining > 0)
            {
                suggested = Math.Round(remaining / 12m, 2);
                pace = "No target date";
            }
            else
            {
                pace = "Complete";
            }

            list.Add(new GoalAnalyticsDto(g.Id, g.Name, g.ProgressPercentage, remaining, suggested, projected, pace));
        }

        return list;
    }
}
