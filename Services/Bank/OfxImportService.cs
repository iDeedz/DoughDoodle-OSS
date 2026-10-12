using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Services.Bank;

public record ParsedImportRow(DateTime Date, decimal Amount, string Description, string? FitId);

public interface IOfxImportService
{
    Task<(int success, int failed)> ImportOfxOrQfxAsync(Stream stream, string fileName, ImportSession session, string userId, int? accountId, CancellationToken ct = default);
    IReadOnlyList<ParsedImportRow> ParseOfx(string content);
}

public class OfxImportService : IOfxImportService
{
    private readonly BudgetDbContext _db;

    public OfxImportService(BudgetDbContext db) => _db = db;

    public IReadOnlyList<ParsedImportRow> ParseOfx(string content)
    {
        // Strip SGML OFX header if present; normalize to XML-ish
        var xmlStart = content.IndexOf("<OFX>", StringComparison.OrdinalIgnoreCase);
        if (xmlStart < 0) xmlStart = content.IndexOf("<ofx>", StringComparison.OrdinalIgnoreCase);
        var body = xmlStart >= 0 ? content[xmlStart..] : content;

        // Close unclosed SGML tags common in OFX 1.x
        body = Regex.Replace(body, @"<(STMTTRN)>(.*?)</\1>", m => m.Value, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        // Fallback: extract STMTTRN blocks with regex
        var rows = new List<ParsedImportRow>();
        var blocks = Regex.Matches(body, @"<STMTTRN>(.*?)</STMTTRN>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (blocks.Count == 0)
        {
            // Try SGML without closing tags
            blocks = Regex.Matches(body, @"<STMTTRN>(.*?)(?=<STMTTRN>|</BANKTRANLIST>|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        foreach (Match block in blocks)
        {
            var chunk = block.Groups[1].Value;
            var dt = ExtractTag(chunk, "DTPOSTED");
            var amt = ExtractTag(chunk, "TRNAMT");
            var name = ExtractTag(chunk, "NAME") ?? ExtractTag(chunk, "MEMO") ?? "Imported";
            var fit = ExtractTag(chunk, "FITID");
            if (string.IsNullOrWhiteSpace(dt) || string.IsNullOrWhiteSpace(amt)) continue;
            if (!TryParseOfxDate(dt, out var date)) continue;
            if (!decimal.TryParse(amt, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount)) continue;
            rows.Add(new ParsedImportRow(date, amount, name.Trim(), fit));
        }

        return rows;
    }

    public async Task<(int success, int failed)> ImportOfxOrQfxAsync(Stream stream, string fileName, ImportSession session, string userId, int? accountId, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(ct);
        var rows = ParseOfx(content);
        session.TotalRecords = rows.Count;
        await _db.SaveChangesAsync(ct);

        var account = accountId.HasValue
            ? await _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, ct)
            : await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId && a.IsActive, ct);

        int ok = 0, fail = 0;
        foreach (var row in rows)
        {
            try
            {
                if (!string.IsNullOrEmpty(row.FitId))
                {
                    var dup = await _db.Transactions.AnyAsync(t => t.ImportId == row.FitId && t.AccountId == account!.Id, ct);
                    if (dup) { fail++; continue; }
                }

                var tx = new Transaction
                {
                    Description = row.Description,
                    Amount = Math.Abs(row.Amount),
                    Date = row.Date,
                    TransactionType = row.Amount < 0 ? TransactionType.Expense : TransactionType.Income,
                    AccountId = account?.Id,
                    ImportId = row.FitId ?? Guid.NewGuid().ToString("N"),
                    ImportSource = Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Transactions.Add(tx);
                ok++;
            }
            catch
            {
                fail++;
            }
        }

        session.SuccessfulRecords = ok;
        session.FailedRecords = fail;
        session.ProcessedRecords = ok + fail;
        await _db.SaveChangesAsync(ct);
        return (ok, fail);
    }

    private static string? ExtractTag(string chunk, string tag)
    {
        var m = Regex.Match(chunk, $"<{tag}>([^<\r\n]+)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static bool TryParseOfxDate(string raw, out DateTime date)
    {
        raw = raw.Trim();
        // YYYYMMDD or YYYYMMDDHHMMSS
        if (raw.Length >= 8 &&
            DateTime.TryParseExact(raw[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date))
            return true;
        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date);
    }
}
