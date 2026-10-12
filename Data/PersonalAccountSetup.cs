using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Data;

/// <summary>
/// This class provides a template for setting up a personal account with South African financial categories.
/// DO NOT store real credentials in source code. Use this as a reference only.
/// </summary>
public static class PersonalAccountSetup
{
    /// <summary>
    /// Creates a personal user account with appropriate categories for South African budgeting and debt review.
    /// NOTE: This should be called manually via admin interface or one-time setup, NOT from seed data.
    /// </summary>
    public static async Task CreatePersonalAccountAsync(
        BudgetDbContext context, 
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string firstName,
        string lastName)
    {
        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            Console.WriteLine($"User {email} already exists. Skipping creation.");
            return;
        }

        // Create the user account
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            UserType = UserType.MainAccount,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            EmailConfirmed = true,
            TrialStartDate = DateTime.UtcNow,
            TrialEndDate = DateTime.UtcNow.AddDays(30)
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            Console.WriteLine($"Failed to create user {email}:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  - {error.Description}");
            }
            return;
        }

        Console.WriteLine($"Successfully created user account: {email}");

        // Create comprehensive South African budget categories
        var categories = CreateSouthAfricanBudgetCategories(user.Id);
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        Console.WriteLine($"Created {categories.Length} budget categories for {email}");
    }

    /// <summary>
    /// Creates a comprehensive set of budget categories suitable for South African users,
    /// including debt review (PDA) specific categories.
    /// </summary>
    private static Category[] CreateSouthAfricanBudgetCategories(string userId)
    {
        return new[]
        {
            // Essential Living Expenses
            new Category { Name = "Groceries & Food", UserId = userId },
            new Category { Name = "Rent/Bond Payment", UserId = userId },
            new Category { Name = "Electricity & Water", UserId = userId },
            new Category { Name = "Municipal Rates & Taxes", UserId = userId },
            new Category { Name = "Home Maintenance", UserId = userId },
            
            // Transportation
            new Category { Name = "Car Payment/Installment", UserId = userId },
            new Category { Name = "Fuel/Petrol", UserId = userId },
            new Category { Name = "Car Insurance", UserId = userId },
            new Category { Name = "Car Maintenance & Repairs", UserId = userId },
            new Category { Name = "Public Transport/Taxi", UserId = userId },
            new Category { Name = "Toll Fees", UserId = userId },
            
            // Insurance & Protection
            new Category { Name = "Life Insurance", UserId = userId },
            new Category { Name = "Medical Aid/Health Insurance", UserId = userId },
            new Category { Name = "Home Insurance", UserId = userId },
            new Category { Name = "Funeral Cover", UserId = userId },
            
            // Debt Payments (Debt Review/PDA Specific)
            new Category { Name = "PDA - Debt Review Payment", UserId = userId },
            new Category { Name = "Credit Card Payment", UserId = userId },
            new Category { Name = "Personal Loan Payment", UserId = userId },
            new Category { Name = "Store Account Payment", UserId = userId },
            new Category { Name = "Overdraft Payment", UserId = userId },
            
            // Communication
            new Category { Name = "Cellphone Contract", UserId = userId },
            new Category { Name = "Internet & WIFI", UserId = userId },
            new Category { Name = "DSTV/Streaming Services", UserId = userId },
            
            // Family & Education
            new Category { Name = "School Fees", UserId = userId },
            new Category { Name = "Childcare/Daycare", UserId = userId },
            new Category { Name = "Children's Activities", UserId = userId },
            new Category { Name = "School Supplies", UserId = userId },
            
            // Personal Care
            new Category { Name = "Clothing & Shoes", UserId = userId },
            new Category { Name = "Haircare & Beauty", UserId = userId },
            new Category { Name = "Medical Expenses", UserId = userId },
            new Category { Name = "Pharmacy & Medication", UserId = userId },
            
            // Household
            new Category { Name = "Cleaning Supplies", UserId = userId },
            new Category { Name = "Toiletries", UserId = userId },
            new Category { Name = "Household Items", UserId = userId },
            
            // Savings & Investments
            new Category { Name = "Emergency Fund", UserId = userId },
            new Category { Name = "Savings Account", UserId = userId },
            new Category { Name = "Retirement Annuity", UserId = userId },
            new Category { Name = "Investment/Unit Trusts", UserId = userId },
            
            // Discretionary/Lifestyle
            new Category { Name = "Entertainment & Leisure", UserId = userId },
            new Category { Name = "Dining Out & Takeaways", UserId = userId },
            new Category { Name = "Hobbies", UserId = userId },
            new Category { Name = "Gifts & Donations", UserId = userId },
            new Category { Name = "Gym/Fitness Membership", UserId = userId },
            
            // Other
            new Category { Name = "Miscellaneous", UserId = userId },
            new Category { Name = "Uncategorized", UserId = userId }
        };
    }

    /// <summary>
    /// Creates sample monthly budgets for demonstration purposes.
    /// These should be adjusted based on actual income and debt review plan.
    /// </summary>
    public static async Task CreateSampleBudgetsAsync(BudgetDbContext context, string userId)
    {
        var categories = await context.Categories
            .Where(c => c.UserId == userId)
            .ToListAsync();

        var currentMonth = DateTime.Now.ToString("yyyy-MM");
        
        // Sample budget allocations - these are examples and should be customized
        var budgetSamples = new Dictionary<string, decimal>
        {
            { "Groceries & Food", 3500.00m },
            { "Rent/Bond Payment", 8000.00m },
            { "Electricity & Water", 1500.00m },
            { "Fuel/Petrol", 1200.00m },
            { "PDA - Debt Review Payment", 5000.00m },
            { "Medical Aid/Health Insurance", 2500.00m },
            { "Car Insurance", 800.00m },
            { "Cellphone Contract", 500.00m },
            { "Emergency Fund", 500.00m }
        };

        foreach (var category in categories)
        {
            if (budgetSamples.TryGetValue(category.Name, out var amount))
            {
                var budget = new MonthlyBudget
                {
                    CategoryId = category.Id,
                    MonthYear = currentMonth,
                    Amount = amount
                };
                context.MonthlyBudgets.Add(budget);
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"Created sample budget allocations for {currentMonth}");
    }
}
