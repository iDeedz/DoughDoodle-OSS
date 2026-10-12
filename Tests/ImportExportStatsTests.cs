using Xunit;
using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Tests;

public class ImportExportStatsTests
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

    private static async Task AddUserAsync(BudgetDbContext db, string id, string email)
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
    }

    [Fact]
    public async Task ImportSessions_EmptyUser_AreZero()
    {
        await using var db = CreateDb();
        await AddUserAsync(db, "u1", "a@t.com");
        var q = db.ImportSessions.Where(i => i.UserId == "u1");
        Assert.Equal(0, await q.CountAsync());
        Assert.Equal(0, await q.SumAsync(i => (int?)i.SuccessfulRecords) ?? 0);
    }

    [Fact]
    public async Task ImportSessions_AggregatesIgnoreOtherUsers()
    {
        await using var db = CreateDb();
        await AddUserAsync(db, "u1", "a@t.com");
        await AddUserAsync(db, "u2", "b@t.com");
        db.ImportSessions.AddRange(
            new ImportSession
            {
                UserId = "u1",
                FileName = "mine.csv",
                FileType = "CSV",
                Status = ImportStatus.Completed,
                SuccessfulRecords = 10,
                FailedRecords = 2,
                CreatedAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ImportSession
            {
                UserId = "u1",
                FileName = "mine2.csv",
                FileType = "CSV",
                Status = ImportStatus.Completed,
                SuccessfulRecords = 5,
                FailedRecords = 1,
                CreatedAt = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc)
            },
            new ImportSession
            {
                UserId = "u2",
                FileName = "theirs.csv",
                FileType = "CSV",
                Status = ImportStatus.Completed,
                SuccessfulRecords = 999,
                FailedRecords = 50,
                CreatedAt = new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc)
            }
        );
        await db.SaveChangesAsync();

        var q = db.ImportSessions.Where(i => i.UserId == "u1");
        Assert.Equal(2, await q.CountAsync());
        Assert.Equal(15, await q.SumAsync(i => i.SuccessfulRecords));
        Assert.Equal(3, await q.SumAsync(i => i.FailedRecords));
        var last = await q.OrderByDescending(i => i.CreatedAt).Select(i => i.CreatedAt).FirstAsync();
        Assert.Equal(new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc), last);
    }
}
