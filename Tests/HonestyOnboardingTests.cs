using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DoughDoodle.Tests;

public class HonestyOnboardingTests
{
    [Fact]
    public void ImportStats_Empty_AreHonestZeros()
    {
        var summary = ImportSessionStats.From(Array.Empty<ImportSession>());
        Assert.Equal(0, summary.SessionCount);
        Assert.Equal(0, summary.RecordsImported);
        Assert.Equal(0, summary.FailedRecords);
        Assert.Null(summary.LastActivityAt);
        Assert.Equal(ImportSessionSummary.EmptyLastActivityLabel, summary.LastActivityLabel);
    }

    [Fact]
    public void ImportStats_SumsOnlyProvidedSessions()
    {
        var now = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
        var sessions = new[]
        {
            new ImportSession
            {
                UserId = "a",
                FileName = "a.csv",
                FileType = "CSV",
                SuccessfulRecords = 10,
                FailedRecords = 2,
                CreatedAt = now.AddDays(-2)
            },
            new ImportSession
            {
                UserId = "a",
                FileName = "b.csv",
                FileType = "CSV",
                SuccessfulRecords = 5,
                FailedRecords = 1,
                CreatedAt = now.AddHours(-3),
                CompletedAt = now.AddHours(-3)
            }
        };

        var summary = ImportSessionStats.From(sessions, now);
        Assert.Equal(2, summary.SessionCount);
        Assert.Equal(15, summary.RecordsImported);
        Assert.Equal(3, summary.FailedRecords);
        Assert.Equal(now.AddHours(-3), summary.LastActivityAt);
        Assert.Equal("3h ago", summary.LastActivityLabel);
    }

    [Fact]
    public async Task ImportStats_QueryIsScopedToClaimUser()
    {
        var db = CreateDb();
        db.Users.AddRange(
            User("user-a", "a@test.com"),
            User("user-b", "b@test.com"));
        db.ImportSessions.AddRange(
            new ImportSession
            {
                UserId = "user-a",
                FileName = "mine.csv",
                FileType = "CSV",
                SuccessfulRecords = 4,
                FailedRecords = 1
            },
            new ImportSession
            {
                UserId = "user-b",
                FileName = "theirs.csv",
                FileType = "CSV",
                SuccessfulRecords = 90,
                FailedRecords = 9
            });
        await db.SaveChangesAsync();

        var owned = await db.ImportSessions.Where(i => i.UserId == "user-a").ToListAsync();
        var summary = ImportSessionStats.From(owned);
        Assert.Equal(1, summary.SessionCount);
        Assert.Equal(4, summary.RecordsImported);
        Assert.Equal(1, summary.FailedRecords);
        Assert.DoesNotContain(owned, s => s.UserId != "user-a");
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("defaults", true)]
    [InlineData("DEFAULTS", true)]
    [InlineData("minimal", false)]
    [InlineData(" Minimal ", false)]
    [InlineData("family", true)]
    public void StarterSet_OnlyMinimalSkipsDefaultSeed(string? starterSet, bool shouldSeed)
    {
        Assert.Equal(shouldSeed, EnvelopeStarterSets.ShouldSeedDefaultEnvelopes(starterSet));
    }

    [Fact]
    public async Task StarterSet_DefaultsAndMinimal_Differ()
    {
        var db = CreateDb();
        db.Users.AddRange(
            User("defaults-user", "defaults@test.com"),
            User("minimal-user", "minimal@test.com"));
        await db.SaveChangesAsync();

        var svc = new DefaultCategoryService(db);
        await svc.ApplyStarterSetAsync("defaults-user", EnvelopeStarterSets.Defaults);
        await svc.ApplyStarterSetAsync("minimal-user", EnvelopeStarterSets.Minimal);

        var defaultNames = await db.Categories.Where(c => c.UserId == "defaults-user").Select(c => c.Name).ToListAsync();
        var minimalNames = await db.Categories.Where(c => c.UserId == "minimal-user").Select(c => c.Name).ToListAsync();

        Assert.True(defaultNames.Count > 10);
        Assert.Single(minimalNames);
        Assert.Equal("Uncategorized", minimalNames[0]);
        Assert.DoesNotContain("Rent/Mortgage", minimalNames);
        Assert.Contains("Rent/Mortgage", defaultNames);
    }

    [Fact]
    public async Task StarterSet_DefaultsStillPlantsWhenOnlyUncategorizedExists()
    {
        var db = CreateDb();
        db.Users.Add(User("import-first", "import@test.com"));
        db.Categories.Add(new Category { Name = "Uncategorized", UserId = "import-first" });
        await db.SaveChangesAsync();

        var svc = new DefaultCategoryService(db);
        await svc.ApplyStarterSetAsync("import-first", EnvelopeStarterSets.Defaults);

        var names = await db.Categories.Where(c => c.UserId == "import-first").Select(c => c.Name).ToListAsync();
        Assert.Contains("Uncategorized", names);
        Assert.Contains("Rent/Mortgage", names);
        Assert.True(names.Count > 10);
    }

    [Fact]
    public async Task StarterSet_DoesNotOverwriteExistingEnvelopes()
    {
        var db = CreateDb();
        db.Users.Add(User("keep-user", "keep@test.com"));
        db.Categories.Add(new Category { Name = "Already Mine", UserId = "keep-user" });
        await db.SaveChangesAsync();

        var svc = new DefaultCategoryService(db);
        await svc.ApplyStarterSetAsync("keep-user", EnvelopeStarterSets.Defaults);

        var names = await db.Categories.Where(c => c.UserId == "keep-user").Select(c => c.Name).ToListAsync();
        Assert.Single(names);
        Assert.Equal("Already Mine", names[0]);
    }

    [Fact]
    public async Task StarterSet_UnknownFallsBackToDefaults()
    {
        var db = CreateDb();
        db.Users.Add(User("unknown-user", "unknown@test.com"));
        await db.SaveChangesAsync();

        var svc = new DefaultCategoryService(db);
        await svc.ApplyStarterSetAsync("unknown-user", "not-a-real-set");

        var count = await db.Categories.CountAsync(c => c.UserId == "unknown-user");
        Assert.True(count > 10);
    }

    [Fact]
    public void SampleCsv_HasRequiredHeaders()
    {
        var header = BankImportPresets.SampleCsvContent.Split('\n')[0].Trim();
        Assert.Equal("Date,Description,Amount,Category,Account", header);
        Assert.Contains("Everyday Checking", BankImportPresets.SampleCsvContent);
        Assert.DoesNotContain("Salary Income", BankImportPresets.SampleCsvContent);
        Assert.Contains(BankImportPresets.All, p => p.Key == "generic-csv");
        Assert.Contains(BankImportPresets.All, p => p.Key == "chase");
    }

    private static BudgetDbContext CreateDb()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var db = new BudgetDbContext(new DbContextOptionsBuilder<BudgetDbContext>().UseSqlite(conn).Options);
        db.Database.EnsureCreated();
        return db;
    }

    private static ApplicationUser User(string id, string email) => new()
    {
        Id = id,
        UserName = email,
        Email = email,
        NormalizedUserName = email.ToUpperInvariant(),
        NormalizedEmail = email.ToUpperInvariant(),
        FirstName = "Test",
        LastName = "Banks",
        EmailConfirmed = true
    };
}
