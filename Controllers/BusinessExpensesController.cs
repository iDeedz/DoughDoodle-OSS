using System.Security.Claims;
using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Controllers;

[Authorize]
public class BusinessExpensesController : Controller
{
    private readonly BudgetDbContext _db;
    public BusinessExpensesController(BudgetDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");
        var projects = await _db.ExpenseProjects.Where(p => p.UserId == uid).OrderBy(p => p.Name).ToListAsync();
        var projectIds = projects.Select(p => p.Id).ToList();
        var spend = await _db.Transactions
            .Where(t => t.ExpenseProjectId != null && projectIds.Contains(t.ExpenseProjectId.Value) && t.TransactionType == TransactionType.Expense)
            .GroupBy(t => t.ExpenseProjectId!.Value)
            .Select(g => new { ProjectId = g.Key, Total = g.Sum(x => Math.Abs(x.Amount)) })
            .ToListAsync();
        ViewBag.Spend = spend.ToDictionary(x => x.ProjectId, x => x.Total);
        return View(projects);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Name required."; return RedirectToAction(nameof(Index)); }
        _db.ExpenseProjects.Add(new ExpenseProject { UserId = uid, Name = name.Trim(), Description = description, IsBusiness = true });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Project created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Report(int id)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");
        var project = await _db.ExpenseProjects.FirstOrDefaultAsync(p => p.Id == id && p.UserId == uid);
        if (project == null) return NotFound();
        var txs = await _db.Transactions.Where(t => t.ExpenseProjectId == id).OrderByDescending(t => t.Date).ToListAsync();
        ViewBag.Project = project;
        return View(txs);
    }
}
