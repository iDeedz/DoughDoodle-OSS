using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoughDoodle.Models;

public enum TransactionType
{
    Expense = 1,
    Income = 2,
    Transfer = 3
}

public class TransactionSplit
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    
    [ForeignKey("TransactionId")]
    public Transaction Transaction { get; set; } = null!;
    
    public int CategoryId { get; set; }
    
    [ForeignKey("CategoryId")]
    public Category Category { get; set; } = null!;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
}

public class TransactionTag
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(7)] // Color code
    public string Color { get; set; } = "#3b82f6";
    
    // User association
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    public List<TransactionTagAssignment> TransactionTags { get; set; } = new();
}

public class TransactionTagAssignment
{
    public int TransactionId { get; set; }
    public int TagId { get; set; }
    
    [ForeignKey("TransactionId")]
    public Transaction Transaction { get; set; } = null!;
    
    [ForeignKey("TagId")]
    public TransactionTag Tag { get; set; } = null!;
}