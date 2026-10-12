using Xunit;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Tests;

public class SubscriptionHunterTests
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

    private static async Task<string> AddUserAsync(BudgetDbContext db, string id, string email)
    {
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            UserName = email,
            Email = email,
            FirstName = "T",
            LastName = "U",
            NormalizedUserName = email.ToUpperInvariant(),
            NormalizedEmail = email.ToUpperInvariant()
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task<Category> AddCategoryAsync(BudgetDbContext db, string userId, string name)
    {
        var cat = new Category { Name = name, UserId = userId };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return cat;
    }

    private static ScheduledTransaction Schedule(
        string userId,
        int categoryId,
        string description,
        decimal amount,
        RecurrenceType cadence,
        bool isActive = true,
        TransactionType transactionType = TransactionType.Expense)
    {
        return new ScheduledTransaction
        {
            UserId = userId,
            CategoryId = categoryId,
            Description = description,
            Amount = amount,
            RecurrenceType = cadence,
            RecurrenceInterval = 1,
            StartDate = DateTime.Today,
            NextDueDate = DateTime.Today,
            IsActive = isActive,
            TransactionType = transactionType,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public void ToMonthlyAmount_ConvertsCadencesWithDecimalMath()
    {
        Assert.Equal(10m, SubscriptionHunterService.ToMonthlyAmount(new ScheduledTransaction
        {
            Amount = 120m,
            RecurrenceType = RecurrenceType.Annually
        }));
        Assert.Equal(10m, SubscriptionHunterService.ToMonthlyAmount(new ScheduledTransaction
        {
            Amount = 30m,
            RecurrenceType = RecurrenceType.Quarterly
        }));
        Assert.Equal(10m, SubscriptionHunterService.ToMonthlyAmount(new ScheduledTransaction
        {
            Amount = 60m,
            RecurrenceType = RecurrenceType.SemiAnnually
        }));
        Assert.Equal(15.99m, SubscriptionHunterService.ToMonthlyAmount(new ScheduledTransaction
        {
            Amount = 15.99m,
            RecurrenceType = RecurrenceType.Monthly
        }));
        Assert.Equal(10m * 52m / 12m, SubscriptionHunterService.ToMonthlyAmount(new ScheduledTransaction
        {
            Amount = 10m,
            RecurrenceType = RecurrenceType.Weekly
        }));
        Assert.Equal(10m * 26m / 12m, SubscriptionHunterService.ToMonthlyAmount(new ScheduledTransaction
        {
            Amount = 10m,
            RecurrenceType = RecurrenceType.Biweekly
        }));
    }

    [Fact]
    public void IsSubscriptionSuspect_MonthlyCadenceWithoutKeyword()
    {
        var st = new ScheduledTransaction
        {
            Description = "Rent",
            RecurrenceType = RecurrenceType.Monthly
        };
        Assert.True(SubscriptionHunterService.IsSubscriptionSuspect(st));
    }

    [Fact]
    public void IsSubscriptionSuspect_WeeklyNeedsKeyword()
    {
        var gym = new ScheduledTransaction
        {
            Description = "Gym membership",
            RecurrenceType = RecurrenceType.Weekly,
            Category = new Category { Name = "Fitness" }
        };
        var groceries = new ScheduledTransaction
        {
            Description = "Weekly shop",
            RecurrenceType = RecurrenceType.Weekly,
            Category = new Category { Name = "Groceries" }
        };
        Assert.True(SubscriptionHunterService.IsSubscriptionSuspect(gym));
        Assert.False(SubscriptionHunterService.IsSubscriptionSuspect(groceries));
    }

    [Fact]
    public async Task AnalyzeAsync_RoundsBurnAndIgnoresOtherUsersAndInactive()
    {
        await using var db = CreateDb();
        var user1 = await AddUserAsync(db, "user-1", "a@t.com");
        var user2 = await AddUserAsync(db, "user-2", "b@t.com");
        var cat1 = await AddCategoryAsync(db, user1, "Entertainment");
        var cat2 = await AddCategoryAsync(db, user2, "Entertainment");

        db.ScheduledTransactions.AddRange(
            Schedule(user1, cat1.Id, "Netflix", 15.99m, RecurrenceType.Monthly),
            Schedule(user1, cat1.Id, "Adobe", 119.88m, RecurrenceType.Annually),
            Schedule(user1, cat1.Id, "Old gym", 40m, RecurrenceType.Monthly, isActive: false),
            Schedule(user2, cat2.Id, "Spotify", 999m, RecurrenceType.Monthly)
        );
        await db.SaveChangesAsync();

        var result = await new SubscriptionHunterService(db).AnalyzeAsync(user1);

        Assert.Equal(2, result.SuspectCount);
        Assert.Equal(decimal.Round(15.99m + (119.88m / 12m), 2), result.MonthlyBurn);
        Assert.Equal(decimal.Round(result.MonthlyBurn * 12m, 2), result.AnnualBurn);
        Assert.DoesNotContain(result.Suspects, s => s.Description == "Spotify");
        Assert.DoesNotContain(result.Suspects, s => s.Description == "Old gym");
    }

    [Fact]
    public async Task AnalyzeAsync_EmptyUser_HasFriendlyZeroState()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db, "user-empty", "e@t.com");
        var result = await new SubscriptionHunterService(db).AnalyzeAsync(userId);

        Assert.Equal(0, result.SuspectCount);
        Assert.Equal(0m, result.MonthlyBurn);
        Assert.Equal(0m, result.AnnualBurn);
        Assert.Empty(result.Suspects);
    }

    [Fact]
    public async Task AnalyzeAsync_MissingUserId_ReturnsEmpty()
    {
        await using var db = CreateDb();
        var result = await new SubscriptionHunterService(db).AnalyzeAsync("");
        Assert.Equal(0, result.SuspectCount);
        Assert.Equal(0m, result.MonthlyBurn);
    }

    [Fact]
    public async Task AnalyzeAsync_MonthlyIncome_DoesNotInflateBurn()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db, "user-inc", "i@t.com");
        var cat = await AddCategoryAsync(db, userId, "Income");
        db.ScheduledTransactions.AddRange(
            Schedule(userId, cat.Id, "Salary", 5000m, RecurrenceType.Monthly, transactionType: TransactionType.Income),
            Schedule(userId, cat.Id, "Internal move", 200m, RecurrenceType.Monthly, transactionType: TransactionType.Transfer),
            Schedule(userId, cat.Id, "Netflix", 15.99m, RecurrenceType.Monthly)
        );
        await db.SaveChangesAsync();

        var result = await new SubscriptionHunterService(db).AnalyzeAsync(userId);

        Assert.Equal(1, result.SuspectCount);
        Assert.Equal(15.99m, result.MonthlyBurn);
        Assert.DoesNotContain(result.Suspects, s => s.Description == "Salary");
        Assert.DoesNotContain(result.Suspects, s => s.Description == "Internal move");
    }
}
