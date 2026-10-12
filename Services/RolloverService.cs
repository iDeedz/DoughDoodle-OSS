using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Services;

public interface IRolloverService
{
    Task ProcessMonthlyRolloversAsync(string userId, string fromMonth, string toMonth);
    Task<List<CategoryRollover>> GetRolloverPreviewAsync(string userId, string monthYear);
    Task<bool> ApproveRolloverAsync(string userId, string fromMonth, string toMonth);
    Task<decimal> GetCategoryRolloverAmountAsync(int categoryId, string monthYear);
}

public class RolloverService : IRolloverService
{
    private readonly BudgetDbContext _context;
    private readonly IMoneyTransferService _transferService;

    public RolloverService(BudgetDbContext context, IMoneyTransferService transferService)
    {
        _context = context;
        _transferService = transferService;
    }

    public async Task ProcessMonthlyRolloversAsync(string userId, string fromMonth, string toMonth)
    {
        var userSettings = await GetOrCreateUserSettingsAsync(userId);
        
        if (!userSettings.AutoRolloverSurplus && !userSettings.AutoRolloverOverspend)
            return;

        var rollovers = await GetRolloverPreviewAsync(userId, fromMonth);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var rollover in rollovers)
            {
                // Skip if settings don't allow this type of rollover
                if (rollover.IsOverspend && !userSettings.AutoRolloverOverspend)
                    continue;
                    
                if (!rollover.IsOverspend && !userSettings.AutoRolloverSurplus)
                    continue;

                // Save the rollover record
                rollover.ToMonthYear = toMonth;
                _context.CategoryRollovers.Add(rollover);

                if (!rollover.IsOverspend && rollover.Amount > 0)
                {
                    // For surplus, create a positive adjustment transaction in the next month
                    var adjustmentTransaction = new Transaction
                    {
                        Description = $"Rollover surplus from {rollover.FromMonthYear}",
                        Amount = rollover.Amount,
                        Date = DateTime.ParseExact(toMonth + "-01", "yyyy-MM-dd", null),
                        TransactionType = TransactionType.Income,
                        CategoryId = rollover.CategoryId,
                        Notes = "Automatic rollover from previous month"
                    };
                    
                    _context.Transactions.Add(adjustmentTransaction);
                }
                else if (rollover.IsOverspend && rollover.Amount < 0)
                {
                    // For overspend, create a negative adjustment transaction in the next month
                    var adjustmentTransaction = new Transaction
                    {
                        Description = $"Rollover overspend from {rollover.FromMonthYear}",
                        Amount = rollover.Amount, // Already negative
                        Date = DateTime.ParseExact(toMonth + "-01", "yyyy-MM-dd", null),
                        TransactionType = TransactionType.Expense,
                        CategoryId = rollover.CategoryId,
                        Notes = "Automatic rollover overspend from previous month"
                    };
                    
                    _context.Transactions.Add(adjustmentTransaction);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<CategoryRollover>> GetRolloverPreviewAsync(string userId, string monthYear)
    {
        var startDate = DateTime.ParseExact(monthYear + "-01", "yyyy-MM-dd", null);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .Include(c => c.Budgets.Where(b => b.MonthYear == monthYear))
            .Include(c => c.Transactions.Where(t => t.Date >= startDate && t.Date <= endDate))
            .ToListAsync();

        var rollovers = new List<CategoryRollover>();

        foreach (var category in categories)
        {
            var budget = category.Budgets.FirstOrDefault()?.Amount ?? 0;
            var spent = category.Transactions.Sum(t => t.Amount);
            var remaining = budget + spent; // spent is negative for expenses

            if (remaining != 0) // Only create rollovers for categories with surplus or deficit
            {
                rollovers.Add(new CategoryRollover
                {
                    CategoryId = category.Id,
                    Category = category,
                    FromMonthYear = monthYear,
                    Amount = remaining,
                    IsOverspend = remaining < 0,
                    Notes = remaining > 0 ? "Surplus to rollover" : "Overspend to rollover"
                });
            }
        }

        return rollovers;
    }

    public async Task<bool> ApproveRolloverAsync(string userId, string fromMonth, string toMonth)
    {
        await ProcessMonthlyRolloversAsync(userId, fromMonth, toMonth);
        return true;
    }

    public async Task<decimal> GetCategoryRolloverAmountAsync(int categoryId, string monthYear)
    {
        var rollover = await _context.CategoryRollovers
            .FirstOrDefaultAsync(cr => cr.CategoryId == categoryId && cr.ToMonthYear == monthYear);
        
        return rollover?.Amount ?? 0;
    }

    private async Task<BudgetSettings> GetOrCreateUserSettingsAsync(string userId)
    {
        var settings = await _context.BudgetSettings
            .FirstOrDefaultAsync(bs => bs.UserId == userId);

        if (settings == null)
        {
            settings = new BudgetSettings
            {
                UserId = userId,
                AutoRolloverSurplus = true,
                AutoRolloverOverspend = true
            };
            
            _context.BudgetSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return settings;
    }
}