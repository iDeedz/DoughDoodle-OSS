using System.Security.Claims;
using DoughDoodle.Services.Currency;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoughDoodle.Controllers;

[Authorize]
public class CurrencyController : Controller
{
    private readonly ICurrencyRateService _rates;
    public CurrencyController(ICurrencyRateService rates) => _rates = rates;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rates = await _rates.ListAsync();
        return View(rates);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upsert(string baseCurrency, string quoteCurrency, decimal rate)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");
        if (rate <= 0) { TempData["Error"] = "Rate must be positive."; return RedirectToAction(nameof(Index)); }
        await _rates.UpsertManualAsync(baseCurrency, quoteCurrency, rate, uid);
        TempData["Success"] = "Rate saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshApi()
    {
        var ok = await _rates.TryRefreshFromApiAsync();
        TempData[ok ? "Success" : "Error"] = ok
            ? "Rates refreshed from API."
            : "FX API key not configured or request failed. Use manual rates.";
        return RedirectToAction(nameof(Index));
    }
}
