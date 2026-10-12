using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoughDoodle.Models;

public enum RecurrenceType
{
    Daily = 1,
    Weekly = 2,
    Biweekly = 3,
    Monthly = 4,
    Quarterly = 5,
    SemiAnnually = 6,
    Annually = 7
}

public class ScheduledTransaction
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    public TransactionType TransactionType { get; set; } = TransactionType.Expense;
    
    public int CategoryId { get; set; }
    
    [ForeignKey("CategoryId")]
    public Category Category { get; set; } = null!;
    
    public int? AccountId { get; set; }
    
    [ForeignKey("AccountId")]
    public Account? Account { get; set; }
    
    public RecurrenceType RecurrenceType { get; set; }
    public int RecurrenceInterval { get; set; } = 1; // Every N periods
    
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    public DateTime NextDueDate { get; set; }
    public DateTime? LastExecuted { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool AutoExecute { get; set; } = false;
    
    // User association
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TransactionRule
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    // Rule matching criteria
    [MaxLength(200)]
    public string DescriptionContains { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? AmountEquals { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? AmountGreaterThan { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? AmountLessThan { get; set; }
    
    // Rule actions
    public int? AssignCategoryId { get; set; }
    
    [ForeignKey("AssignCategoryId")]
    public Category? AssignCategory { get; set; }
    
    public int? AssignAccountId { get; set; }
    
    [ForeignKey("AssignAccountId")]
    public Account? AssignAccount { get; set; }
    
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0; // Higher number = higher priority
    
    // User association
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}