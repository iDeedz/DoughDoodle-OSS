using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Controllers;

[Authorize]
public class ReportBuilderController : Controller
{
    private readonly BudgetDbContext _db;
    public ReportBuilderController(BudgetDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");
        var saved = await _db.SavedReports.Where(r => r.UserId == uid).OrderByDescending(r => r.LastGeneratedAt).ToListAsync();
        return View(saved);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(string name, DateTime startDate, DateTime endDate, bool includeIncome, bool includeExpense, bool includeByCategory, string? saveAs)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");

        var accountIds = await _db.Accounts.Where(a => a.UserId == uid).Select(a => a.Id).ToListAsync();
        var catIds = await _db.Categories.Where(c => c.UserId == uid).Select(c => c.Id).ToListAsync();
        var txs = await _db.Transactions
            .Include(t => t.Category)
            .Where(t => t.Date >= startDate && t.Date <= endDate &&
                ((t.AccountId != null && accountIds.Contains(t.AccountId.Value)) ||
                 (t.CategoryId != null && catIds.Contains(t.CategoryId.Value))))
            .ToListAsync();

        decimal income = includeIncome ? txs.Where(t => t.TransactionType == TransactionType.Income).Sum(t => Math.Abs(t.Amount)) : 0;
        decimal expense = includeExpense ? txs.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => Math.Abs(t.Amount)) : 0;
        object? byCat = null;
        if (includeByCategory)
        {
            byCat = txs.Where(t => t.TransactionType == TransactionType.Expense && t.Category != null)
                .GroupBy(t => t.Category!.Name)
                .Select(g => new { Category = g.Key, Total = g.Sum(x => Math.Abs(x.Amount)) })
                .OrderByDescending(x => x.Total)
                .ToList();
        }

        var config = JsonSerializer.Serialize(new { startDate, endDate, includeIncome, includeExpense, includeByCategory });
        if (!string.IsNullOrWhiteSpace(saveAs))
        {
            _db.SavedReports.Add(new SavedReport
            {
                Name = saveAs,
                Description = name ?? saveAs,
                ReportType = ReportType.Custom,
                Configuration = config,
                UserId = uid,
                LastGeneratedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        ViewBag.Income = income;
        ViewBag.Expense = expense;
        ViewBag.Net = income - expense;
        ViewBag.ByCategory = byCat;
        ViewBag.Start = startDate;
        ViewBag.End = endDate;
        ViewBag.Name = name;
        return View("Result");
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(DateTime startDate, DateTime endDate)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");
        var accountIds = await _db.Accounts.Where(a => a.UserId == uid).Select(a => a.Id).ToListAsync();
        var catIds = await _db.Categories.Where(c => c.UserId == uid).Select(c => c.Id).ToListAsync();
        var txs = await _db.Transactions
            .Where(t => t.Date >= startDate && t.Date <= endDate &&
                ((t.AccountId != null && accountIds.Contains(t.AccountId.Value)) ||
                 (t.CategoryId != null && catIds.Contains(t.CategoryId.Value))))
            .OrderBy(t => t.Date)
            .ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Date,Description,Amount,Type,CategoryId");
        foreach (var t in txs)
            sb.AppendLine($"{t.Date:yyyy-MM-dd},\"{t.Description.Replace("\"", "\"\"")}\",{t.Amount},{t.TransactionType},{t.CategoryId}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"report-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.csv");
    }
}
