using DoughDoodle.Models;

namespace DoughDoodle.Models.ViewModels;

public class MonthlyTrendItem
{
    public string Month { get; set; } = string.Empty;
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalSpending { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransaction { get; set; }
    public int CategoriesUsed { get; set; }
}

public class TrendAnalysisViewModel
{
    public List<MonthlyTrendItem> MonthlyTrends { get; set; } = new();
    public Dictionary<string, List<MonthlyTrendItem>> CategoryTrends { get; set; } = new();
}

public class UserReportViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public decimal TotalSpent { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageMonthlySpending { get; set; }
    public decimal HighestMonthlySpending { get; set; }
    public int TotalCategories { get; set; }
    public int ActiveGoals { get; set; }
    public int CompletedGoals { get; set; }
    public string AccountAgeText { get; set; } = string.Empty;
    public TimeSpan AccountAge { get; set; }
}

public class MonthlyReportItem
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal BudgetedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal RemainingAmount => BudgetedAmount - ActualAmount;
    public bool IsOverBudget => ActualAmount > BudgetedAmount;
    public double BudgetUtilizationPercentage => BudgetedAmount == 0 ? 0 : (double)(ActualAmount / BudgetedAmount) * 100;
    public int TransactionCount { get; set; }
    public List<Transaction> Transactions { get; set; } = new();
}

public class CategoryAnalysisItem
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
    public decimal AverageTransaction { get; set; }
    public decimal LargestTransaction { get; set; }
    public int TransactionCount { get; set; }
    public decimal MonthlyAverage { get; set; }
    public DateTime LastTransaction { get; set; }
}