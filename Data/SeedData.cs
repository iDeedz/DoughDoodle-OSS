using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Data;

public static class SeedData
{
    public static async Task InitializeAsync(BudgetDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, bool allowDemoUsers = true)
    {
        if (!allowDemoUsers)
        {
            Console.WriteLine("Demo seeding skipped (Seed:AllowDemoUsers=false).");
            return;
        }

        // Check if demo data already exists
        if (context.Categories.Any())
        {
            return; // Database has been seeded
        }

        // Create a demo main account
        var demoUser = new ApplicationUser
        {
            UserName = "demo@doughdoodle.com",
            Email = "demo@doughdoodle.com",
            FirstName = "Demo",
            LastName = "User",
            UserType = UserType.MainAccount,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            EmailConfirmed = true,
            TrialStartDate = DateTime.UtcNow,
            TrialEndDate = DateTime.UtcNow.AddDays(30)
        };

        var result = await userManager.CreateAsync(demoUser, "Demo123!");
        if (!result.Succeeded)
        {
            return; // Failed to create demo user
        }

        // Create sample categories (envelopes) for demo user
        var categories = new[]
        {
            new Category { Name = "Groceries", UserId = demoUser.Id },
            new Category { Name = "Rent/Mortgage", UserId = demoUser.Id },
            new Category { Name = "Transportation", UserId = demoUser.Id },
            new Category { Name = "Utilities", UserId = demoUser.Id },
            new Category { Name = "Entertainment", UserId = demoUser.Id },
            new Category { Name = "Emergency Fund", UserId = demoUser.Id },
            new Category { Name = "Dining Out", UserId = demoUser.Id },
            new Category { Name = "Clothing", UserId = demoUser.Id }
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        // Get the current month
        var currentMonth = DateTime.Now.ToString("yyyy-MM");

        // Create sample budgets for current month
        var budgets = new[]
        {
            new MonthlyBudget { CategoryId = categories[0].Id, MonthYear = currentMonth, Amount = 400.00m }, // Groceries
            new MonthlyBudget { CategoryId = categories[1].Id, MonthYear = currentMonth, Amount = 1200.00m }, // Rent
            new MonthlyBudget { CategoryId = categories[2].Id, MonthYear = currentMonth, Amount = 200.00m }, // Transportation
            new MonthlyBudget { CategoryId = categories[3].Id, MonthYear = currentMonth, Amount = 150.00m }, // Utilities
            new MonthlyBudget { CategoryId = categories[4].Id, MonthYear = currentMonth, Amount = 100.00m }, // Entertainment
            new MonthlyBudget { CategoryId = categories[5].Id, MonthYear = currentMonth, Amount = 300.00m }, // Emergency Fund
            new MonthlyBudget { CategoryId = categories[6].Id, MonthYear = currentMonth, Amount = 120.00m }, // Dining Out
            new MonthlyBudget { CategoryId = categories[7].Id, MonthYear = currentMonth, Amount = 80.00m }   // Clothing
        };

        context.MonthlyBudgets.AddRange(budgets);
        await context.SaveChangesAsync();

        // Create sample transactions
        var transactions = new[]
        {
            // Groceries
            new Transaction { CategoryId = categories[0].Id, Description = "Weekly grocery shopping", Amount = 85.50m, Date = DateTime.Now.AddDays(-5) },
            new Transaction { CategoryId = categories[0].Id, Description = "Fresh produce", Amount = 32.75m, Date = DateTime.Now.AddDays(-10) },
            new Transaction { CategoryId = categories[0].Id, Description = "Meat and dairy", Amount = 67.20m, Date = DateTime.Now.AddDays(-3) },
            
            // Rent
            new Transaction { CategoryId = categories[1].Id, Description = "Monthly rent payment", Amount = 1200.00m, Date = DateTime.Now.AddDays(-1) },
            
            // Transportation
            new Transaction { CategoryId = categories[2].Id, Description = "Gas station fill-up", Amount = 45.00m, Date = DateTime.Now.AddDays(-7) },
            new Transaction { CategoryId = categories[2].Id, Description = "Public transport pass", Amount = 35.00m, Date = DateTime.Now.AddDays(-15) },
            
            // Utilities
            new Transaction { CategoryId = categories[3].Id, Description = "Electricity bill", Amount = 78.90m, Date = DateTime.Now.AddDays(-2) },
            new Transaction { CategoryId = categories[3].Id, Description = "Internet service", Amount = 45.00m, Date = DateTime.Now.AddDays(-12) },
            
            // Entertainment
            new Transaction { CategoryId = categories[4].Id, Description = "Movie tickets", Amount = 24.00m, Date = DateTime.Now.AddDays(-4) },
            new Transaction { CategoryId = categories[4].Id, Description = "Streaming service", Amount = 12.99m, Date = DateTime.Now.AddDays(-8) },
            
            // Dining Out
            new Transaction { CategoryId = categories[6].Id, Description = "Restaurant dinner", Amount = 45.60m, Date = DateTime.Now.AddDays(-6) },
            new Transaction { CategoryId = categories[6].Id, Description = "Coffee shop", Amount = 8.75m, Date = DateTime.Now.AddDays(-9) },
            
            // Clothing
            new Transaction { CategoryId = categories[7].Id, Description = "Work shirt", Amount = 29.99m, Date = DateTime.Now.AddDays(-11) }
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        // Create a demo sub-account
        var subAccount = new ApplicationUser
        {
            UserName = "sub@doughdoodle.com",
            Email = "sub@doughdoodle.com",
            FirstName = "Sub",
            LastName = "Account",
            UserType = UserType.SubAccount,
            ParentUserId = demoUser.Id,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(subAccount, "Sub123!");

        // Create a few categories for the sub-account
        var subCategories = new[]
        {
            new Category { Name = "Personal Groceries", UserId = subAccount.Id },
            new Category { Name = "Personal Entertainment", UserId = subAccount.Id }
        };

        context.Categories.AddRange(subCategories);
        await context.SaveChangesAsync();

        // Add budgets for sub-account
        var subBudgets = new[]
        {
            new MonthlyBudget { CategoryId = subCategories[0].Id, MonthYear = currentMonth, Amount = 150.00m },
            new MonthlyBudget { CategoryId = subCategories[1].Id, MonthYear = currentMonth, Amount = 50.00m }
        };

        context.MonthlyBudgets.AddRange(subBudgets);
        await context.SaveChangesAsync();
    }
}