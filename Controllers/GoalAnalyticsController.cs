using System.Security.Claims;
using DoughDoodle.Services.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoughDoodle.Controllers;

[Authorize]
public class GoalAnalyticsController : Controller
{
    private readonly IGoalAnalyticsService _analytics;
    public GoalAnalyticsController(IGoalAnalyticsService analytics) => _analytics = analytics;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");
        var data = await _analytics.AnalyzeAsync(uid);
        return View(data);
    }
}
