using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Models.ViewModels;
using DoughDoodle.Services;

namespace DoughDoodle.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly BudgetDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISafeToSpendService _safeToSpend;

    public DashboardController(BudgetDbContext context, UserManager<ApplicationUser> userManager, ISafeToSpendService safeToSpend)
    {
        _context = context;
        _userManager = userManager;
        _safeToSpend = safeToSpend;
    }

    public async Task<IActionResult> Index(string? month = null)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }
        if (!currentUser.OnboardingCompleted)
        {
            return RedirectToAction("Index", "Onboarding");
        }

        month ??= DateTime.Now.ToString("yyyy-MM");
        var monthDate = DateTime.ParseExact(month, "yyyy-MM", null);

        var dashboardData = await GetUserDashboardData(currentUser.Id, month);
        return View(dashboardData);
    }

    public async Task<IActionResult> FamilyDashboard()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null || currentUser.UserType != UserType.MainAccount)
        {
            return RedirectToAction("Index");
        }

        var familyDashboard = await GetFamilyDashboardData(currentUser);
        return View(familyDashboard);
    }

    [HttpGet]
    public async Task<IActionResult> GetChartData(string chartType, string period = "6months")
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Json(new { error = "User not found" });
        }

        return chartType.ToLower() switch
        {
            "monthlyspending" => Json(await GetMonthlySpendingData(currentUser.Id, period)),
            "categorybreakdown" => Json(await GetCategoryBreakdownData(currentUser.Id)),
            "budgetvsactual" => Json(await GetBudgetVsActualData(currentUser.Id)),
            "spendingtrend" => Json(await GetSpendingTrendData(currentUser.Id)),
            "incomevsexpenses" => Json(await GetIncomeVsExpensesData(currentUser.Id, period)),
            _ => Json(new { error = "Invalid chart type" })
        };
    }

    private async Task<UserDashboardViewModel> GetUserDashboardData(string userId, string month)
    {
        var monthDate = DateTime.ParseExact(month, "yyyy-MM", null);
        var user = await _context.Users.FindAsync(userId);
        
        // Get user's categories and transactions
        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .Include(c => c.Transactions)
            .Include(c => c.Budgets)
            .ToListAsync();

        var currentMonthTransactions = categories
            .SelectMany(c => c.Transactions)
            .Where(t => t.Date.Year == monthDate.Year && t.Date.Month == monthDate.Month)
            .ToList();

        var currentMonthBudgets = categories
            .SelectMany(c => c.Budgets)
            .Where(b => b.MonthYear == month)
            .ToList();

        // Calculate basic stats
        var budgetStats = CalculateBudgetStats(currentMonthBudgets, currentMonthTransactions, categories);
        var spendingAnalytics = await CalculateSpendingAnalytics(userId, currentMonthTransactions);
        var financialHealth = await CalculateFinancialHealth(userId, categories);

        var safe = await _safeToSpend.CalculateAsync(userId, month, user?.PreferredCurrency);

        var dashboard = new UserDashboardViewModel
        {
            UserId = userId,
            UserName = user?.FullName ?? "User",
            CurrentMonth = month,
            PreferredCurrency = user?.PreferredCurrency ?? "USD",
            BudgetStats = budgetStats,
            SpendingAnalytics = spendingAnalytics,
            FinancialHealth = financialHealth,
            SafeToSpend = safe,
            MonthlySpendingChart = await GetMonthlySpendingData(userId, "6months"),
            CategoryBreakdownChart = await GetCategoryBreakdownData(userId),
            BudgetVsActualChart = await GetBudgetVsActualData(userId),
            SpendingTrendChart = await GetSpendingTrendData(userId),
            IncomeVsExpensesChart = await GetIncomeVsExpensesData(userId, "6months")
        };

        return dashboard;
    }

    private UserBudgetStatsViewModel CalculateBudgetStats(List<MonthlyBudget> budgets, List<Transaction> transactions, List<Category> categories)
    {
        var totalBudgeted = budgets.Sum(b => b.Amount);
        var totalSpent = transactions.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount);
        var totalRemaining = totalBudgeted - totalSpent;
        
        var categoryStats = categories.Select(c => new
        {
            Category = c,
            Budgeted = budgets.Where(b => b.CategoryId == c.Id).Sum(b => b.Amount),
            Spent = transactions.Where(t => t.CategoryId == c.Id && t.TransactionType == TransactionType.Expense).Sum(t => t.Amount)
        }).ToList();

        var overBudgetCategories = categoryStats.Count(cs => cs.Spent > cs.Budgeted && cs.Budgeted > 0);
        var biggestOverspend = categoryStats.Where(cs => cs.Spent > cs.Budgeted).OrderByDescending(cs => cs.Spent - cs.Budgeted).FirstOrDefault();

        return new UserBudgetStatsViewModel
        {
            TotalBudgeted = totalBudgeted,
            TotalSpent = totalSpent,
            TotalRemaining = totalRemaining,
            SpentPercentage = totalBudgeted > 0 ? Math.Round(totalSpent / totalBudgeted * 100, 1) : 0,
            TotalCategories = categories.Count,
            OverBudgetCategories = overBudgetCategories,
            UnderBudgetCategories = categoryStats.Count(cs => cs.Spent < cs.Budgeted && cs.Budgeted > 0),
            BiggestOverspend = biggestOverspend != null ? biggestOverspend.Spent - biggestOverspend.Budgeted : 0,
            BiggestOverspendCategory = biggestOverspend?.Category.Name ?? ""
        };
    }

    private async Task<UserSpendingAnalyticsViewModel> CalculateSpendingAnalytics(string userId, List<Transaction> currentMonthTransactions)
    {
        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        var allTransactions = await _context.Transactions
            .Where(t => t.Category!.UserId == userId && t.Date >= sixMonthsAgo && t.TransactionType == TransactionType.Expense)
            .Include(t => t.Category)
            .ToListAsync();

        var averageMonthlySpending = allTransactions.GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => g.Sum(t => t.Amount))
            .DefaultIfEmpty(0)
            .Average();

        var mostSpentCategory = allTransactions
            .GroupBy(t => t.Category!.Name)
            .OrderByDescending(g => g.Sum(t => t.Amount))
            .FirstOrDefault();

        var daysIntoMonth = DateTime.Now.Day;
        var dailyAvg = currentMonthTransactions.Count > 0 ? currentMonthTransactions.Sum(t => t.Amount) / daysIntoMonth : 0;
        var daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
        var projectedSpending = dailyAvg * daysInMonth;

        return new UserSpendingAnalyticsViewModel
        {
            AverageMonthlySpending = Math.Round(averageMonthlySpending, 2),
            HighestTransactionAmount = allTransactions.Any() ? Math.Round(allTransactions.Max(t => t.Amount), 2) : 0,
            MostSpentCategory = mostSpentCategory?.Key ?? "",
            MostSpentCategoryAmount = Math.Round(mostSpentCategory?.Sum(t => t.Amount) ?? 0, 2),
            TransactionCount = currentMonthTransactions.Count,
            AverageTransactionAmount = currentMonthTransactions.Any() ? Math.Round(currentMonthTransactions.Average(t => t.Amount), 2) : 0,
            DaysIntoMonth = daysIntoMonth,
            DailySpendingAverage = Math.Round(dailyAvg, 2),
            ProjectedMonthlySpending = Math.Round(projectedSpending, 2)
        };
    }

    private async Task<UserFinancialHealthViewModel> CalculateFinancialHealth(string userId, List<Category> categories)
    {
        // This is a simplified health calculation - in a real app you'd have more sophisticated metrics
        var emergencyCategory = categories.FirstOrDefault(c => c.Name.ToLower().Contains("emergency"));
        var emergencyBalance = emergencyCategory != null ? 
            emergencyCategory.Transactions.Sum(t => t.TransactionType == TransactionType.Income ? t.Amount : -t.Amount) : 0;

        // Get all transactions and calculate on client side to avoid SQLite decimal aggregation issues
        var monthlyTransactions = await _context.Transactions
            .Where(t => t.Category!.UserId == userId && t.TransactionType == TransactionType.Expense && 
                       t.Date >= DateTime.Now.AddDays(-30))
            .ToListAsync();
        
        var totalMonthlyExpenses = monthlyTransactions.Sum(t => t.Amount);

        var recommendedEmergencyFund = totalMonthlyExpenses * 6; // 6 months of expenses
        var healthScore = Math.Min(100, emergencyBalance / Math.Max(recommendedEmergencyFund, 1) * 100);

        var insights = new List<string>();
        if (emergencyBalance < recommendedEmergencyFund * 0.1m)
            insights.Add("Consider building an emergency fund");
        if (healthScore > 80)
            insights.Add("Great job maintaining financial stability!");
        
        return new UserFinancialHealthViewModel
        {
            HealthScore = Math.Round(healthScore, 1),
            HealthStatus = healthScore switch
            {
                >= 90 => "Excellent",
                >= 70 => "Good", 
                >= 50 => "Fair",
                _ => "Needs Improvement"
            },
            HealthInsights = insights,
            EmergencyFundBalance = emergencyBalance,
            RecommendedEmergencyFund = recommendedEmergencyFund,
            OnTrackForGoals = healthScore >= 70,
            SavingsRate = totalMonthlyExpenses > 0 ? Math.Round(emergencyBalance / totalMonthlyExpenses * 100, 1) : 0
        };
    }

    private async Task<List<MonthlySpendingData>> GetMonthlySpendingData(string userId, string period)
    {
        var months = period == "12months" ? 12 : 6;
        var data = new List<MonthlySpendingData>();

        for (int i = months - 1; i >= 0; i--)
        {
            var targetDate = DateTime.Now.AddMonths(-i);
            var monthKey = targetDate.ToString("yyyy-MM");
            
            // Get transactions and calculate on client side
            var monthTransactions = await _context.Transactions
                .Where(t => t.Category!.UserId == userId && 
                           t.Date.Year == targetDate.Year && 
                           t.Date.Month == targetDate.Month &&
                           t.TransactionType == TransactionType.Expense)
                .ToListAsync();

            var monthBudgetsQuery = await _context.MonthlyBudgets
                .Where(b => b.Category.UserId == userId && b.MonthYear == monthKey)
                .ToListAsync();

            data.Add(new MonthlySpendingData
            {
                Month = targetDate.ToString("MMM yyyy"),
                Amount = monthTransactions.Sum(t => t.Amount),
                Budget = monthBudgetsQuery.Sum(b => b.Amount),
                TransactionCount = monthTransactions.Count
            });
        }

        return data;
    }

    private async Task<List<CategorySpendingData>> GetCategoryBreakdownData(string userId)
    {
        var currentMonth = DateTime.Now.ToString("yyyy-MM");
        
        // Get all categories for the user
        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .Include(c => c.Transactions)
            .Include(c => c.Budgets)
            .ToListAsync();

        var categoryData = categories.Select(c => new CategorySpendingData
        {
            Category = c.Name,
            Amount = c.Transactions
                .Where(t => t.Date.Year == DateTime.Now.Year && 
                           t.Date.Month == DateTime.Now.Month &&
                           t.TransactionType == TransactionType.Expense)
                .Sum(t => t.Amount),
            Budget = c.Budgets.Where(b => b.MonthYear == currentMonth).Sum(b => b.Amount)
        })
        .Where(cd => cd.Amount > 0)
        .OrderByDescending(cd => cd.Amount)
        .ToList();

        var total = categoryData.Sum(cd => cd.Amount);
        var colors = new[] { "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0", "#9966FF", "#FF9F40", "#FF6384", "#C9CBCF" };

        for (int i = 0; i < categoryData.Count; i++)
        {
            categoryData[i].Percentage = total > 0 ? Math.Round(categoryData[i].Amount / total * 100, 1) : 0;
            categoryData[i].Color = colors[i % colors.Length];
        }

        return categoryData;
    }

    private async Task<List<BudgetVsActualData>> GetBudgetVsActualData(string userId)
    {
        var currentMonth = DateTime.Now.ToString("yyyy-MM");
        
        // Get all categories with their transactions and budgets
        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .Include(c => c.Transactions)
            .Include(c => c.Budgets)
            .ToListAsync();

        return categories.Select(c => new BudgetVsActualData
        {
            Category = c.Name,
            Budgeted = c.Budgets.Where(b => b.MonthYear == currentMonth).Sum(b => b.Amount),
            Actual = c.Transactions
                .Where(t => t.Date.Year == DateTime.Now.Year && 
                           t.Date.Month == DateTime.Now.Month &&
                           t.TransactionType == TransactionType.Expense)
                .Sum(t => t.Amount)
        })
        .Where(bva => bva.Budgeted > 0 || bva.Actual > 0)
        .Select(bva => new BudgetVsActualData
        {
            Category = bva.Category,
            Budgeted = bva.Budgeted,
            Actual = bva.Actual,
            Variance = bva.Budgeted - bva.Actual,
            Status = bva.Actual > bva.Budgeted ? "Over" : 
                    bva.Actual < bva.Budgeted * 0.9m ? "Under" : "OnTrack"
        })
        .ToList();
    }

    private async Task<List<SpendingTrendData>> GetSpendingTrendData(string userId)
    {
        var currentMonth = DateTime.Now;
        var daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
        
        // Get budgets and calculate on client side
        var budgets = await _context.MonthlyBudgets
            .Where(b => b.Category.UserId == userId && b.MonthYear == currentMonth.ToString("yyyy-MM"))
            .ToListAsync();
        var dailyBudget = budgets.Sum(b => b.Amount) / daysInMonth;

        var data = new List<SpendingTrendData>();
        decimal cumulativeSpending = 0;

        for (int day = 1; day <= Math.Min(daysInMonth, currentMonth.Day); day++)
        {
            var targetDate = new DateTime(currentMonth.Year, currentMonth.Month, day);
            
            // Get daily transactions and calculate on client side
            var dailyTransactions = await _context.Transactions
                .Where(t => t.Category!.UserId == userId && 
                           t.Date.Date == targetDate.Date &&
                           t.TransactionType == TransactionType.Expense)
                .ToListAsync();
            
            var dailySpending = dailyTransactions.Sum(t => t.Amount);
            cumulativeSpending += dailySpending;

            data.Add(new SpendingTrendData
            {
                Date = targetDate.ToString("MMM dd"),
                CumulativeSpending = cumulativeSpending,
                BudgetPace = dailyBudget * day,
                ProjectedSpending = cumulativeSpending / day * daysInMonth
            });
        }

        return data;
    }

    private async Task<List<IncomeVsExpensesData>> GetIncomeVsExpensesData(string userId, string period)
    {
        var months = period == "12months" ? 12 : 6;
        var data = new List<IncomeVsExpensesData>();

        for (int i = months - 1; i >= 0; i--)
        {
            var targetDate = DateTime.Now.AddMonths(-i);
            
            // Get transactions and calculate on client side
            var monthIncomeTransactions = await _context.Transactions
                .Where(t => t.Category!.UserId == userId && 
                           t.Date.Year == targetDate.Year && 
                           t.Date.Month == targetDate.Month &&
                           t.TransactionType == TransactionType.Income)
                .ToListAsync();

            var monthExpenseTransactions = await _context.Transactions
                .Where(t => t.Category!.UserId == userId && 
                           t.Date.Year == targetDate.Year && 
                           t.Date.Month == targetDate.Month &&
                           t.TransactionType == TransactionType.Expense)
                .ToListAsync();

            var monthIncome = monthIncomeTransactions.Sum(t => t.Amount);
            var monthExpenses = monthExpenseTransactions.Sum(t => t.Amount);

            data.Add(new IncomeVsExpensesData
            {
                Month = targetDate.ToString("MMM yyyy"),
                Income = monthIncome,
                Expenses = monthExpenses,
                NetAmount = monthIncome - monthExpenses
            });
        }

        return data;
    }

    private async Task<SubUserDashboardViewModel> GetFamilyDashboardData(ApplicationUser parentUser)
    {
        var subUsers = await _context.Users
            .Where(u => u.ParentUserId == parentUser.Id && u.IsActive)
            .Include(u => u.Categories)
                .ThenInclude(c => c.Transactions)
            .Include(u => u.Categories)
                .ThenInclude(c => c.Budgets)
            .ToListAsync();

        var subUserSummaries = subUsers.Select(user => 
        {
            var totalBudget = user.Categories.SelectMany(c => c.Budgets)
                .Where(b => b.MonthYear == DateTime.Now.ToString("yyyy-MM"))
                .Sum(b => b.Amount);
            
            var totalSpent = user.Categories.SelectMany(c => c.Transactions)
                .Where(t => t.Date.Year == DateTime.Now.Year && 
                           t.Date.Month == DateTime.Now.Month &&
                           t.TransactionType == TransactionType.Expense)
                .Sum(t => t.Amount);

            return new SubUserSummary
            {
                User = user,
                TotalBudget = totalBudget,
                TotalSpent = totalSpent,
                Remaining = totalBudget - totalSpent,
                CategoryCount = user.Categories.Count,
                LastActivity = user.Categories.SelectMany(c => c.Transactions).Any() ? 
                    user.Categories.SelectMany(c => c.Transactions).Max(t => t.Date) : DateTime.MinValue,
                HealthStatus = totalBudget > 0 && totalSpent <= totalBudget ? "Good" : "Needs Attention"
            };
        }).ToList();

        var familyOverview = new FamilyFinancialOverview
        {
            TotalFamilyBudget = subUserSummaries.Sum(s => s.TotalBudget),
            TotalFamilySpending = subUserSummaries.Sum(s => s.TotalSpent),
            TotalFamilyRemaining = subUserSummaries.Sum(s => s.Remaining),
            TotalFamilyMembers = subUsers.Count,
            TopSpendingMember = subUserSummaries.OrderByDescending(s => s.TotalSpent).FirstOrDefault()?.User.FullName ?? "",
            TopSpendingAmount = subUserSummaries.Any() ? subUserSummaries.Max(s => s.TotalSpent) : 0,
            FamilyInsights = GenerateFamilyInsights(subUserSummaries)
        };

        var familySpendingData = subUserSummaries.Select((summary, index) => new FamilySpendingData
        {
            Member = summary.User.FirstName,
            Amount = summary.TotalSpent,
            Budget = summary.TotalBudget,
            Color = new[] { "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0", "#9966FF" }[index % 5]
        }).ToList();

        return new SubUserDashboardViewModel
        {
            ParentUser = parentUser,
            SubUserSummaries = subUserSummaries,
            FamilyOverview = familyOverview,
            FamilySpendingChart = familySpendingData
        };
    }

    private List<string> GenerateFamilyInsights(List<SubUserSummary> summaries)
    {
        var insights = new List<string>();
        
        if (summaries.Any(s => s.Remaining < 0))
            insights.Add($"{summaries.Count(s => s.Remaining < 0)} family member(s) are over budget this month");
        
        if (summaries.All(s => s.Remaining >= 0))
            insights.Add("Great job! All family members are staying within budget");

        var mostActiveUser = summaries.OrderByDescending(s => s.LastActivity).FirstOrDefault();
        if (mostActiveUser != null)
            insights.Add($"{mostActiveUser.User.FirstName} has been the most active recently");

        return insights;
    }
}