using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Models.ViewModels;
using System.Security.Claims;

namespace DoughDoodle.Controllers;

[Authorize]
public class NetWorthController : Controller
{
    private readonly BudgetDbContext _context;

    public NetWorthController(BudgetDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        var netWorthItems = await _context.NetWorthItems
            .Where(nw => nw.UserId == currentUserId && nw.IsActive)
            .ToListAsync();

        // Sort on client side to avoid SQLite decimal ordering issue
        netWorthItems = netWorthItems.OrderByDescending(nw => nw.CurrentValue).ToList();

        var assets = netWorthItems.Where(nw => nw.IsAsset).ToList();
        var liabilities = netWorthItems.Where(nw => !nw.IsAsset).ToList();

        var totalAssets = assets.Sum(a => a.CurrentValue);
        var totalLiabilities = liabilities.Sum(l => l.CurrentValue);
        var netWorth = totalAssets - totalLiabilities;

        // Get historical data for the last 12 months
        var twelveMonthsAgo = DateTime.Now.AddMonths(-12);
        var historicalData = await _context.NetWorthHistories
            .Where(nwh => nwh.NetWorthItem.UserId == currentUserId && nwh.RecordedAt >= twelveMonthsAgo)
            .Include(nwh => nwh.NetWorthItem)
            .OrderBy(nwh => nwh.RecordedAt)
            .ToListAsync();

        // Calculate monthly net worth progression (client-side processing)
        var monthlyNetWorth = historicalData
            .ToList() // Force client-side evaluation
            .GroupBy(h => new { h.RecordedAt.Year, h.RecordedAt.Month })
            .Select(g => new NetWorthTrendData
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Assets = g.Where(h => h.NetWorthItem.IsAsset).Sum(h => h.Value),
                Liabilities = g.Where(h => !h.NetWorthItem.IsAsset).Sum(h => h.Value),
                NetWorth = g.Where(h => h.NetWorthItem.IsAsset).Sum(h => h.Value) - g.Where(h => !h.NetWorthItem.IsAsset).Sum(h => h.Value)
            })
            .ToList();

        var viewModel = new NetWorthViewModel
        {
            Assets = assets,
            Liabilities = liabilities,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            NetWorth = netWorth,
            MonthlyTrends = monthlyNetWorth,
            TotalItems = netWorthItems.Count,
            AssetCount = assets.Count,
            LiabilityCount = liabilities.Count
        };

        return View(viewModel);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NetWorthItem item)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        if (ModelState.IsValid)
        {
            item.UserId = currentUserId;
            item.CreatedAt = DateTime.UtcNow;
            item.LastUpdated = DateTime.UtcNow;

            _context.NetWorthItems.Add(item);
            await _context.SaveChangesAsync();

            // Create initial history record
            var history = new NetWorthHistory
            {
                NetWorthItemId = item.Id,
                Value = item.CurrentValue,
                RecordedAt = DateTime.UtcNow,
                Notes = "Initial value"
            };

            _context.NetWorthHistories.Add(history);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{(item.IsAsset ? "Asset" : "Liability")} '{item.Name}' added successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(item);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var item = await _context.NetWorthItems
            .FirstOrDefaultAsync(nw => nw.Id == id && nw.UserId == currentUserId);

        if (item == null) return NotFound();

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NetWorthItem item)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id != item.Id) return NotFound();

        var existingItem = await _context.NetWorthItems
            .FirstOrDefaultAsync(nw => nw.Id == id && nw.UserId == currentUserId);
        
        if (existingItem == null) return NotFound();

        if (ModelState.IsValid)
        {
            // Check if value changed to add history record
            bool valueChanged = existingItem.CurrentValue != item.CurrentValue;

            existingItem.Name = item.Name;
            existingItem.Description = item.Description;
            existingItem.IsAsset = item.IsAsset;
            existingItem.AssetType = item.AssetType;
            existingItem.LiabilityType = item.LiabilityType;
            existingItem.Notes = item.Notes;
            existingItem.IsActive = item.IsActive;
            existingItem.LastUpdated = DateTime.UtcNow;

            if (valueChanged)
            {
                existingItem.CurrentValue = item.CurrentValue;
                
                // Add history record for value change
                var history = new NetWorthHistory
                {
                    NetWorthItemId = existingItem.Id,
                    Value = item.CurrentValue,
                    RecordedAt = DateTime.UtcNow,
                    Notes = "Value updated"
                };

                _context.NetWorthHistories.Add(history);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{(existingItem.IsAsset ? "Asset" : "Liability")} updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateValue(int id, decimal newValue, string? notes = null)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var item = await _context.NetWorthItems
            .FirstOrDefaultAsync(nw => nw.Id == id && nw.UserId == currentUserId);

        if (item == null) return NotFound();

        if (item.CurrentValue != newValue)
        {
            item.CurrentValue = newValue;
            item.LastUpdated = DateTime.UtcNow;

            // Add history record
            var history = new NetWorthHistory
            {
                NetWorthItemId = item.Id,
                Value = newValue,
                RecordedAt = DateTime.UtcNow,
                Notes = notes ?? "Value updated"
            };

            _context.NetWorthHistories.Add(history);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{item.Name} value updated successfully!";
        }

        return Json(new { success = true, message = "Value updated successfully!" });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var item = await _context.NetWorthItems
            .FirstOrDefaultAsync(nw => nw.Id == id && nw.UserId == currentUserId);

        if (item == null) return NotFound();

        // Soft delete
        item.IsActive = false;
        item.LastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"{(item.IsAsset ? "Asset" : "Liability")} '{item.Name}' removed successfully!";
        return Json(new { success = true, message = "Item removed successfully!" });
    }

    public async Task<IActionResult> History(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var item = await _context.NetWorthItems
            .FirstOrDefaultAsync(nw => nw.Id == id && nw.UserId == currentUserId);

        if (item == null) return NotFound();

        var history = await _context.NetWorthHistories
            .Where(nwh => nwh.NetWorthItemId == id)
            .OrderByDescending(nwh => nwh.RecordedAt)
            .ToListAsync();

        ViewBag.NetWorthItem = item;
        return View(history);
    }

    [HttpGet]
    public async Task<IActionResult> GetNetWorthChart()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return Unauthorized();

        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        var historicalData = await _context.NetWorthHistories
            .Where(nwh => nwh.NetWorthItem.UserId == currentUserId && nwh.RecordedAt >= sixMonthsAgo)
            .Include(nwh => nwh.NetWorthItem)
            .OrderBy(nwh => nwh.RecordedAt)
            .Select(nwh => new
            {
                Date = nwh.RecordedAt.ToString("yyyy-MM-dd"),
                Value = nwh.Value,
                IsAsset = nwh.NetWorthItem.IsAsset,
                ItemName = nwh.NetWorthItem.Name
            })
            .ToListAsync();

        // Group by month and calculate net worth progression
        var monthlyData = historicalData
            .GroupBy(h => h.Date.Substring(0, 7)) // Group by YYYY-MM
            .Select(g => new
            {
                Month = g.Key,
                Assets = g.Where(h => h.IsAsset).Sum(h => h.Value),
                Liabilities = g.Where(h => !h.IsAsset).Sum(h => h.Value),
                NetWorth = g.Where(h => h.IsAsset).Sum(h => h.Value) - g.Where(h => !h.IsAsset).Sum(h => h.Value)
            })
            .OrderBy(d => d.Month)
            .ToList();

        return Json(monthlyData);
    }
}