using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Controllers;

[Authorize]
public class OnboardingController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly BudgetDbContext _db;
    private readonly IDefaultCategoryService _defaults;

    public OnboardingController(
        UserManager<ApplicationUser> userManager,
        BudgetDbContext db,
        IDefaultCategoryService defaults)
    {
        _userManager = userManager;
        _db = db;
        _defaults = defaults;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        if (user.OnboardingCompleted)
            return RedirectToAction("Index", "Dashboard");

        var envelopeCount = await _db.Categories.CountAsync(c => c.UserId == user.Id);
        var accountCount = await _db.Accounts.CountAsync(a => a.UserId == user.Id);
        ViewBag.EnvelopeCount = envelopeCount;
        ViewBag.AccountCount = accountCount;
        ViewBag.FirstName = user.FirstName;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickStart(string? starterSet, decimal? monthlyIncome, string? currency)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (!string.IsNullOrWhiteSpace(currency) && currency.Length == 3)
            user.PreferredCurrency = currency.ToUpperInvariant();

        var hasCategories = await _db.Categories.AnyAsync(c => c.UserId == user.Id);
        if (!hasCategories && EnvelopeStarterSets.ShouldSeedDefaultEnvelopes(starterSet))
        {
            await _defaults.CreateDefaultCategoriesAsync(user.Id);
        }

        if (monthlyIncome is > 0)
        {
            var month = DateTime.Today.ToString("yyyy-MM");
            var existing = await _db.MonthlyIncomes.FirstOrDefaultAsync(m => m.UserId == user.Id && m.MonthYear == month);
            if (existing == null)
            {
                _db.MonthlyIncomes.Add(new MonthlyIncome
                {
                    UserId = user.Id,
                    MonthYear = month,
                    Amount = monthlyIncome.Value
                });
            }
        }

        // Ensure at least one checking account
        if (!await _db.Accounts.AnyAsync(a => a.UserId == user.Id))
        {
            _db.Accounts.Add(new Account
            {
                UserId = user.Id,
                Name = "Everyday Checking",
                AccountType = AccountType.Checking,
                Balance = 0m,
                IsActive = true
            });
        }

        user.OnboardingCompleted = true;
        user.OnboardingCompletedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "You're set — assign every dollar, then check Dough left today.";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Skip()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        user.OnboardingCompleted = true;
        user.OnboardingCompletedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        return RedirectToAction("Index", "Dashboard");
    }
}
