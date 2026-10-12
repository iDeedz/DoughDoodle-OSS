using System.Security.Claims;
using DoughDoodle.Services.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoughDoodle.Controllers;

[Authorize]
public class ForecastController : Controller
{
    private readonly ICashflowForecastService _forecast;

    public ForecastController(ICashflowForecastService forecast) => _forecast = forecast;

    [HttpGet]
    public async Task<IActionResult> Index(int history = 6, int ahead = 3)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");
        var points = await _forecast.ForecastAsync(uid, history, ahead);
        return View(points);
    }
}
