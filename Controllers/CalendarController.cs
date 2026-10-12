using System.Security.Claims;
using DoughDoodle.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Controllers;

[Authorize]
public class CalendarController : Controller
{
    private readonly BudgetDbContext _db;
    public CalendarController(BudgetDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(int? year = null, int? month = null)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");
        var y = year ?? DateTime.UtcNow.Year;
        var m = month ?? DateTime.UtcNow.Month;
        var start = new DateTime(y, m, 1);
        var end = start.AddMonths(1);

        var bills = await _db.Bills.Where(b => b.UserId == uid && b.DueDate >= start && b.DueDate < end).ToListAsync();
        var goals = await _db.FinancialGoals.Where(g => g.UserId == uid && g.TargetDate != null && g.TargetDate >= start && g.TargetDate < end).ToListAsync();
        var recurring = await _db.ScheduledTransactions.Where(s => s.UserId == uid && s.NextDueDate >= start && s.NextDueDate < end).ToListAsync();

        ViewBag.Year = y;
        ViewBag.Month = m;
        ViewBag.Bills = bills;
        ViewBag.Goals = goals;
        ViewBag.Recurring = recurring;
        return View();
    }
}
