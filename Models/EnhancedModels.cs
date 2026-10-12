using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoughDoodle.Models;

public enum GoalType
{
    Savings = 1,
    DebtPayoff = 2,
    Emergency = 3,
    EmergencyFund = 3, // Alias for Emergency
    Purchase = 4,
    Investment = 5,
    Income = 6,
    Custom = 7
}

public class FinancialGoal
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TargetAmount { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentAmount { get; set; } = 0;
    
    public DateTime? TargetDate { get; set; }
    
    public GoalType GoalType { get; set; } = GoalType.Savings;
    
    public bool IsActive { get; set; } = true;
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    
    // User association
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    // Collaborative household budgeting
    public bool IsShared { get; set; } = false;
    public string? SharedWithUserIds { get; set; } // Comma-separated list of user IDs
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Calculated properties
    [NotMapped]
    public decimal ProgressPercentage => TargetAmount > 0 ? Math.Round((CurrentAmount / TargetAmount) * 100, 1) : 0;
    
    [NotMapped]
    public decimal RemainingAmount => Math.Max(0, TargetAmount - CurrentAmount);
    
    [NotMapped]
    public int DaysRemaining => TargetDate?.Subtract(DateTime.Now).Days ?? 0;
    
    [NotMapped]
    public string Status => IsCompleted ? "Completed" : 
                           IsActive ? "Active" : "Paused";
    
    [NotMapped]
    public List<string> SharedUserIdsList => string.IsNullOrEmpty(SharedWithUserIds) 
        ? new List<string>() 
        : SharedWithUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
}

public enum NotificationType
{
    BudgetAlert = 1,
    GoalReminder = 2,
    GoalAchievement = 3,
    ScheduledTransactionDue = 4,
    MonthlyReport = 5,
    SecurityAlert = 6,
    SystemNotification = 7
}

public enum NotificationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public class Notification
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Medium;
    
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    
    [MaxLength(200)]
    public string? ActionUrl { get; set; }
    
    [MaxLength(50)]
    public string? ActionText { get; set; }
    
    // User association
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class NotificationSettings
{
    public int Id { get; set; }
    
    // User association
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    // Notification preferences
    public bool BudgetAlerts { get; set; } = true;
    public bool GoalReminders { get; set; } = true;
    public bool ScheduledTransactionReminders { get; set; } = true;
    public bool MonthlyReports { get; set; } = true;
    public bool EmailNotifications { get; set; } = true;
    
    // Alert thresholds
    public decimal BudgetThreshold { get; set; } = 80; // Percentage
    public int GoalReminderDays { get; set; } = 7;
    public int ScheduledTransactionReminderDays { get; set; } = 1;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SecurityLog
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Details { get; set; } = string.Empty;
    
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public bool IsSuccessful { get; set; } = true;
    
    [MaxLength(200)]
    public string? FailureReason { get; set; }
}

public enum BillStatus
{
    Pending,
    Paid,
    Overdue,
    Scheduled
}

public enum BillRecurrence
{
    OneTime,
    Daily,
    Weekly,
    BiWeekly,
    Monthly,
    Quarterly,
    SemiAnnually,
    Annually
}

public class Bill
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    public DateTime DueDate { get; set; }
    
    public BillStatus Status { get; set; } = BillStatus.Pending;
    
    public BillRecurrence Recurrence { get; set; } = BillRecurrence.Monthly;
    
    public bool IsRecurring { get; set; } = true;
    
    [MaxLength(100)]
    public string? Payee { get; set; }
    
    [MaxLength(100)]
    public string? AccountNumber { get; set; }
    
    [MaxLength(200)]
    public string? Website { get; set; }
    
    [MaxLength(100)]
    public string? PhoneNumber { get; set; }
    
    public int? CategoryId { get; set; }
    
    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }
    
    public DateTime? LastPaidDate { get; set; }
    
    public DateTime? NextDueDate { get; set; }
    
    public bool AutoPay { get; set; } = false;
    
    [MaxLength(100)]
    public string? AutoPayAccount { get; set; }
    
    public bool NotificationsEnabled { get; set; } = true;
    
    public int NotificationDaysBefore { get; set; } = 3;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class BillPayment
{
    public int Id { get; set; }
    
    [Required]
    public int BillId { get; set; }
    
    [ForeignKey("BillId")]
    public Bill Bill { get; set; } = null!;
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountPaid { get; set; }
    
    public DateTime PaymentDate { get; set; }
    
    public DateTime DueDateWhenPaid { get; set; }
    
    [MaxLength(100)]
    public string PaymentMethod { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? ConfirmationNumber { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public bool IsLate { get; set; } = false;
    
    public int? TransactionId { get; set; }
    
    [ForeignKey("TransactionId")]
    public Transaction? Transaction { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum AssetType
{
    Cash,
    Checking,
    Savings,
    Investment,
    Retirement,
    RealEstate,
    Vehicle,
    Other
}

public enum LiabilityType
{
    CreditCard,
    StudentLoan,
    Mortgage,
    CarLoan,
    PersonalLoan,
    Other
}

public class NetWorthItem
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    public bool IsAsset { get; set; } = true; // true for assets, false for liabilities
    
    public AssetType? AssetType { get; set; }
    
    public LiabilityType? LiabilityType { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentValue { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    // Navigation property for historical values
    public ICollection<NetWorthHistory> History { get; set; } = new List<NetWorthHistory>();
}

public class NetWorthHistory
{
    public int Id { get; set; }
    
    [Required]
    public int NetWorthItemId { get; set; }
    
    [ForeignKey("NetWorthItemId")]
    public NetWorthItem NetWorthItem { get; set; } = null!;
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }
    
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(200)]
    public string? Notes { get; set; }
}

// Investment Account Integration
public enum InvestmentType
{
    Stocks,
    Bonds,
    MutualFunds,
    ETFs,
    Cryptocurrency,
    RealEstate,
    RetirementAccount,
    Other
}

public class InvestmentAccount
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    public InvestmentType InvestmentType { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentValue { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostBasis { get; set; }
    
    [MaxLength(100)]
    public string? AccountNumber { get; set; }
    
    [MaxLength(100)]
    public string? InstitutionName { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    // Navigation property for portfolio holdings
    public ICollection<PortfolioHolding> Holdings { get; set; } = new List<PortfolioHolding>();
    
    [NotMapped]
    public decimal GainLoss => CurrentValue - CostBasis;
    
    [NotMapped]
    public decimal GainLossPercentage => CostBasis > 0 ? Math.Round(((CurrentValue - CostBasis) / CostBasis) * 100, 2) : 0;
}

public class PortfolioHolding
{
    public int Id { get; set; }
    
    [Required]
    public int InvestmentAccountId { get; set; }
    
    [ForeignKey("InvestmentAccountId")]
    public InvestmentAccount InvestmentAccount { get; set; } = null!;
    
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,6)")]
    public decimal Quantity { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal AverageCost { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentPrice { get; set; }
    
    public DateTime LastPriceUpdate { get; set; } = DateTime.UtcNow;
    
    [NotMapped]
    public decimal TotalValue => Quantity * CurrentPrice;
    
    [NotMapped]
    public decimal TotalCost => Quantity * AverageCost;
    
    [NotMapped]
    public decimal GainLoss => TotalValue - TotalCost;
    
    [NotMapped]
    public decimal GainLossPercentage => TotalCost > 0 ? Math.Round(((TotalValue - TotalCost) / TotalCost) * 100, 2) : 0;
}

// Automated Savings Transfer
public enum TransferFrequency
{
    OneTime,
    Daily,
    Weekly,
    BiWeekly,
    Monthly,
    Quarterly,
    Annually
}

public class ScheduledTransfer
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    public int? FromCategoryId { get; set; }
    
    [ForeignKey("FromCategoryId")]
    public Category? FromCategory { get; set; }
    
    public int? ToCategoryId { get; set; }
    
    [ForeignKey("ToCategoryId")]
    public Category? ToCategory { get; set; }
    
    public TransferFrequency Frequency { get; set; } = TransferFrequency.Monthly;
    
    public DateTime NextTransferDate { get; set; }
    
    public DateTime? LastTransferDate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool AutoExecute { get; set; } = false;
    
    public DateTime? EndDate { get; set; }
    
    public int? MaxTransfers { get; set; }
    
    public int TransfersCompleted { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
}

// Tax Optimization
public class TaxOptimizationSuggestion
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal PotentialSavings { get; set; }
    
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty; // e.g., "Deductions", "Credits", "Retirement"
    
    public int Priority { get; set; } = 1; // 1-5, 5 being highest
    
    public bool IsApplied { get; set; } = false;
    
    public DateTime? AppliedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(500)]
    public string? ActionSteps { get; set; }
}