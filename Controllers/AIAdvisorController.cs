using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Services;
using DoughDoodle.Services.Billing;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DoughDoodle.Controllers;

[Authorize]
public class AIAdvisorController : Controller
{
    private readonly BudgetDbContext _db;
    private readonly IGrokBudgetAdvisorService _grokService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAIProviderManager _aiProviderManager;
    private readonly IDoughEntitlementService _entitlements;

    public AIAdvisorController(BudgetDbContext db, IGrokBudgetAdvisorService grokService, UserManager<ApplicationUser> userManager, IAIProviderManager aiProviderManager, IDoughEntitlementService entitlements)
    {
        _db = db;
        _grokService = grokService;
        _userManager = userManager;
        _aiProviderManager = aiProviderManager;
        _entitlements = entitlements;
    }

    [HttpGet]
    public async Task<IActionResult> GetBudgetRecommendations(string? month = null)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        month ??= DateTime.Now.ToString("yyyy-MM");
        
        var categories = await _db.Categories
            .Where(c => c.UserId == currentUser.Id)
            .Include(c => c.Transactions)
            .Include(c => c.Budgets)
            .ToListAsync();

        var recommendation = await _grokService.GetBudgetRecommendationsAsync(categories, month);
        
        return Json(recommendation);
    }

    [HttpPost]
    public async Task<IActionResult> GetDebtPayoffAdvice([FromBody] DebtPayoffRequest request)
    {
        var recommendation = await _grokService.GetDebtPayoffRecommendationsAsync(request.ExtraMoney, request.Debts);
        return Json(recommendation);
    }

    [HttpPost]
    public async Task<IActionResult> GetSavingsOptimization([FromBody] SavingsOptimizationRequest request)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var categories = await _db.Categories
            .Where(c => c.UserId == currentUser.Id)
            .Include(c => c.Transactions)
            .Include(c => c.Budgets)
            .ToListAsync();

        var recommendation = await _grokService.GetSavingsOptimizationAsync(categories, request.TotalIncome);
        return Json(recommendation);
    }

    [HttpGet]
    public async Task<IActionResult> BudgetInsights()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var currentMonth = DateTime.Now.ToString("yyyy-MM");
        var categories = await _db.Categories
            .Where(c => c.UserId == currentUser.Id)
            .Include(c => c.Transactions)
            .Include(c => c.Budgets)
            .ToListAsync();

        ViewBag.CurrentMonth = currentMonth;
        return View(categories);
    }

    [HttpGet]
    public async Task<IActionResult> DebtManager()
    {
        var u = await _userManager.GetUserAsync(User);
        if (u == null) return RedirectToAction("Login", "Account");
        if (!_entitlements.CanUseAiInsights(_entitlements.GetTier(u)) && !u.IsAdmin)
        {
            TempData["ErrorMessage"] = "AI insights unlock on Stack and above.";
            return RedirectToAction("Index", "Billing");
        }
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> SavingsPlanner()
    {
        var u = await _userManager.GetUserAsync(User);
        if (u == null) return RedirectToAction("Login", "Account");
        if (!_entitlements.CanUseAiInsights(_entitlements.GetTier(u)) && !u.IsAdmin)
        {
            TempData["ErrorMessage"] = "AI insights unlock on Stack and above.";
            return RedirectToAction("Index", "Billing");
        }
        return View();
    }

    // New Creative AI Endpoints
    [HttpPost]
    public async Task<IActionResult> CategorizePendingTransactions([FromBody] TransactionCategorizationRequest request)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var categories = await _db.Categories
            .Where(c => c.UserId == currentUser.Id)
            .ToListAsync();

        var result = await _grokService.CategorizePendingTransactionsAsync(request.PendingTransactions, categories);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetBudgetForecast(int months = 3)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var categories = await _db.Categories
            .Where(c => c.UserId == currentUser.Id)
            .Include(c => c.Transactions)
            .Include(c => c.Budgets)
            .ToListAsync();

        var historicalTransactions = await _db.Transactions
            .Where(t => t.Category!.UserId == currentUser.Id && t.Date >= DateTime.Now.AddMonths(-12))
            .Include(t => t.Category)
            .ToListAsync();

        var result = await _grokService.GenerateBudgetForecastAsync(categories, historicalTransactions, months);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> AnalyzeSpendingPatterns()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var transactions = await _db.Transactions
            .Where(t => t.Category!.UserId == currentUser.Id && t.Date >= DateTime.Now.AddMonths(-6))
            .Include(t => t.Category)
            .ToListAsync();

        var categories = await _db.Categories
            .Where(c => c.UserId == currentUser.Id)
            .ToListAsync();

        var result = await _grokService.AnalyzeSpendingPatternsAsync(transactions, categories);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetPersonalizedCoaching()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var recentTransactions = await _db.Transactions
            .Where(t => t.Category!.UserId == currentUser.Id && t.Date >= DateTime.Now.AddDays(-30))
            .Include(t => t.Category)
            .ToListAsync();

        var goals = await _db.FinancialGoals
            .Where(g => g.UserId == currentUser.Id && g.IsActive)
            .ToListAsync();

        var result = await _grokService.GetPersonalizedCoachingAsync(currentUser.Id, recentTransactions, goals);
        return Json(result);
    }

    [HttpGet]
    public IActionResult SmartInsights()
    {
        return View();
    }

    [HttpGet]
    public IActionResult BudgetForecasting()
    {
        return View();
    }

    [HttpGet]
    public IActionResult GetAIProviderStatus()
    {
        var availableProviders = _aiProviderManager.GetAvailableProviders();
        var activeProvider = _aiProviderManager.GetActiveProvider();
        
        var status = new
        {
            ActiveProvider = activeProvider?.Name ?? "None",
            AvailableProviders = availableProviders.Select(p => new
            {
                Name = p.Name,
                Priority = p.Priority,
                IsConfigured = p.IsConfigured,
                IsAvailable = p.IsAvailable
            }).ToList(),
            TotalAvailable = availableProviders.Count
        };
        
        return Json(status);
    }

    // GET: AIAdvisor/TaxOptimization
    [HttpGet]
    public IActionResult TaxOptimization()
    {
        return View();
    }

    // GET: AIAdvisor/GetTaxOptimization
    [HttpGet]
    public async Task<IActionResult> GetTaxOptimization(decimal? annualIncome)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        if (!annualIncome.HasValue || annualIncome <= 0)
        {
            return BadRequest("Annual income is required");
        }

        var transactions = await _db.Transactions
            .Where(t => t.Category!.UserId == currentUser.Id && t.Date >= DateTime.Now.AddYears(-1))
            .Include(t => t.Category)
            .ToListAsync();

        var categories = await _db.Categories
            .Where(c => c.UserId == currentUser.Id)
            .ToListAsync();

        var result = await _grokService.GetTaxOptimizationSuggestionsAsync(currentUser.Id, transactions, categories, annualIncome.Value);
        return Json(result);
    }
}

public class DebtPayoffRequest
{
    public decimal ExtraMoney { get; set; }
    public List<DebtInfo> Debts { get; set; } = new();
}

public class SavingsOptimizationRequest
{
    public decimal TotalIncome { get; set; }
}

public class TransactionCategorizationRequest
{
    public List<PendingTransaction> PendingTransactions { get; set; } = new();
}