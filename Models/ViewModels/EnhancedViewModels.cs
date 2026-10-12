using DoughDoodle.Models;

namespace DoughDoodle.Models.ViewModels;

public class GoalsViewModel
{
    public List<FinancialGoal> Goals { get; set; } = new();
    public int TotalGoals { get; set; }
    public int ActiveGoals { get; set; }
    public int CompletedGoals { get; set; }
    public decimal TotalTargetAmount { get; set; }
    public decimal TotalCurrentAmount { get; set; }
    public decimal OverallProgress => TotalTargetAmount > 0 ? Math.Round((TotalCurrentAmount / TotalTargetAmount) * 100, 1) : 0;
    public string PreferredCurrency { get; set; } = "USD";
}

public class NotificationsViewModel
{
    public List<Notification> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }
}

public class SettingsViewModel
{
    public string PreferredCurrency { get; set; } = "USD";
    public BudgetSettings BudgetSettings { get; set; } = null!;
    public NotificationSettings NotificationSettings { get; set; } = null!;
}

public class SecurityViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public List<SecurityLog> RecentSecurityLogs { get; set; } = new();
    public bool TwoFactorEnabled { get; set; }
}

public class ImportExportViewModel
{
    public List<ImportSession> RecentImports { get; set; } = new();
    public int ImportSessionCount { get; set; }
    public int RecordsImported { get; set; }
    /// <summary>No persisted export-download counter. Always 0 until one exists.</summary>
    public int ExportDownloadCount { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public string LastActivityLabel { get; set; } = "—";
}

public class SearchViewModel
{
    public string Query { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? CategoryFilter { get; set; }
    public string? AccountFilter { get; set; }
    public TransactionType? TypeFilter { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    
    public List<Transaction> Results { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Account> Accounts { get; set; } = new();
    public int TotalResults { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);
}

public class RecurringTransactionsViewModel
{
    public List<ScheduledTransaction> ScheduledTransactions { get; set; } = new();
    public List<ScheduledTransaction> DueTransactions { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Account> Accounts { get; set; } = new();
    public int TotalScheduled { get; set; }
    public int ActiveScheduled { get; set; }
    public int DueCount { get; set; }
}

public class RecurringTransactionViewModel
{
    public List<ScheduledTransaction> ScheduledTransactions { get; set; } = new();
    public List<ScheduledTransaction> DueTransactions { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Account> Accounts { get; set; } = new();
    public int TotalScheduled { get; set; }
    public int ActiveScheduled { get; set; }
    public int DueCount { get; set; }
    public int PausedCount { get; set; }
}

public class AdvancedReportsViewModel
{
    public string SelectedReportType { get; set; } = "spending-trends";
    public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-12);
    public DateTime EndDate { get; set; } = DateTime.Now;
    public List<string> SelectedCategories { get; set; } = new();
    public List<string> SelectedAccounts { get; set; } = new();
    
    // Chart data
    public List<MonthlyTrendData> MonthlyTrends { get; set; } = new();
    public List<CategoryComparisonData> CategoryComparisons { get; set; } = new();
    public List<NetWorthData> NetWorthHistory { get; set; } = new();
    public List<CashFlowData> CashFlowData { get; set; } = new();
    
    // Summary statistics
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetChange { get; set; }
    public decimal AverageMonthlyExpenses { get; set; }
    public string TopSpendingCategory { get; set; } = string.Empty;
    public decimal TopSpendingAmount { get; set; }
}

public class MonthlyTrendData
{
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetAmount { get; set; }
    public decimal BudgetedAmount { get; set; }
}

public class CategoryComparisonData
{
    public string Category { get; set; } = string.Empty;
    public decimal CurrentMonth { get; set; }
    public decimal PreviousMonth { get; set; }
    public decimal AverageAmount { get; set; }
    public decimal PercentageChange { get; set; }
}

public class NetWorthData
{
    public string Date { get; set; } = string.Empty;
    public decimal Assets { get; set; }
    public decimal Liabilities { get; set; }
    public decimal NetWorth { get; set; }
}

public class CashFlowData
{
    public string Month { get; set; } = string.Empty;
    public decimal OperatingCashFlow { get; set; }
    public decimal InvestmentCashFlow { get; set; }
    public decimal FinancingCashFlow { get; set; }
    public decimal NetCashFlow { get; set; }
}

public class BillsViewModel
{
    public List<Bill> AllBills { get; set; } = new();
    public List<Bill> UpcomingBills { get; set; } = new();
    public List<Bill> OverdueBills { get; set; } = new();
    public decimal TotalMonthlyAmount { get; set; }
    public int TotalBillsCount { get; set; }
    public int OverdueBillsCount { get; set; }
    public int UpcomingBillsCount { get; set; }
    public decimal PaidThisMonth { get; set; }
    public decimal RemainingThisMonth { get; set; }
}

public class NetWorthViewModel
{
    public List<NetWorthItem> Assets { get; set; } = new();
    public List<NetWorthItem> Liabilities { get; set; } = new();
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal NetWorth { get; set; }
    public List<NetWorthTrendData> MonthlyTrends { get; set; } = new();
    public int TotalItems { get; set; }
    public int AssetCount { get; set; }
    public int LiabilityCount { get; set; }
}

public class NetWorthTrendData
{
    public string Month { get; set; } = string.Empty;
    public decimal Assets { get; set; }
    public decimal Liabilities { get; set; }
    public decimal NetWorth { get; set; }
}