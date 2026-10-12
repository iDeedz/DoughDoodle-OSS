using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DoughDoodle.Tests;

public class DefaultCategoryStarterSetTests
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

    private static async Task AddUserAsync(BudgetDbContext db, string id)
    {
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            UserName = id + "@t.com",
            Email = id + "@t.com",
            FirstName = "T",
            LastName = "U",
            NormalizedUserName = (id + "@t.com").ToUpperInvariant(),
            NormalizedEmail = (id + "@t.com").ToUpperInvariant()
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Minimal_SeedsEssentialsOnly_OwnedByUser()
    {
        await using var db = CreateDb();
        await AddUserAsync(db, "u1");
        var svc = new DefaultCategoryService(db);

        await svc.CreateStarterCategoriesAsync("u1", "minimal");

        var names = await db.Categories.Where(c => c.UserId == "u1").Select(c => c.Name).ToListAsync();
        Assert.Equal(6, names.Count);
        Assert.Contains("Groceries", names);
        Assert.Contains("Rent/Mortgage", names);
        Assert.Contains("Utilities", names);
        Assert.Contains("Transportation/Gas", names);
        Assert.Contains("Insurance", names);
        Assert.Contains("Phone/Internet", names);
        Assert.DoesNotContain("Dining Out", names);
        Assert.DoesNotContain("Fun Money", names);
        Assert.All(await db.Categories.ToListAsync(), c => Assert.Equal("u1", c.UserId));
    }

    [Fact]
    public async Task Defaults_SeedsFullStarterSet_OwnedByUser()
    {
        await using var db = CreateDb();
        await AddUserAsync(db, "u1");
        var svc = new DefaultCategoryService(db);

        await svc.CreateStarterCategoriesAsync("u1", "defaults");

        var names = await db.Categories.Where(c => c.UserId == "u1").Select(c => c.Name).ToListAsync();
        Assert.Equal(28, names.Count);
        Assert.Contains("Groceries", names);
        Assert.Contains("Dining Out", names);
        Assert.All(await db.Categories.ToListAsync(), c => Assert.Equal("u1", c.UserId));
    }

    [Fact]
    public async Task CreateDefaultCategoriesAsync_IsClassicStarterSet()
    {
        await using var db = CreateDb();
        await AddUserAsync(db, "u1");
        var svc = new DefaultCategoryService(db);

        await svc.CreateDefaultCategoriesAsync("u1");

        Assert.Equal(28, await db.Categories.CountAsync(c => c.UserId == "u1"));
    }

    [Fact]
    public async Task UnknownStarterSet_FallsBackToClassicSet()
    {
        await using var db = CreateDb();
        await AddUserAsync(db, "u1");
        var svc = new DefaultCategoryService(db);

        await svc.CreateStarterCategoriesAsync("u1", "custom-unknown");

        Assert.Equal(28, await db.Categories.CountAsync(c => c.UserId == "u1"));
    }

    [Fact]
    public async Task StarterSet_DoesNotSeedOtherUsersOrOverwriteExisting()
    {
        await using var db = CreateDb();
        await AddUserAsync(db, "u1");
        await AddUserAsync(db, "u2");
        db.Categories.Add(new Category { Name = "Mine", UserId = "u1" });
        await db.SaveChangesAsync();
        var svc = new DefaultCategoryService(db);

        await svc.CreateStarterCategoriesAsync("u1", "defaults");
        await svc.CreateStarterCategoriesAsync("u2", "minimal");
        await svc.CreateStarterCategoriesAsync("", "defaults");

        Assert.Equal(1, await db.Categories.CountAsync(c => c.UserId == "u1"));
        Assert.Equal("Mine", (await db.Categories.SingleAsync(c => c.UserId == "u1")).Name);
        Assert.Equal(6, await db.Categories.CountAsync(c => c.UserId == "u2"));
        Assert.Equal(0, await db.Categories.CountAsync(c => c.UserId == ""));
    }
}
