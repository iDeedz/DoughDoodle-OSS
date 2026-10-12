using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoughDoodle.Models;
public class Transaction
{
  public int Id { get; set; }
  
  [Required]
  [MaxLength(200)]
  public string Description { get; set; } = string.Empty;
  
  [Column(TypeName = "decimal(18,2)")]
  public decimal Amount { get; set; }
  
  public DateTime Date { get; set; }
  public TransactionType TransactionType { get; set; } = TransactionType.Expense;
  
  // Category (can be null for transfers)
  public int? CategoryId { get; set; }
  
  [ForeignKey("CategoryId")]
  public Category? Category { get; set; }
  
  // Account association
  public int? AccountId { get; set; }
  
  [ForeignKey("AccountId")]
  public Account? Account { get; set; }
  
  // Transfer details (for transfers between categories/accounts)
  public int? TransferToCategoryId { get; set; }
  
  [ForeignKey("TransferToCategoryId")]
  public Category? TransferToCategory { get; set; }
  
  public int? TransferToAccountId { get; set; }
  
  [ForeignKey("TransferToAccountId")]
  public Account? TransferToAccount { get; set; }
  
  // Split transactions
  public bool IsSplit { get; set; } = false;
  public List<TransactionSplit> Splits { get; set; } = new();
  
  // Tags
  public List<TransactionTagAssignment> Tags { get; set; } = new();
  
  // Scheduling
  public int? ScheduledTransactionId { get; set; }
  
  [ForeignKey("ScheduledTransactionId")]
  public ScheduledTransaction? ScheduledTransaction { get; set; }
  
  [MaxLength(500)]
  public string Notes { get; set; } = string.Empty;
  
  // Reconciliation
  public bool IsReconciled { get; set; } = false;
  public DateTime? ReconciledDate { get; set; }
  
  // Import tracking
  [MaxLength(100)]
  public string? ImportId { get; set; }
  
  [MaxLength(100)]
  public string? ImportSource { get; set; }
  
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>Optional business/project association.</summary>
  public int? ExpenseProjectId { get; set; }

  [MaxLength(3)]
  public string? CurrencyCode { get; set; }

  [Column(TypeName = "decimal(18,2)")]
  public decimal? OriginalAmount { get; set; }
}
