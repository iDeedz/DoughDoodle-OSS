namespace DoughDoodle.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

[Authorize]
public class ReportsController : Controller
{
  private readonly BudgetDbContext _context;
  public ReportsController(BudgetDbContext db) { _context = db; }
  public async Task<IActionResult> Index()
  {
    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (currentUserId == null) return RedirectToAction("Login", "Account");

    var userTransactions = await _context.Transactions
        .Include(t => t.Category)
        .Where(t => t.Category!.UserId == currentUserId)
        .ToListAsync();

    var groups = userTransactions
        .GroupBy(t => new { t.Date.Year, t.Date.Month })
        .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(t => t.Amount) })
        .ToList();

    var monthlyTotals = groups.Select(g => new { Month = $"{g.Year:D4}-{g.Month:D2}", Total = g.Total }).ToList();

    var categorySpent = userTransactions
        .GroupBy(t => t.Category!.Name)
        .Select(g => new { Category = g.Key, Spent = g.Sum(t => t.Amount) })
        .ToList();
    
    var prefUser = await _context.Users.FindAsync(currentUserId);
    ViewBag.MonthlyTotalsJson = JsonSerializer.Serialize(monthlyTotals);
    ViewBag.CategorySpentJson = JsonSerializer.Serialize(categorySpent);
    ViewBag.PreferredCurrency = string.IsNullOrWhiteSpace(prefUser?.PreferredCurrency)
        ? "USD"
        : prefUser!.PreferredCurrency;
    return View();
  }

  public async Task<IActionResult> TrendAnalysis()
  {
    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (currentUserId == null) return RedirectToAction("Login", "Account");

    var userTransactions = await _context.Transactions
        .Include(t => t.Category)
        .Where(t => t.Category!.UserId == currentUserId)
        .OrderBy(t => t.Date)
        .ToListAsync();

    // Monthly trends for the last 12 months
    var monthlyTrends = userTransactions
        .Where(t => t.Date >= DateTime.Now.AddMonths(-11))
        .GroupBy(t => new { t.Date.Year, t.Date.Month })
        .Select(g => new MonthlyTrendItem
        {
            Month = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
            MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
            TotalSpending = g.Sum(t => t.Amount),
            TransactionCount = g.Count(),
            AverageTransaction = g.Average(t => t.Amount),
            CategoriesUsed = g.Select(t => t.Category!.Name).Distinct().Count()
        })
        .OrderBy(x => x.Month)
        .ToList();

    // Category trends (top 5 categories)
    var topCategories = userTransactions
        .GroupBy(t => t.Category!.Name)
        .OrderByDescending(g => g.Sum(t => t.Amount))
        .Take(5)
        .Select(g => g.Key)
        .ToList();

    var categoryTrends = new Dictionary<string, List<MonthlyTrendItem>>();
    foreach (var category in topCategories)
    {
        var categoryMonthlyData = userTransactions
            .Where(t => t.Category!.Name == category && t.Date >= DateTime.Now.AddMonths(-11))
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new MonthlyTrendItem
            {
                Month = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                TotalSpending = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                AverageTransaction = g.Average(t => t.Amount)
            })
            .OrderBy(x => x.Month)
            .ToList();
        
        categoryTrends[category] = categoryMonthlyData;
    }

    ViewBag.MonthlyTrendsJson = JsonSerializer.Serialize(monthlyTrends);
    ViewBag.CategoryTrendsJson = JsonSerializer.Serialize(categoryTrends);
    
    return View(new TrendAnalysisViewModel
    {
        MonthlyTrends = monthlyTrends,
        CategoryTrends = categoryTrends
    });
  }

  public async Task<IActionResult> UserReport()
  {
    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (currentUserId == null) return RedirectToAction("Login", "Account");

    var user = await _context.Users.FindAsync(currentUserId);
    if (user == null) return NotFound();

    var userTransactions = await _context.Transactions
        .Include(t => t.Category)
        .Where(t => t.Category!.UserId == currentUserId)
        .ToListAsync();

    var totalSpent = userTransactions.Sum(t => t.Amount);
    var totalTransactions = userTransactions.Count;
    var totalCategories = userTransactions.Select(t => t.Category!.Id).Distinct().Count();

    // Calculate monthly averages
    var monthlyGroups = userTransactions.GroupBy(t => new { t.Date.Year, t.Date.Month });
    var averageMonthlySpending = monthlyGroups.Any() ? monthlyGroups.Average(g => g.Sum(t => t.Amount)) : 0;
    var highestMonthlySpending = monthlyGroups.Any() ? monthlyGroups.Max(g => g.Sum(t => t.Amount)) : 0;

    // Calculate account age
    var accountAge = DateTime.UtcNow - user.CreatedAt;
    var accountAgeText = accountAge.Days > 365 
        ? $"{accountAge.Days / 365} year(s)" 
        : $"{accountAge.Days} day(s)";

    var goals = await _context.FinancialGoals
        .Where(g => g.UserId == currentUserId)
        .ToListAsync();
    var activeGoals = goals.Count(g => g.IsActive && !g.IsCompleted);
    var completedGoals = goals.Count(g => g.IsCompleted);

    // Add monthly spending data for chart
    var monthlySpendingData = userTransactions
        .GroupBy(t => new { t.Date.Year, t.Date.Month })
        .Select(g => new { Month = $"{g.Key.Year:D4}-{g.Key.Month:D2}", Amount = g.Sum(t => t.Amount) })
        .OrderBy(x => x.Month)
        .ToList();

    ViewBag.MonthlySpendingJson = JsonSerializer.Serialize(monthlySpendingData);

    return View(new UserReportViewModel
    {
        User = user,
        TotalSpent = totalSpent,
        TotalTransactions = totalTransactions,
        AverageMonthlySpending = averageMonthlySpending,
        HighestMonthlySpending = highestMonthlySpending,
        TotalCategories = totalCategories,
        ActiveGoals = activeGoals,
        CompletedGoals = completedGoals,
        AccountAgeText = accountAgeText,
        AccountAge = accountAge
    });
  }

  public async Task<IActionResult> MonthlyReport(string? month = null)
  {
    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (currentUserId == null) return RedirectToAction("Login", "Account");

    // Use current month if none specified
    var targetDate = string.IsNullOrEmpty(month) 
        ? DateTime.Now 
        : DateTime.TryParseExact(month, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var parsed) 
            ? parsed 
            : DateTime.Now;

    var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

    // Get all categories and their transactions for the month
    var categories = await _context.Categories
        .Where(c => c.UserId == currentUserId)
        .Include(c => c.Transactions.Where(t => t.Date >= startOfMonth && t.Date <= endOfMonth))
        .ToListAsync();

    var monthlyBudgets = await _context.MonthlyBudgets
        .Where(mb => mb.MonthYear == targetDate.ToString("yyyy-MM") && 
                     categories.Select(c => c.Id).Contains(mb.CategoryId))
        .ToListAsync();

    var reportItems = categories.Select(category =>
    {
        var budget = monthlyBudgets.FirstOrDefault(mb => mb.CategoryId == category.Id);
        var transactions = category.Transactions.Where(t => t.Date >= startOfMonth && t.Date <= endOfMonth).ToList();
        var actualAmount = transactions.Sum(t => t.Amount);

        return new MonthlyReportItem
        {
            CategoryName = category.Name,
            BudgetedAmount = budget?.Amount ?? 0,
            ActualAmount = actualAmount,
            TransactionCount = transactions.Count,
            Transactions = transactions
        };
    }).ToList();

    // Available months for dropdown
    var availableMonths = await _context.Transactions
        .Include(t => t.Category)
        .Where(t => t.Category!.UserId == currentUserId)
        .GroupBy(t => new { t.Date.Year, t.Date.Month })
        .Select(g => $"{g.Key.Year:D4}-{g.Key.Month:D2}")
        .Distinct()
        .OrderByDescending(x => x)
        .ToListAsync();

    ViewBag.AvailableMonths = availableMonths;
    ViewBag.SelectedMonth = targetDate.ToString("yyyy-MM");
    ViewBag.SelectedMonthName = targetDate.ToString("MMMM yyyy");

    return View(reportItems);
  }

  public async Task<IActionResult> CategoryAnalysis()
  {
    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (currentUserId == null) return RedirectToAction("Login", "Account");

    var userTransactions = await _context.Transactions
        .Include(t => t.Category)
        .Where(t => t.Category!.UserId == currentUserId)
        .ToListAsync();

    var categoryAnalysis = userTransactions
        .GroupBy(t => t.Category!)
        .Select(g => new CategoryAnalysisItem
        {
            CategoryName = g.Key.Name,
            TotalSpent = g.Sum(t => t.Amount),
            AverageTransaction = g.Average(t => t.Amount),
            LargestTransaction = g.Max(t => t.Amount),
            TransactionCount = g.Count(),
            MonthlyAverage = g.GroupBy(t => new { t.Date.Year, t.Date.Month })
                             .Average(mg => mg.Sum(t => t.Amount)),
            LastTransaction = g.Max(t => t.Date)
        })
        .OrderByDescending(x => x.TotalSpent)
        .ToList();

    return View(categoryAnalysis);
  }

  public async Task<IActionResult> CashFlow(int months = 6)
  {
    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (currentUserId == null) return RedirectToAction("Login", "Account");

    months = Math.Clamp(months, 3, 24);
    var start = DateTime.Today.AddMonths(-(months - 1));
    start = new DateTime(start.Year, start.Month, 1);

    var transactions = await _context.Transactions
        .Include(t => t.Category)
        .Where(t => t.Category!.UserId == currentUserId && t.Date >= start)
        .ToListAsync();

    var incomes = await _context.MonthlyIncomes
        .Where(m => m.UserId == currentUserId)
        .ToListAsync();

    var rows = Enumerable.Range(0, months).Select(i =>
    {
        var d = start.AddMonths(i);
        var key = d.ToString("yyyy-MM");
        var monthTx = transactions.Where(t => t.Date.Year == d.Year && t.Date.Month == d.Month).ToList();
        var expense = monthTx.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount);
        var incomeFromTx = monthTx.Where(t => t.TransactionType == TransactionType.Income).Sum(t => t.Amount);
        var incomeLogged = incomes.FirstOrDefault(m => m.MonthYear == key)?.Amount ?? 0m;
        var income = incomeFromTx > 0 ? incomeFromTx : incomeLogged;
        return new
        {
            Month = key,
            Label = d.ToString("MMM yyyy"),
            Income = income,
            Expenses = expense,
            Net = income - expense
        };
    }).ToList();

    ViewBag.CashFlowJson = System.Text.Json.JsonSerializer.Serialize(rows);
    ViewBag.Months = months;
    ViewBag.TotalIncome = rows.Sum(r => (decimal)r.Income);
    ViewBag.TotalExpenses = rows.Sum(r => (decimal)r.Expenses);
    ViewBag.Net = rows.Sum(r => (decimal)r.Net);
    return View(rows);
  }
}
