using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DoughDoodle.Services.Currency;

public class FxApiOptions
{
    public const string SectionName = "FxApi";
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api.exchangerate.host";
}

public interface ICurrencyRateService
{
    Task<decimal> ConvertAsync(decimal amount, string from, string to, CancellationToken ct = default);
    Task<IReadOnlyList<ExchangeRate>> ListAsync(CancellationToken ct = default);
    Task UpsertManualAsync(string baseCcy, string quoteCcy, decimal rate, string? userId, CancellationToken ct = default);
    Task<bool> TryRefreshFromApiAsync(CancellationToken ct = default);
}

public class CurrencyRateService : ICurrencyRateService
{
    private readonly BudgetDbContext _db;
    private readonly FxApiOptions _opts;
    private readonly IHttpClientFactory _http;

    public CurrencyRateService(BudgetDbContext db, IOptions<FxApiOptions> opts, IHttpClientFactory http)
    {
        _db = db;
        _opts = opts.Value;
        _http = http;
    }

    public async Task<IReadOnlyList<ExchangeRate>> ListAsync(CancellationToken ct = default)
        => await _db.ExchangeRates.OrderByDescending(r => r.AsOfUtc).Take(100).ToListAsync(ct);

    public async Task UpsertManualAsync(string baseCcy, string quoteCcy, decimal rate, string? userId, CancellationToken ct = default)
    {
        baseCcy = baseCcy.ToUpperInvariant();
        quoteCcy = quoteCcy.ToUpperInvariant();
        var existing = await _db.ExchangeRates
            .FirstOrDefaultAsync(r => r.BaseCurrency == baseCcy && r.QuoteCurrency == quoteCcy && r.Source == "manual", ct);
        if (existing == null)
        {
            _db.ExchangeRates.Add(new ExchangeRate
            {
                BaseCurrency = baseCcy,
                QuoteCurrency = quoteCcy,
                Rate = rate,
                Source = "manual",
                UpdatedByUserId = userId,
                AsOfUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.Rate = rate;
            existing.AsOfUtc = DateTime.UtcNow;
            existing.UpdatedByUserId = userId;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<decimal> ConvertAsync(decimal amount, string from, string to, CancellationToken ct = default)
    {
        from = from.ToUpperInvariant();
        to = to.ToUpperInvariant();
        if (from == to) return amount;
        var rate = await _db.ExchangeRates
            .Where(r => r.BaseCurrency == from && r.QuoteCurrency == to)
            .OrderByDescending(r => r.AsOfUtc)
            .Select(r => (decimal?)r.Rate)
            .FirstOrDefaultAsync(ct);
        if (rate == null)
        {
            var inverse = await _db.ExchangeRates
                .Where(r => r.BaseCurrency == to && r.QuoteCurrency == from)
                .OrderByDescending(r => r.AsOfUtc)
                .Select(r => (decimal?)r.Rate)
                .FirstOrDefaultAsync(ct);
            if (inverse == null || inverse == 0) return amount;
            rate = 1m / inverse.Value;
        }
        return Math.Round(amount * rate.Value, 2);
    }

    public async Task<bool> TryRefreshFromApiAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opts.ApiKey)) return false;
        // Honest: only call when key present; generic exchangerate.host style
        try
        {
            var client = _http.CreateClient();
            var url = $"{_opts.BaseUrl.TrimEnd('/')}/latest?base=USD&access_key={Uri.EscapeDataString(_opts.ApiKey)}";
            using var res = await client.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return false;
            // Minimal parse — store a few common pairs if JSON contains rates
            var json = await res.Content.ReadAsStringAsync(ct);
            if (!json.Contains("rates", StringComparison.OrdinalIgnoreCase)) return false;
            // Without pulling a JSON package dependency beyond System.Text.Json
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("rates", out var rates)) return false;
            foreach (var ccy in new[] { "EUR", "GBP", "ZAR", "CAD", "AUD" })
            {
                if (rates.TryGetProperty(ccy, out var v) && v.TryGetDecimal(out var rate))
                    await UpsertApiRate("USD", ccy, rate, ct);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task UpsertApiRate(string from, string to, decimal rate, CancellationToken ct)
    {
        _db.ExchangeRates.Add(new ExchangeRate
        {
            BaseCurrency = from,
            QuoteCurrency = to,
            Rate = rate,
            Source = "api",
            AsOfUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
