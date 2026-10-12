using Xunit;
using DoughDoodle.Data;
using DoughDoodle.Helpers;
using DoughDoodle.Models;
using DoughDoodle.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Tests;

public class SafeToSpendTests
{
    private static BudgetDbContext CreateDb()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<BudgetDbContext>().UseSqlite(conn).Options;
        var db = new BudgetDbContext(opts);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task DoughLeftToday_DividesRemainingByDaysLeft()
    {
        await using var db = CreateDb();
        var userId = "user-1";
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = "t@t.com",
            Email = "t@t.com",
            FirstName = "T",
            LastName = "U",
            NormalizedUserName = "T@T.COM",
            NormalizedEmail = "T@T.COM"
        });
        var cat = new Category { Name = "Groceries", UserId = userId };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        var month = DateTime.Today.ToString("yyyy-MM");
        db.MonthlyBudgets.Add(new MonthlyBudget { CategoryId = cat.Id, MonthYear = month, Amount = 300m });
        db.Transactions.Add(new Transaction
        {
            CategoryId = cat.Id,
            Description = "Shop",
            Amount = 100m,
            Date = DateTime.Today,
            TransactionType = TransactionType.Expense
        });
        await db.SaveChangesAsync();

        var svc = new SafeToSpendService(db);
        var result = await svc.CalculateAsync(userId, month);

        Assert.Equal(200m, result.RemainingBudget);
        Assert.True(result.DaysRemaining >= 1);
        Assert.Equal(decimal.Round(200m / result.DaysRemaining, 2), result.DoughLeftToday);
    }

    [Fact]
    public async Task PaceAndOverBudgetMessages_UsePreferredCurrency()
    {
        await using var db = CreateDb();
        var userId = "user-zar";
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = "z@t.com",
            Email = "z@t.com",
            FirstName = "Z",
            LastName = "A",
            NormalizedUserName = "Z@T.COM",
            NormalizedEmail = "Z@T.COM",
            PreferredCurrency = "ZAR"
        });
        var cat = new Category { Name = "Subscriptions", UserId = userId };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        const string month = "2026-10";
        db.MonthlyBudgets.Add(new MonthlyBudget { CategoryId = cat.Id, MonthYear = month, Amount = 310m });
        db.Transactions.Add(new Transaction
        {
            CategoryId = cat.Id,
            Description = "Stream",
            Amount = 400m,
            Date = new DateTime(2026, 10, 20),
            TransactionType = TransactionType.Expense
        });
        await db.SaveChangesAsync();

        var svc = new SafeToSpendService(db);
        var result = await svc.CalculateAsync(userId, month, asOf: new DateTime(2026, 10, 20));

        var over = Assert.Single(result.Alerts, a => a.CategoryName == "Subscriptions");
        Assert.Contains(CurrencyDisplay.Format(90m, "ZAR"), over.Message);
        Assert.DoesNotContain("$", over.Message);

        var pace = Assert.Single(result.Alerts, a => a.CategoryName == null && a.Severity == "warning");
        Assert.Contains(CurrencyDisplay.Format(Math.Abs(result.PaceVariance), "ZAR"), pace.Message);
        Assert.DoesNotContain("$", pace.Message);
    }
}
