using Xunit;
using DoughDoodle.Services.Bank;
using DoughDoodle.Services.Currency;
using DoughDoodle.Services.Security;
using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OtpNet;

namespace DoughDoodle.Tests;

public class RoadmapClearanceTests
{
    [Fact]
    public void OfxParser_ExtractsStmtTrn()
    {
        var svc = new OfxImportService(CreateDb());
        var ofx = @"OFXHEADER:100
DATA:OFXSGML
<OFX><BANKMSGSRSV1><STMTTRNRS><STMTRS><BANKTRANLIST>
<STMTTRN>
<TRNTYPE>DEBIT
<DTPOSTED>20260115
<TRNAMT>-12.34
<FITID>abc123
<NAME>Coffee
</STMTTRN>
<STMTTRN>
<TRNTYPE>CREDIT
<DTPOSTED>20260116
<TRNAMT>100.00
<FITID>abc124
<NAME>Pay
</STMTTRN>
</BANKTRANLIST></STMTRS></STMTTRNRS></BANKMSGSRSV1></OFX>";
        var rows = svc.ParseOfx(ofx);
        Assert.Equal(2, rows.Count);
        Assert.Equal(-12.34m, rows[0].Amount);
        Assert.Equal("Coffee", rows[0].Description);
    }

    [Fact]
    public void Totp_VerifyRoundTrip()
    {
        var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        var code = totp.ComputeTotp();
        // lightweight verify without DI UserManager
        var ok = totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        Assert.True(ok);
    }

    [Fact]
    public async Task Currency_ManualConvert()
    {
        var db = CreateDb();
        db.ExchangeRates.Add(new ExchangeRate { BaseCurrency = "USD", QuoteCurrency = "ZAR", Rate = 18m, Source = "manual" });
        await db.SaveChangesAsync();
        var svc = new CurrencyRateService(db, Options.Create(new FxApiOptions()), new DummyHttpFactory());
        var zar = await svc.ConvertAsync(10m, "USD", "ZAR");
        Assert.Equal(180m, zar);
    }

    [Fact]
    public void ManualBankProvider_AlwaysConfigured()
    {
        var p = new ManualCsvBankSyncProvider();
        Assert.True(p.IsLiveConfigured);
        Assert.Equal(BankSyncProviderKind.ManualCsv, p.Kind);
    }

    private static BudgetDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        var db = new BudgetDbContext(opts);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    private sealed class DummyHttpFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
