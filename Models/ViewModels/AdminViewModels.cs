using DoughDoodle.Models;

namespace DoughDoodle.Models.ViewModels;

// View Models for Admin functionality
public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TrialUsers { get; set; }
    public int PaidUsers { get; set; }
    public int ExpiredTrialUsers { get; set; }
    public List<ApplicationUser> RecentUsers { get; set; } = new();
    public SystemStatsViewModel SystemStats { get; set; } = new();
    
    // Enhanced analytics
    public AdminAnalyticsViewModel Analytics { get; set; } = new();
}

public class SystemStatsViewModel
{
    public int TotalCategories { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalBudgets { get; set; }
    public decimal TotalSystemRevenue { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public int TotalSubAccounts { get; set; }
}

public class AdminAnalyticsViewModel
{
    // User growth data
    public List<UserGrowthData> UserGrowthChart { get; set; } = new();
    public List<SubscriptionDistributionData> SubscriptionDistribution { get; set; } = new();
    public List<MonthlyRevenueData> RevenueChart { get; set; } = new();
    public List<UserActivityData> UserActivityChart { get; set; } = new();
    public List<TrialConversionData> TrialConversions { get; set; } = new();
    
    // System performance
    public int TotalTransactionsThisMonth { get; set; }
    public int TotalCategoriesThisMonth { get; set; }
    public decimal AverageTransactionValue { get; set; }
    public decimal SystemHealthScore { get; set; }
    
    // User engagement
    public int ActiveUsersThisWeek { get; set; }
    public int ActiveUsersThisMonth { get; set; }
    public double AvgTransactionsPerUser { get; set; }
    public double AvgCategoriesPerUser { get; set; }
}

public class UserGrowthData
{
    public string Date { get; set; } = string.Empty;
    public int NewUsers { get; set; }
    public int CumulativeUsers { get; set; }
}

public class SubscriptionDistributionData
{
    public string Plan { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public class MonthlyRevenueData
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int NewSubscriptions { get; set; }
    public int Churn { get; set; }
}

public class UserActivityData
{
    public string Date { get; set; } = string.Empty;
    public int ActiveUsers { get; set; }
    public int Transactions { get; set; }
    public int BudgetsCreated { get; set; }
}

public class TrialConversionData
{
    public string CohortMonth { get; set; } = string.Empty;
    public int TrialUsers { get; set; }
    public int Conversions { get; set; }
    public double ConversionRate { get; set; }
}

public class AdminUserManagementViewModel
{
    public List<ApplicationUser> Users { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string SearchTerm { get; set; } = "";
    public string StatusFilter { get; set; } = "all";
    public int TotalUsers { get; set; }
}

public class UserDetailsViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public int TotalCategories { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalBudgetAmount { get; set; }
    public DateTime? LastActivity { get; set; }
}

public class SystemSettingsViewModel
{
    public int DefaultTrialPeriodDays { get; set; }
    public int MaxSubAccountsForFree { get; set; }
    public bool EnableTrialExtensions { get; set; }
    public bool AutoDeactivateExpiredTrials { get; set; }
}

public class AdminSettingsViewModel
{
    public SystemSettingsViewModel SystemSettings { get; set; } = new();
    public bool NotificationsEnabled { get; set; }
    public string AdminEmail { get; set; } = "";
    public bool MaintenanceMode { get; set; }
    public int TotalUsers { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalCategories { get; set; }
    public double DatabaseSizeMB { get; set; }
    
    // System configuration
    public int DefaultTrialPeriodDays { get; set; } = 30;
    public int MaxSubAccountsForFree { get; set; } = 5;
    public bool EnableTrialExtensions { get; set; } = true;
    public bool AutoDeactivateExpiredTrials { get; set; } = false;
    
    // User management settings
    public bool AllowSelfRegistration { get; set; } = true;
    public bool RequireEmailVerification { get; set; } = true;
    public string DefaultUserRole { get; set; } = "User";
    public int PasswordMinLength { get; set; } = 6;
    
    // Security policies
    public bool EnableTwoFactorAuth { get; set; } = false;
    public bool LogUserActivity { get; set; } = true;
    public int SessionTimeoutMinutes { get; set; } = 60;
    public int MaxLoginAttempts { get; set; } = 5;
}

// Database Management ViewModels
public class DatabaseManagerViewModel
{
    public List<DatabaseTableInfo> Tables { get; set; } = new();
}

public class DatabaseTableInfo
{
    public string TableName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class BrowseTableViewModel
{
    public string TableName { get; set; } = string.Empty;
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public string? ErrorMessage { get; set; }
}