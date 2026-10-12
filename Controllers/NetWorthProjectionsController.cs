using System.Security.Claims;
using DoughDoodle.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Controllers;

[Authorize]
public class NetWorthProjectionsController : Controller
{
    private readonly BudgetDbContext _db;
    public NetWorthProjectionsController(BudgetDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(int months = 12, decimal monthlySavings = 0)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");
        var items = await _db.NetWorthItems.Where(i => i.UserId == uid).ToListAsync();
        var assets = items.Where(i => i.IsAsset).Sum(i => i.CurrentValue);
        var liabilities = items.Where(i => !i.IsAsset).Sum(i => i.CurrentValue);
        var net = assets - liabilities;

        var history = await _db.NetWorthHistories
            .Where(h => items.Select(i => i.Id).Contains(h.NetWorthItemId))
            .OrderBy(h => h.RecordedAt)
            .Take(200)
            .ToListAsync();

        var points = new List<object>();
        var running = net;
        var now = DateTime.UtcNow;
        for (var i = 0; i <= months; i++)
        {
            points.Add(new { Month = now.AddMonths(i).ToString("yyyy-MM"), Projected = Math.Round(running, 2) });
            running += monthlySavings;
        }
        ViewBag.CurrentNet = net;
        ViewBag.Assets = assets;
        ViewBag.Liabilities = liabilities;
        ViewBag.Points = points;
        ViewBag.MonthlySavings = monthlySavings;
        ViewBag.History = history;
        return View();
    }
}
