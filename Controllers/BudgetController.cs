using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Services;
using System.Security.Claims;

namespace DoughDoodle.Controllers;

[Authorize]
public class BudgetController : Controller
{
    private readonly BudgetDbContext _context;
    private readonly IMoneyTransferService _transferService;
    private readonly IRolloverService _rolloverService;

    public BudgetController(
        BudgetDbContext context,
        IMoneyTransferService transferService,
        IRolloverService rolloverService)
    {
        _context = context;
        _transferService = transferService;
        _rolloverService = rolloverService;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    // GET: Budget/MonthlyWorkflow
    public async Task<IActionResult> MonthlyWorkflow(string? monthYear = null)
    {
        var userId = GetUserId();
        monthYear ??= DateTime.Now.ToString("yyyy-MM");
        
        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .Include(c => c.Budgets.Where(b => b.MonthYear == monthYear))
            .Include(c => c.Transactions.Where(t => t.Date >= DateTime.ParseExact(monthYear + "-01", "yyyy-MM-dd", null)
                && t.Date < DateTime.ParseExact(monthYear + "-01", "yyyy-MM-dd", null).AddMonths(1)))
            .ToListAsync();

        ViewBag.MonthYear = monthYear;
        ViewBag.CanCopyLastMonth = await HasBudgetsForPreviousMonth(userId, monthYear);
        ViewBag.AccountBalance = await GetAccountBalances(userId);
        
        return View(categories);
    }

    // POST: Budget/CopyLastMonthBudgets
    [HttpPost]
    public async Task<IActionResult> CopyLastMonthBudgets(string monthYear)
    {
        var userId = GetUserId();
        var previousMonth = DateTime.ParseExact(monthYear + "-01", "yyyy-MM-dd", null)
            .AddMonths(-1).ToString("yyyy-MM");

        var previousBudgets = await _context.MonthlyBudgets
            .Include(mb => mb.Category)
            .Where(mb => mb.Category.UserId == userId && mb.MonthYear == previousMonth)
            .ToListAsync();

        var newBudgets = previousBudgets.Select(pb => new MonthlyBudget
        {
            CategoryId = pb.CategoryId,
            MonthYear = monthYear,
            Amount = pb.Amount
        }).ToList();

        // Only add budgets that don't already exist for this month
        var existingBudgets = await _context.MonthlyBudgets
            .Where(mb => mb.MonthYear == monthYear && 
                        _context.Categories.Any(c => c.UserId == userId && c.Id == mb.CategoryId))
            .Select(mb => mb.CategoryId)
            .ToListAsync();

        var budgetsToAdd = newBudgets.Where(nb => !existingBudgets.Contains(nb.CategoryId)).ToList();

        _context.MonthlyBudgets.AddRange(budgetsToAdd);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(MonthlyWorkflow), new { monthYear });
    }

    // GET: Budget/Transfer
    public async Task<IActionResult> Transfer(string monthYear)
    {
        var userId = GetUserId();
        
        var categoriesWithSurplus = await _transferService.GetCategoriesWithSurplusAsync(userId, monthYear);
        var allCategories = await _context.Categories
            .Where(c => c.UserId == userId)
            .ToListAsync();

        ViewBag.MonthYear = monthYear;
        ViewBag.CategoriesWithSurplus = categoriesWithSurplus;
        ViewBag.AllCategories = allCategories;
        
        return View();
    }

    // POST: Budget/Transfer
    [HttpPost]
    public async Task<IActionResult> Transfer(int fromCategoryId, int toCategoryId, decimal amount, string monthYear, string? description)
    {
        var userId = GetUserId();
        
        var success = await _transferService.TransferBetweenCategoriesAsync(
            userId, fromCategoryId, toCategoryId, amount, monthYear, description ?? "");

        if (success)
        {
            var currency = DoughDoodle.Helpers.CurrencyDisplay.Normalize((await _context.Users.FindAsync(userId))?.PreferredCurrency);
            TempData["SuccessMessage"] = $"Successfully transferred {DoughDoodle.Helpers.CurrencyDisplay.Format(amount, currency)} between categories.";
        }
        else
        {
            TempData["ErrorMessage"] = "Transfer failed. Please check available amounts and try again.";
        }

        return RedirectToAction(nameof(Transfer), new { monthYear });
    }

    // GET: Budget/CoverOverspend
    public async Task<IActionResult> CoverOverspend(string monthYear)
    {
        var userId = GetUserId();
        var startDate = DateTime.ParseExact(monthYear + "-01", "yyyy-MM-dd", null);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var overspentCategories = await _context.Categories
            .Where(c => c.UserId == userId)
            .Include(c => c.Budgets.Where(b => b.MonthYear == monthYear))
            .Include(c => c.Transactions.Where(t => t.Date >= startDate && t.Date <= endDate))
            .ToListAsync();

        overspentCategories = overspentCategories.Where(c =>
        {
            var budget = c.Budgets.FirstOrDefault()?.Amount ?? 0;
            var spent = c.Transactions.Sum(t => t.Amount);
            return (budget + spent) < 0; // Overspent
        }).ToList();

        var categoriesWithSurplus = await _transferService.GetCategoriesWithSurplusAsync(userId, monthYear);

        ViewBag.MonthYear = monthYear;
        ViewBag.OverspentCategories = overspentCategories;
        ViewBag.CategoriesWithSurplus = categoriesWithSurplus;
        
        return View();
    }

    // POST: Budget/CoverOverspend
    [HttpPost]
    public async Task<IActionResult> CoverOverspend(int overspentCategoryId, int coveringCategoryId, decimal amount, string monthYear)
    {
        var userId = GetUserId();
        
        var success = await _transferService.CoverOverspendFromCategoryAsync(
            userId, overspentCategoryId, coveringCategoryId, amount, monthYear);

        if (success)
        {
            var currency = DoughDoodle.Helpers.CurrencyDisplay.Normalize((await _context.Users.FindAsync(userId))?.PreferredCurrency);
            TempData["SuccessMessage"] = $"Successfully covered {DoughDoodle.Helpers.CurrencyDisplay.Format(amount, currency)} of overspend.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to cover overspend. Please check available amounts.";
        }

        return RedirectToAction(nameof(CoverOverspend), new { monthYear });
    }

    // GET: Budget/RolloverPreview
    public async Task<IActionResult> RolloverPreview(string fromMonth)
    {
        var userId = GetUserId();
        var toMonth = DateTime.ParseExact(fromMonth + "-01", "yyyy-MM-dd", null)
            .AddMonths(1).ToString("yyyy-MM");

        var rollovers = await _rolloverService.GetRolloverPreviewAsync(userId, fromMonth);
        
        ViewBag.FromMonth = fromMonth;
        ViewBag.ToMonth = toMonth;
        
        return View(rollovers);
    }

    // POST: Budget/ApproveRollover
    [HttpPost]
    public async Task<IActionResult> ApproveRollover(string fromMonth)
    {
        var userId = GetUserId();
        var toMonth = DateTime.ParseExact(fromMonth + "-01", "yyyy-MM-dd", null)
            .AddMonths(1).ToString("yyyy-MM");

        var success = await _rolloverService.ApproveRolloverAsync(userId, fromMonth, toMonth);

        if (success)
        {
            TempData["SuccessMessage"] = "Rollover completed successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Rollover failed. Please try again.";
        }

        return RedirectToAction("Index", "Home", new { month = toMonth });
    }

    // GET: Budget/Settings
    public async Task<IActionResult> Settings()
    {
        var userId = GetUserId();
        var settings = await _context.BudgetSettings
            .FirstOrDefaultAsync(bs => bs.UserId == userId);

        if (settings == null)
        {
            settings = new BudgetSettings
            {
                UserId = userId
            };
        }

        return View(settings);
    }

    // POST: Budget/Settings
    [HttpPost]
    public async Task<IActionResult> Settings(BudgetSettings model)
    {
        var userId = GetUserId();
        model.UserId = userId;
        model.UpdatedAt = DateTime.UtcNow;

        var existingSettings = await _context.BudgetSettings
            .FirstOrDefaultAsync(bs => bs.UserId == userId);

        if (existingSettings == null)
        {
            _context.BudgetSettings.Add(model);
        }
        else
        {
            existingSettings.AutoRolloverSurplus = model.AutoRolloverSurplus;
            existingSettings.AutoRolloverOverspend = model.AutoRolloverOverspend;
            existingSettings.AllowCoverOverspendFromOtherCategories = model.AllowCoverOverspendFromOtherCategories;
            existingSettings.DefaultIncomeAllocation = model.DefaultIncomeAllocation;
            existingSettings.AutoHoldIncomeForNextMonth = model.AutoHoldIncomeForNextMonth;
            existingSettings.CopyLastMonthBudgets = model.CopyLastMonthBudgets;
            existingSettings.FillBudgetsFromAccountBalances = model.FillBudgetsFromAccountBalances;
            existingSettings.EnableDarkMode = model.EnableDarkMode;
            existingSettings.ColorTheme = model.ColorTheme;
            existingSettings.MinimalUI = model.MinimalUI;
            existingSettings.UpdatedAt = model.UpdatedAt;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Settings saved successfully!";
        
        return RedirectToAction(nameof(Settings));
    }

    private async Task<bool> HasBudgetsForPreviousMonth(string userId, string currentMonth)
    {
        var previousMonth = DateTime.ParseExact(currentMonth + "-01", "yyyy-MM-dd", null)
            .AddMonths(-1).ToString("yyyy-MM");

        return await _context.MonthlyBudgets
            .AnyAsync(mb => _context.Categories.Any(c => c.UserId == userId && c.Id == mb.CategoryId) 
                           && mb.MonthYear == previousMonth);
    }

    private async Task<decimal> GetAccountBalances(string userId)
    {
        return await _context.Accounts
            .Where(a => a.UserId == userId && a.IsOnBudget && a.IsActive)
            .SumAsync(a => a.Balance);
    }
}