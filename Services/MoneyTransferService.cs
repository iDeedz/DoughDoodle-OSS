using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Services;

public interface IMoneyTransferService
{
    Task<bool> TransferBetweenCategoriesAsync(string userId, int fromCategoryId, int toCategoryId, decimal amount, string monthYear, string description = "");
    Task<bool> CoverOverspendFromCategoryAsync(string userId, int overspentCategoryId, int coveringCategoryId, decimal amount, string monthYear);
    Task<decimal> GetAvailableToTransferAsync(int categoryId, string monthYear);
    Task<List<Category>> GetCategoriesWithSurplusAsync(string userId, string monthYear);
}

public class MoneyTransferService : IMoneyTransferService
{
    private readonly BudgetDbContext _context;

    public MoneyTransferService(BudgetDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TransferBetweenCategoriesAsync(string userId, int fromCategoryId, int toCategoryId, decimal amount, string monthYear, string description = "")
    {
        if (amount <= 0) return false;

        // Verify both categories belong to the user
        var categories = await _context.Categories
            .Where(c => c.UserId == userId && (c.Id == fromCategoryId || c.Id == toCategoryId))
            .ToListAsync();

        if (categories.Count != 2) return false;

        var fromCategory = categories.First(c => c.Id == fromCategoryId);
        var toCategory = categories.First(c => c.Id == toCategoryId);

        // Check if source category has enough available funds
        var availableAmount = await GetAvailableToTransferAsync(fromCategoryId, monthYear);
        if (availableAmount < amount) return false;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create transfer transaction for the "from" category (negative amount)
            var transferOutTransaction = new Transaction
            {
                Description = string.IsNullOrEmpty(description) ? $"Transfer to {toCategory.Name}" : description,
                Amount = -amount, // Negative to reduce the from category
                Date = DateTime.UtcNow,
                TransactionType = TransactionType.Transfer,
                CategoryId = fromCategoryId,
                TransferToCategoryId = toCategoryId,
                Notes = $"Transfer from {fromCategory.Name} to {toCategory.Name}"
            };

            // Create transfer transaction for the "to" category (positive amount)
            var transferInTransaction = new Transaction
            {
                Description = string.IsNullOrEmpty(description) ? $"Transfer from {fromCategory.Name}" : description,
                Amount = amount, // Positive to increase the to category
                Date = DateTime.UtcNow,
                TransactionType = TransactionType.Transfer,
                CategoryId = toCategoryId,
                TransferToCategoryId = fromCategoryId,
                Notes = $"Transfer from {fromCategory.Name} to {toCategory.Name}"
            };

            _context.Transactions.Add(transferOutTransaction);
            _context.Transactions.Add(transferInTransaction);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> CoverOverspendFromCategoryAsync(string userId, int overspentCategoryId, int coveringCategoryId, decimal amount, string monthYear)
    {
        var description = $"Cover overspend from category";
        return await TransferBetweenCategoriesAsync(userId, coveringCategoryId, overspentCategoryId, amount, monthYear, description);
    }

    public async Task<decimal> GetAvailableToTransferAsync(int categoryId, string monthYear)
    {
        // Get the budget for this category and month
        var budget = await _context.MonthlyBudgets
            .FirstOrDefaultAsync(mb => mb.CategoryId == categoryId && mb.MonthYear == monthYear);

        if (budget == null) return 0;

        // Get total spent in this category for the month
        var startDate = DateTime.ParseExact(monthYear + "-01", "yyyy-MM-dd", null);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var totalSpent = await _context.Transactions
            .Where(t => t.CategoryId == categoryId && t.Date >= startDate && t.Date <= endDate)
            .SumAsync(t => t.Amount);

        var available = budget.Amount + totalSpent; // totalSpent is negative for expenses, so we add it
        return Math.Max(0, available);
    }

    public async Task<List<Category>> GetCategoriesWithSurplusAsync(string userId, string monthYear)
    {
        var startDate = DateTime.ParseExact(monthYear + "-01", "yyyy-MM-dd", null);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .Include(c => c.Budgets.Where(b => b.MonthYear == monthYear))
            .Include(c => c.Transactions.Where(t => t.Date >= startDate && t.Date <= endDate))
            .ToListAsync();

        return categories.Where(c => 
        {
            var budget = c.Budgets.FirstOrDefault()?.Amount ?? 0;
            var spent = c.Transactions.Sum(t => t.Amount);
            var available = budget + spent; // spent is negative, so we add it
            return available > 0;
        }).ToList();
    }
}