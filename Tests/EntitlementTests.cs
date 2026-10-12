using Xunit;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Services.Billing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DoughDoodle.Tests;

public class EntitlementTests
{
    [Fact]
    public void Pocket_IsFreeForeverFloor()
    {
        var opts = Options.Create(new StripeOptions());
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var db = new BudgetDbContext(new DbContextOptionsBuilder<BudgetDbContext>().UseSqlite(conn).Options);
        db.Database.EnsureCreated();
        var svc = new DoughEntitlementService(db, opts);
        var user = new ApplicationUser { DoughTier = DoughTier.Pocket };
        Assert.Equal(DoughTier.Pocket, svc.GetTier(user));
        Assert.True(svc.CanUseCsvImport(DoughTier.Pocket));
        Assert.False(svc.CanUseInvestments(DoughTier.Pocket));
        Assert.False(svc.CanUseAiInsights(DoughTier.Pocket));
        Assert.True(svc.CanUseHouseholdSharing(DoughTier.Bankroll));
        Assert.Equal(10, svc.GetMaxEnvelopes(DoughTier.Pocket));
    }

    [Fact]
    public void Admin_GetsPrime()
    {
        var opts = Options.Create(new StripeOptions());
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var db = new BudgetDbContext(new DbContextOptionsBuilder<BudgetDbContext>().UseSqlite(conn).Options);
        db.Database.EnsureCreated();
        var svc = new DoughEntitlementService(db, opts);
        var user = new ApplicationUser { IsAdmin = true, DoughTier = DoughTier.Pocket };
        Assert.Equal(DoughTier.Prime, svc.GetTier(user));
    }

    [Fact]
    public void PackDisplayNames_AreFintechMoneyNames()
    {
        var opts = Options.Create(new StripeOptions());
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var db = new BudgetDbContext(new DbContextOptionsBuilder<BudgetDbContext>().UseSqlite(conn).Options);
        db.Database.EnsureCreated();
        var svc = new DoughEntitlementService(db, opts);
        Assert.Equal("Pocket", svc.GetPackDisplayName(DoughTier.Pocket));
        Assert.Equal("Bankroll", svc.GetPackDisplayName(DoughTier.Bankroll));
        Assert.Equal("Stack", svc.GetPackDisplayName(DoughTier.Stack));
        Assert.Equal("Vault", svc.GetPackDisplayName(DoughTier.Vault));
        Assert.Equal("Prime", svc.GetPackDisplayName(DoughTier.Prime));
    }
}

