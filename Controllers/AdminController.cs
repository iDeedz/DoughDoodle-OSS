using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Models.ViewModels;
using System.Text.Json;

namespace DoughDoodle.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly BudgetDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(BudgetDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Check if current user is admin
    private async Task<bool> IsCurrentUserAdmin()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.IsAdmin == true;
    }

    // Admin dashboard
    public async Task<IActionResult> Dashboard()
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        // Get all users first to avoid EF translation issues with computed properties
        var allUsers = await _context.Users
            .Include(u => u.SubAccounts)
            .ToListAsync();

        // Get analytics data
        var analytics = await GetAdminAnalytics();
        
        var model = new AdminDashboardViewModel
        {
            TotalUsers = allUsers.Count,
            ActiveUsers = allUsers.Count(u => u.IsActive),
            TrialUsers = allUsers.Count(u => u.SubscriptionPlan == SubscriptionPlan.Pocket && u.IsActive),
            PaidUsers = allUsers.Count(u => u.SubscriptionPlan != SubscriptionPlan.Pocket && u.IsActive),
            ExpiredTrialUsers = allUsers.Count(u => u.IsTrialExpired && u.IsActive), // Now this works since it's client-side evaluation
            RecentUsers = allUsers
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .ToList(),
            SystemStats = new SystemStatsViewModel
            {
                TotalCategories = await _context.Categories.CountAsync(),
                TotalTransactions = await _context.Transactions.CountAsync(),
                TotalBudgets = await _context.MonthlyBudgets.CountAsync(),
                TotalSystemRevenue = CalculateSystemRevenue(allUsers),
                MonthlyRecurringRevenue = CalculateMonthlyRecurringRevenue(allUsers),
                TotalSubAccounts = allUsers.Count(u => u.UserType == UserType.SubAccount)
            },
            Analytics = analytics
        };

        return View(model);
    }

    private async Task<AdminAnalyticsViewModel> GetAdminAnalytics()
    {
        var currentDate = DateTime.UtcNow;
        var sixMonthsAgo = currentDate.AddMonths(-6);
        var oneYearAgo = currentDate.AddYears(-1);

        // User growth data (last 6 months)
        var userGrowthData = new List<UserGrowthData>();
        var allUsers = await _context.Users.ToListAsync();
        
        for (int i = 5; i >= 0; i--)
        {
            var monthStart = currentDate.AddMonths(-i).Date.AddDays(-(currentDate.AddMonths(-i).Day - 1));
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            
            var newUsersThisMonth = allUsers.Count(u => u.CreatedAt >= monthStart && u.CreatedAt <= monthEnd);
            var cumulativeUsers = allUsers.Count(u => u.CreatedAt <= monthEnd);
            
            userGrowthData.Add(new UserGrowthData
            {
                Date = monthStart.ToString("MMM yyyy"),
                NewUsers = newUsersThisMonth,
                CumulativeUsers = cumulativeUsers
            });
        }

        // Subscription distribution
        var subscriptionDistribution = Enum.GetValues<SubscriptionPlan>()
            .Select(plan => new SubscriptionDistributionData
            {
                Plan = plan.ToString(),
                Count = allUsers.Count(u => u.SubscriptionPlan == plan && u.IsActive),
                Revenue = CalculatePlanRevenue(allUsers.Where(u => u.SubscriptionPlan == plan && u.IsActive).ToList(), plan)
            })
            .ToList();

        // Monthly revenue data (last 12 months)
        var revenueData = new List<MonthlyRevenueData>();
        for (int i = 11; i >= 0; i--)
        {
            var month = currentDate.AddMonths(-i);
            var monthStr = month.ToString("MMM yyyy");
            
            revenueData.Add(new MonthlyRevenueData
            {
                Month = monthStr,
                Revenue = CalculateMonthRevenue(allUsers, month),
                NewSubscriptions = allUsers.Count(u => u.LastPaymentDate?.Month == month.Month && u.LastPaymentDate?.Year == month.Year),
                Churn = CalculateChurnForMonth(allUsers, month)
            });
        }

        // User activity data (last 30 days)
        var activityData = new List<UserActivityData>();
        var transactions = await _context.Transactions
            .Where(t => t.Date >= currentDate.AddDays(-30))
            .ToListAsync();
        var budgets = await _context.MonthlyBudgets.ToListAsync();

        for (int i = 29; i >= 0; i--)
        {
            var date = currentDate.AddDays(-i).Date;
            var dayTransactions = transactions.Where(t => t.Date.Date == date).ToList();
            
            activityData.Add(new UserActivityData
            {
                Date = date.ToString("MMM dd"),
                ActiveUsers = dayTransactions.Select(t => t.Category?.UserId).Distinct().Count(),
                Transactions = dayTransactions.Count,
                BudgetsCreated = 0 // Would need creation tracking
            });
        }

        // Trial conversion data (last 6 months)
        var trialConversions = new List<TrialConversionData>();
        for (int i = 5; i >= 0; i--)
        {
            var monthStart = currentDate.AddMonths(-i).Date.AddDays(-(currentDate.AddMonths(-i).Day - 1));
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            
            var trialUsersThisMonth = allUsers.Where(u => u.CreatedAt >= monthStart && u.CreatedAt <= monthEnd && u.SubscriptionPlan == SubscriptionPlan.Pocket).ToList();
            var conversions = trialUsersThisMonth.Count(u => u.SubscriptionPlan != SubscriptionPlan.Pocket);
            
            trialConversions.Add(new TrialConversionData
            {
                CohortMonth = monthStart.ToString("MMM yyyy"),
                TrialUsers = trialUsersThisMonth.Count,
                Conversions = conversions,
                ConversionRate = trialUsersThisMonth.Count > 0 ? Math.Round((double)conversions / trialUsersThisMonth.Count * 100, 1) : 0
            });
        }

        var analytics = new AdminAnalyticsViewModel
        {
            UserGrowthChart = userGrowthData,
            SubscriptionDistribution = subscriptionDistribution,
            RevenueChart = revenueData,
            UserActivityChart = activityData,
            TrialConversions = trialConversions,
            TotalTransactionsThisMonth = transactions.Count(t => t.Date.Month == currentDate.Month && t.Date.Year == currentDate.Year),
            TotalCategoriesThisMonth = await _context.Categories.CountAsync(),
            AverageTransactionValue = transactions.Any() ? Math.Round(transactions.Average(t => t.Amount), 2) : 0,
            SystemHealthScore = CalculateSystemHealthScore(allUsers, transactions),
            ActiveUsersThisWeek = allUsers.Count(u => u.IsActive && (u.LastLoginAt ?? DateTime.MinValue) >= currentDate.AddDays(-7)),
            ActiveUsersThisMonth = allUsers.Count(u => u.IsActive && (u.LastLoginAt ?? DateTime.MinValue) >= currentDate.AddDays(-30)),
            AvgTransactionsPerUser = allUsers.Count > 0 ? Math.Round((double)transactions.Count / allUsers.Count, 1) : 0,
            AvgCategoriesPerUser = allUsers.Count > 0 ? Math.Round((double)await _context.Categories.CountAsync() / allUsers.Count, 1) : 0
        };

        return analytics;
    }

    private decimal CalculateSystemRevenue(List<ApplicationUser> users)
    {
        // Simple calculation - in reality this would be more complex
        return users.Where(u => u.SubscriptionPlan != SubscriptionPlan.Pocket && u.IsActive)
            .Sum(u => u.SubscriptionPlan switch
            {
                SubscriptionPlan.Bankroll => 6.00m,
                SubscriptionPlan.Stack => 10.00m,
                SubscriptionPlan.Vault => 16.00m,
                SubscriptionPlan.Prime => 24.00m,
                _ => 0m
            });
    }

    private decimal CalculateMonthlyRecurringRevenue(List<ApplicationUser> users)
    {
        return CalculateSystemRevenue(users);
    }

    private decimal CalculatePlanRevenue(List<ApplicationUser> users, SubscriptionPlan plan)
    {
        var monthlyRate = plan switch
        {
            SubscriptionPlan.Bankroll => 6.00m,
            SubscriptionPlan.Stack => 10.00m,
            SubscriptionPlan.Vault => 16.00m,
            SubscriptionPlan.Prime => 24.00m,
            _ => 0m
        };
        return users.Count * monthlyRate;
    }

    private decimal CalculateMonthRevenue(List<ApplicationUser> users, DateTime month)
    {
        return users.Where(u => u.SubscriptionPlan != SubscriptionPlan.Pocket && u.IsActive)
            .Sum(u => u.SubscriptionPlan switch
            {
                SubscriptionPlan.Bankroll => 6.00m,
                SubscriptionPlan.Stack => 10.00m,
                SubscriptionPlan.Vault => 16.00m,
                SubscriptionPlan.Prime => 24.00m,
                _ => 0m
            });
    }

    private int CalculateChurnForMonth(List<ApplicationUser> users, DateTime month)
    {
        // Simplified churn calculation
        return users.Count(u => !u.IsActive && u.CreatedAt < month.AddMonths(-1) && 
                          (u.LastLoginAt?.Month == month.Month && u.LastLoginAt?.Year == month.Year));
    }

    private decimal CalculateSystemHealthScore(List<ApplicationUser> users, List<Transaction> transactions)
    {
        var activeUserRatio = users.Count > 0 ? (double)users.Count(u => u.IsActive) / users.Count : 1;
        var paidUserRatio = users.Count > 0 ? (double)users.Count(u => u.SubscriptionPlan != SubscriptionPlan.Pocket) / users.Count : 0;
        var engagementScore = transactions.Any() ? Math.Min(transactions.Count / Math.Max(users.Count, 1) / 10.0, 1) : 0;
        
        return Math.Round((decimal)((activeUserRatio * 0.4 + paidUserRatio * 0.4 + engagementScore * 0.2) * 100), 1);
    }

    // User management - shows ALL users including sub-accounts
    public async Task<IActionResult> Users(int page = 1, string search = "", string status = "all", int pageSize = 20)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        // Include parent and sub-account relationships for comprehensive view
        var query = _context.Users
            .Include(u => u.ParentUser)
            .Include(u => u.SubAccounts)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.FirstName.Contains(search) || 
                                   u.LastName.Contains(search) || 
                                   u.Email!.Contains(search) ||
                                   (u.ParentUser != null && 
                                    (u.ParentUser.FirstName.Contains(search) || 
                                     u.ParentUser.LastName.Contains(search) || 
                                     u.ParentUser.Email!.Contains(search))));
        }

        // Apply status filter
        query = status switch
        {
            "active" => query.Where(u => u.IsActive),
            "inactive" => query.Where(u => !u.IsActive),
            "trial" => query.Where(u => u.SubscriptionPlan == SubscriptionPlan.Pocket && u.IsActive),
            "expired" => query.Where(u => u.IsTrialExpired && u.IsActive),
            "paid" => query.Where(u => u.SubscriptionPlan != SubscriptionPlan.Pocket),
            "main" => query.Where(u => u.UserType == UserType.MainAccount),
            "sub" => query.Where(u => u.UserType == UserType.SubAccount),
            _ => query
        };

        // Ensure pageSize is within reasonable bounds
        pageSize = Math.Min(Math.Max(pageSize, 10), 100);

        var totalUsers = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.UserType) // Main accounts first, then sub-accounts
            .ThenByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new AdminUserManagementViewModel
        {
            Users = users,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize),
            SearchTerm = search,
            StatusFilter = status,
            TotalUsers = totalUsers
        };

        return View(model);
    }

    // Deactivate user
    [HttpPost]
    public async Task<IActionResult> DeactivateUser(string userId, string reason = "")
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.IsActive = false;
        user.DeactivationReason = reason;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"User {user.FullName} has been deactivated.";
        return RedirectToAction(nameof(Users));
    }

    // Reactivate user
    [HttpPost]
    public async Task<IActionResult> ReactivateUser(string userId)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.IsActive = true;
        user.DeactivationReason = null;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"User {user.FullName} has been reactivated.";
        return RedirectToAction(nameof(Users));
    }

    // Extend trial
    [HttpPost]
    public async Task<IActionResult> ExtendTrial(string userId, int days)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.TrialEndDate = user.TrialEndDate.AddDays(days);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Trial extended by {days} days for {user.FullName}.";
        return RedirectToAction(nameof(Users));
    }

    // Update subscription
    [HttpPost]
    public async Task<IActionResult> UpdateSubscription(string userId, SubscriptionPlan plan)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.SubscriptionPlan = plan;
        if (plan != SubscriptionPlan.Pocket)
        {
            user.LastPaymentDate = DateTime.UtcNow;
            user.NextPaymentDue = DateTime.UtcNow.AddMonths(1);
        }
        
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Subscription updated to {plan} for {user.FullName}.";
        return RedirectToAction(nameof(Users));
    }

    // View user details
    public async Task<IActionResult> UserDetails(string id)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var user = await _context.Users
            .Include(u => u.Categories)
                .ThenInclude(c => c.Transactions)
            .Include(u => u.Categories)
                .ThenInclude(c => c.Budgets)
            .Include(u => u.SubAccounts)
            .Include(u => u.ParentUser)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        var model = new UserDetailsViewModel
        {
            User = user,
            TotalCategories = user.Categories.Count,
            TotalTransactions = user.Categories.SelectMany(c => c.Transactions).Count(),
            TotalBudgetAmount = user.Categories
                .SelectMany(c => c.Budgets)
                .Where(b => b.MonthYear == DateTime.Now.ToString("yyyy-MM"))
                .Sum(b => b.Amount),
            LastActivity = user.Categories
                .SelectMany(c => c.Transactions)
                .OrderByDescending(t => t.Date)
                .FirstOrDefault()?.Date
        };

        return View(model);
    }

    // System settings
    public async Task<IActionResult> Settings()
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        // Get statistics for the admin settings page
        var totalUsers = await _context.Users.CountAsync();
        var totalTransactions = await _context.Transactions.CountAsync();
        var totalCategories = await _context.Categories.CountAsync();

        var model = new AdminSettingsViewModel
        {
            TotalUsers = totalUsers,
            TotalTransactions = totalTransactions,
            TotalCategories = totalCategories,
            DatabaseSizeMB = 0, // Would need actual database size calculation
            
            // Default system settings
            DefaultTrialPeriodDays = 30,
            MaxSubAccountsForFree = 5,
            EnableTrialExtensions = true,
            AutoDeactivateExpiredTrials = false,
            
            // Default user management settings
            AllowSelfRegistration = true,
            RequireEmailVerification = true,
            DefaultUserRole = "User",
            PasswordMinLength = 6,
            
            // Default security settings
            EnableTwoFactorAuth = false,
            LogUserActivity = true,
            SessionTimeoutMinutes = 60,
            MaxLoginAttempts = 5
        };

        return View(model);
    }

    // Database management interface
    public async Task<IActionResult> DatabaseManager()
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var model = new DatabaseManagerViewModel
        {
            Tables = new List<DatabaseTableInfo>
            {
                new() { TableName = "AspNetUsers", Description = "Application users and authentication", RecordCount = await _context.Users.CountAsync() },
                new() { TableName = "Categories", Description = "Budget categories/envelopes", RecordCount = await _context.Categories.CountAsync() },
                new() { TableName = "Transactions", Description = "Financial transactions", RecordCount = await _context.Transactions.CountAsync() },
                new() { TableName = "MonthlyBudgets", Description = "Monthly budget allocations", RecordCount = await _context.MonthlyBudgets.CountAsync() },
                new() { TableName = "MonthlyIncomes", Description = "Monthly income records", RecordCount = await _context.MonthlyIncomes.CountAsync() },
                new() { TableName = "FinancialGoals", Description = "Financial goals and targets", RecordCount = await _context.FinancialGoals.CountAsync() },
                new() { TableName = "ScheduledTransactions", Description = "Scheduled/recurring transactions", RecordCount = await _context.ScheduledTransactions.CountAsync() },
                new() { TableName = "SavedReports", Description = "User saved reports", RecordCount = await _context.SavedReports.CountAsync() },
                new() { TableName = "UserActions", Description = "User activity logs", RecordCount = await _context.UserActions.CountAsync() },
                new() { TableName = "Notifications", Description = "System notifications", RecordCount = await _context.Notifications.CountAsync() },
                new() { TableName = "TransactionTags", Description = "Transaction tags and labels", RecordCount = await _context.TransactionTags.CountAsync() },
                new() { TableName = "TransactionSplits", Description = "Transaction category splits", RecordCount = await _context.TransactionSplits.CountAsync() }
            }
        };

        return View(model);
    }

    // Browse table data
    public async Task<IActionResult> BrowseTable(string tableName, int page = 1, int pageSize = 50)
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var model = new BrowseTableViewModel
        {
            TableName = tableName,
            Page = page,
            PageSize = pageSize
        };

        try
        {
            switch (tableName.ToLower())
            {
                case "aspnetusers":
                    var users = await _context.Users
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
                    model.Data = users.Select(u => new Dictionary<string, object>
                    {
                        ["Id"] = u.Id,
                        ["UserName"] = u.UserName ?? "",
                        ["Email"] = u.Email ?? "",
                        ["FirstName"] = u.FirstName,
                        ["LastName"] = u.LastName,
                        ["UserType"] = u.UserType.ToString(),
                        ["SubscriptionPlan"] = u.SubscriptionPlan.ToString(),
                        ["IsAdmin"] = u.IsAdmin,
                        ["IsActive"] = u.IsActive,
                        ["CreatedAt"] = u.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    }).ToList();
                    model.TotalRecords = await _context.Users.CountAsync();
                    break;

                case "categories":
                    var categories = await _context.Categories
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
                    model.Data = categories.Select(c => new Dictionary<string, object>
                    {
                        ["Id"] = c.Id,
                        ["Name"] = c.Name,
                        ["UserId"] = c.UserId
                    }).ToList();
                    model.TotalRecords = await _context.Categories.CountAsync();
                    break;

                case "transactions":
                    var transactions = await _context.Transactions
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
                    model.Data = transactions.Select(t => new Dictionary<string, object>
                    {
                        ["Id"] = t.Id,
                        ["Description"] = t.Description,
                        ["Amount"] = t.Amount,
                        ["Date"] = t.Date.ToString("yyyy-MM-dd"),
                        ["CategoryId"] = t.CategoryId?.ToString() ?? "N/A",
                        ["AccountId"] = t.AccountId?.ToString() ?? "N/A",
                        ["CreatedAt"] = t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    }).ToList();
                    model.TotalRecords = await _context.Transactions.CountAsync();
                    break;

                default:
                    model.Data = new List<Dictionary<string, object>>();
                    model.TotalRecords = 0;
                    break;
            }

            model.TotalPages = (int)Math.Ceiling((double)model.TotalRecords / pageSize);
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = $"Error loading table data: {ex.Message}";
            model.Data = new List<Dictionary<string, object>>();
        }

        return View(model);
    }

    // Export table data
    public async Task<IActionResult> ExportTable(string tableName, string format = "csv")
    {
        if (!await IsCurrentUserAdmin())
        {
            return Forbid();
        }

        try
        {
            var data = new List<Dictionary<string, object>>();
            var fileName = $"{tableName}_{DateTime.Now:yyyyMMdd_HHmmss}";

            // Get data based on table name
            switch (tableName.ToLower())
            {
                case "aspnetusers":
                    var users = await _context.Users.ToListAsync();
                    data = users.Select(u => new Dictionary<string, object>
                    {
                        ["Id"] = u.Id,
                        ["UserName"] = u.UserName ?? "",
                        ["Email"] = u.Email ?? "",
                        ["FirstName"] = u.FirstName,
                        ["LastName"] = u.LastName,
                        ["UserType"] = u.UserType.ToString(),
                        ["SubscriptionPlan"] = u.SubscriptionPlan.ToString(),
                        ["IsAdmin"] = u.IsAdmin,
                        ["IsActive"] = u.IsActive,
                        ["CreatedAt"] = u.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    }).ToList();
                    break;

                case "transactions":
                    var transactions = await _context.Transactions.ToListAsync();
                    data = transactions.Select(t => new Dictionary<string, object>
                    {
                        ["Id"] = t.Id,
                        ["Description"] = t.Description,
                        ["Amount"] = t.Amount,
                        ["Date"] = t.Date.ToString("yyyy-MM-dd"),
                        ["CategoryId"] = t.CategoryId?.ToString() ?? "N/A",
                        ["AccountId"] = t.AccountId?.ToString() ?? "N/A",
                        ["CreatedAt"] = t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    }).ToList();
                    break;
            }

            if (format.ToLower() == "json")
            {
                var json = System.Text.Json.JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"{fileName}.json");
            }
            else
            {
                // Generate CSV
                var csv = new System.Text.StringBuilder();
                if (data.Any())
                {
                    // Header
                    csv.AppendLine(string.Join(",", data.First().Keys));
                    
                    // Data rows
                    foreach (var row in data)
                    {
                        csv.AppendLine(string.Join(",", row.Values.Select(v => $"\"{v}\"")));
                    }
                }

                return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"{fileName}.csv");
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Export failed: {ex.Message}";
            return RedirectToAction("DatabaseManager");
        }
    }
}