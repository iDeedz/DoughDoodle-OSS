using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Services;

public static class EnvelopeStarterSets
{
    public const string Defaults = "defaults";
    public const string Minimal = "minimal";

    /// <summary>
    /// Onboarding select is only <c>defaults</c> or <c>minimal</c>.
    /// Empty, unknown, and <c>defaults</c> seed the classic set.
    /// <c>minimal</c> ("I'll add my own later") seeds nothing.
    /// </summary>
    public static bool ShouldSeedDefaultEnvelopes(string? starterSet)
    {
        if (string.IsNullOrWhiteSpace(starterSet))
            return true;

        return !string.Equals(starterSet.Trim(), Minimal, StringComparison.OrdinalIgnoreCase);
    }
}

public interface IDefaultCategoryService
{
    Task CreateDefaultCategoriesAsync(string userId);
    Task CreateStarterCategoriesAsync(string userId, string? starterSet);
    Task ApplyStarterSetAsync(string userId, string? starterSet);
}

public class DefaultCategoryService : IDefaultCategoryService
{
    private readonly BudgetDbContext _context;

    public DefaultCategoryService(BudgetDbContext context)
    {
        _context = context;
    }

    public Task CreateDefaultCategoriesAsync(string userId)
        => CreateStarterCategoriesAsync(userId, "defaults");

    public async Task CreateStarterCategoriesAsync(string userId, string? starterSet)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        // Check if user already has categories
        if (_context.Categories.Any(c => c.UserId == userId))
        {
            return;
        }

        var set = (starterSet ?? "defaults").Trim().ToLowerInvariant();
        var categories = set == "minimal"
            ? GetEssentialCategories(userId)
            : GetDefaultCategories(userId);

        _context.Categories.AddRange(categories);
        await _context.SaveChangesAsync();
    }

    public async Task ApplyStarterSetAsync(string userId, string? starterSet)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        var set = (starterSet ?? EnvelopeStarterSets.Defaults).Trim().ToLowerInvariant();
        var existingNames = await _context.Categories
            .Where(c => c.UserId == userId)
            .Select(c => c.Name)
            .ToListAsync();

        if (set == EnvelopeStarterSets.Minimal)
        {
            if (existingNames.Count == 0)
            {
                _context.Categories.Add(new Category { Name = "Uncategorized", UserId = userId });
                await _context.SaveChangesAsync();
            }

            return;
        }

        var hasOnlyUncategorized = existingNames.Count == 1 &&
                                   string.Equals(existingNames[0], "Uncategorized", StringComparison.OrdinalIgnoreCase);
        if (existingNames.Count > 0 && !hasOnlyUncategorized)
            return;

        var defaults = GetDefaultCategories(userId);
        if (existingNames.Count > 0)
        {
            defaults = defaults
                .Where(c => !existingNames.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        if (defaults.Count == 0)
            return;

        _context.Categories.AddRange(defaults);
        await _context.SaveChangesAsync();
    }

    private List<Category> GetDefaultCategories(string userId)
    {
        return new List<Category>
        {
            // Essential Living Expenses
            new Category { Name = "Rent/Mortgage", UserId = userId },
            new Category { Name = "Utilities", UserId = userId },
            new Category { Name = "Groceries", UserId = userId },
            new Category { Name = "Transportation/Gas", UserId = userId },
            new Category { Name = "Insurance", UserId = userId },
            new Category { Name = "Phone/Internet", UserId = userId },
            
            // Financial Goals & Security
            new Category { Name = "Emergency Fund", UserId = userId },
            new Category { Name = "Retirement Savings", UserId = userId },
            new Category { Name = "Debt Payments", UserId = userId },
            new Category { Name = "Investment Savings", UserId = userId },
            
            // Personal Care & Health
            new Category { Name = "Healthcare/Medical", UserId = userId },
            new Category { Name = "Personal Care", UserId = userId },
            new Category { Name = "Clothing", UserId = userId },
            
            // Lifestyle & Entertainment
            new Category { Name = "Dining Out", UserId = userId },
            new Category { Name = "Entertainment", UserId = userId },
            new Category { Name = "Hobbies", UserId = userId },
            new Category { Name = "Travel/Vacation", UserId = userId },
            new Category { Name = "Subscriptions", UserId = userId },
            
            // Family & Dependents
            new Category { Name = "Childcare", UserId = userId },
            new Category { Name = "Education", UserId = userId },
            new Category { Name = "Pet Care", UserId = userId },
            
            // Home & Maintenance
            new Category { Name = "Home Maintenance", UserId = userId },
            new Category { Name = "Home Improvement", UserId = userId },
            
            // Professional & Business
            new Category { Name = "Professional Development", UserId = userId },
            new Category { Name = "Business Expenses", UserId = userId },
            
            // Miscellaneous & Flexible
            new Category { Name = "Gifts & Donations", UserId = userId },
            new Category { Name = "Miscellaneous", UserId = userId },
            new Category { Name = "Fun Money", UserId = userId }
        };
    }

    private List<Category> GetEssentialCategories(string userId)
    {
        // Subset of GetDefaultCategories "Essential Living Expenses" - no invented names.
        return new List<Category>
        {
            new Category { Name = "Rent/Mortgage", UserId = userId },
            new Category { Name = "Utilities", UserId = userId },
            new Category { Name = "Groceries", UserId = userId },
            new Category { Name = "Transportation/Gas", UserId = userId },
            new Category { Name = "Insurance", UserId = userId },
            new Category { Name = "Phone/Internet", UserId = userId }
        };
    }
}
