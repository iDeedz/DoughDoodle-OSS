using System.Security.Claims;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Services.Bank;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Controllers;

[Authorize]
public class BankSyncController : Controller
{
    private readonly BudgetDbContext _db;
    private readonly IBankSyncOrchestrator _orch;

    public BankSyncController(BudgetDbContext db, IBankSyncOrchestrator orch)
    {
        _db = db;
        _orch = orch;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");

        var connections = await _db.BankConnections.Where(c => c.UserId == uid).OrderByDescending(c => c.CreatedAt).ToListAsync();
        ViewBag.Providers = _orch.Providers.Select(p => new
        {
            p.Kind,
            p.DisplayName,
            p.IsLiveConfigured
        }).ToList();
        ViewBag.Accounts = await _db.Accounts.Where(a => a.UserId == uid && a.IsActive).ToListAsync();
        return View(connections);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Connect(string displayName, int? accountId, BankSyncProviderKind provider = BankSyncProviderKind.ManualCsv)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");

        var resolved = _orch.Resolve(provider);
        if (provider == BankSyncProviderKind.Plaid && !resolved.IsLiveConfigured)
        {
            TempData["Error"] = "Connect bank (coming with API keys) — Plaid ClientId/Secret are not configured. Use Manual/CSV.";
            provider = BankSyncProviderKind.ManualCsv;
        }

        _db.BankConnections.Add(new BankConnection
        {
            UserId = uid,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? resolved.DisplayName : displayName,
            Provider = provider,
            LinkedAccountId = accountId,
            InstitutionName = provider == BankSyncProviderKind.Plaid ? "Plaid (pending keys)" : "Manual import"
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = provider == BankSyncProviderKind.Plaid
            ? "Plaid connection recorded — live sync requires configured API keys."
            : "Manual bank connection saved. Import CSV/OFX/QFX from Import/Export.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sync(int id)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");
        var conn = await _db.BankConnections.FirstOrDefaultAsync(c => c.Id == id && c.UserId == uid);
        if (conn == null) return NotFound();
        var result = await _orch.SyncAsync(conn, uid);
        conn.LastSyncedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
