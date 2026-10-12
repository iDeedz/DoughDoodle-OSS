using DoughDoodle.Models;

namespace DoughDoodle.Models.ViewModels;

// User Dashboard Analytics ViewModels
public class UserDashboardViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string CurrentMonth { get; set; } = string.Empty;
    /// <summary>ISO display currency from ApplicationUser.PreferredCurrency. Label only — no FX conversion.</summary>
    public string PreferredCurrency { get; set; } = "USD";
    
    // Basic Stats
    public UserBudgetStatsViewModel BudgetStats { get; set; } = new();
    public UserSpendingAnalyticsViewModel SpendingAnalytics { get; set; } = new();
    public UserFinancialHealthViewModel FinancialHealth { get; set; } = new();
    public DoughDoodle.Services.SafeToSpendResult? SafeToSpend { get; set; }
    public DoughDoodle.Services.SubscriptionHunterResult SubscriptionLeak { get; set; } = new();
    
    // Charts Data
    public List<MonthlySpendingData> MonthlySpendingChart { get; set; } = new();
    public List<CategorySpendingData> CategoryBreakdownChart { get; set; } = new();
    public List<BudgetVsActualData> BudgetVsActualChart { get; set; } = new();
    public List<SpendingTrendData> SpendingTrendChart { get; set; } = new();
    public List<IncomeVsExpensesData> IncomeVsExpensesChart { get; set; } = new();
}

public class UserBudgetStatsViewModel
{
    public decimal TotalBudgeted { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalRemaining { get; set; }
    public decimal SpentPercentage { get; set; }
    public int TotalCategories { get; set; }
    public int OverBudgetCategories { get; set; }
    public int UnderBudgetCategories { get; set; }
    public decimal BiggestOverspend { get; set; }
    public string BiggestOverspendCategory { get; set; } = string.Empty;
}

public class UserSpendingAnalyticsViewModel
{
    public decimal AverageMonthlySpending { get; set; }
    public decimal HighestTransactionAmount { get; set; }
    public string MostSpentCategory { get; set; } = string.Empty;
    public decimal MostSpentCategoryAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    public int DaysIntoMonth { get; set; }
    public decimal DailySpendingAverage { get; set; }
    public decimal ProjectedMonthlySpending { get; set; }
}

public class UserFinancialHealthViewModel
{
    public decimal HealthScore { get; set; } // 0-100
    public string HealthStatus { get; set; } = string.Empty; // Excellent, Good, Fair, Poor
    public List<string> HealthInsights { get; set; } = new();
    public decimal EmergencyFundBalance { get; set; }
    public decimal RecommendedEmergencyFund { get; set; }
    public bool OnTrackForGoals { get; set; }
    public decimal SavingsRate { get; set; }
}

public class MonthlySpendingData
{
    public string Month { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Budget { get; set; }
    public int TransactionCount { get; set; }
}

public class CategorySpendingData
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Budget { get; set; }
    public decimal Percentage { get; set; }
    public string Color { get; set; } = string.Empty;
}

public class BudgetVsActualData
{
    public string Category { get; set; } = string.Empty;
    public decimal Budgeted { get; set; }
    public decimal Actual { get; set; }
    public decimal Variance { get; set; }
    public string Status { get; set; } = string.Empty; // Over, Under, OnTrack
}

public class SpendingTrendData
{
    public string Date { get; set; } = string.Empty;
    public decimal CumulativeSpending { get; set; }
    public decimal BudgetPace { get; set; }
    public decimal ProjectedSpending { get; set; }
}

public class IncomeVsExpensesData
{
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetAmount { get; set; }
}

// Sub-user dashboard for parents/managers
public class SubUserDashboardViewModel
{
    public ApplicationUser ParentUser { get; set; } = null!;
    public List<SubUserSummary> SubUserSummaries { get; set; } = new();
    public FamilyFinancialOverview FamilyOverview { get; set; } = new();
    public List<FamilySpendingData> FamilySpendingChart { get; set; } = new();
}

public class SubUserSummary
{
    public ApplicationUser User { get; set; } = null!;
    public decimal TotalBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal Remaining { get; set; }
    public int CategoryCount { get; set; }
    public DateTime LastActivity { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
}

public class FamilyFinancialOverview
{
    public decimal TotalFamilyBudget { get; set; }
    public decimal TotalFamilySpending { get; set; }
    public decimal TotalFamilyRemaining { get; set; }
    public int TotalFamilyMembers { get; set; }
    public string TopSpendingMember { get; set; } = string.Empty;
    public decimal TopSpendingAmount { get; set; }
    public List<string> FamilyInsights { get; set; } = new();
}

public class FamilySpendingData
{
    public string Member { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Budget { get; set; }
    public string Color { get; set; } = string.Empty;
}