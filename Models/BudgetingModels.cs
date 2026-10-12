using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoughDoodle.Models;

public enum IncomeAllocation
{
    Immediate = 1,      // Available immediately 
    HoldForNextMonth = 2,   // Hold for next month
    AutoHold = 3        // Auto-hold based on settings
}

public class Income
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Source { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    public DateTime ReceivedDate { get; set; }
    
    [Required]
    [MaxLength(7)] // Format: YYYY-MM
    public string MonthYear { get; set; } = string.Empty;
    
    public IncomeAllocation Allocation { get; set; } = IncomeAllocation.Immediate;
    
    public bool IsAllocated { get; set; } = false;
    
    // User association
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CategoryRollover
{
    public int Id { get; set; }
    
    public int CategoryId { get; set; }
    
    [ForeignKey("CategoryId")]
    public Category Category { get; set; } = null!;
    
    [Required]
    [MaxLength(7)] // Format: YYYY-MM
    public string FromMonthYear { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(7)] // Format: YYYY-MM
    public string ToMonthYear { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; } // Positive for surplus, negative for overspend
    
    public bool IsOverspend { get; set; } = false;
    
    [MaxLength(200)]
    public string Notes { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class BudgetSettings
{
    public int Id { get; set; }
    
    // User association
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    // Rollover settings
    public bool AutoRolloverSurplus { get; set; } = true;
    public bool AutoRolloverOverspend { get; set; } = true;
    public bool AllowCoverOverspendFromOtherCategories { get; set; } = true;
    
    // Income settings
    public IncomeAllocation DefaultIncomeAllocation { get; set; } = IncomeAllocation.Immediate;
    public bool AutoHoldIncomeForNextMonth { get; set; } = false;
    
    // Budget creation settings
    public bool CopyLastMonthBudgets { get; set; } = false;
    public bool FillBudgetsFromAccountBalances { get; set; } = false;
    
    // UI settings
    public bool EnableDarkMode { get; set; } = false;
    public string ColorTheme { get; set; } = "default";
    public bool MinimalUI { get; set; } = false;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}