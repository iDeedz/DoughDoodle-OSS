using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoughDoodle.Models;

public enum AccountType
{
    Checking = 1,
    Savings = 2,
    CreditCard = 3,
    Investment = 4,
    Mortgage = 5,
    Loan = 6,
    Cash = 7
}

public class Account
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public AccountType AccountType { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; }
    
    public bool IsOnBudget { get; set; } = true; // On-budget vs Off-budget
    public bool IsActive { get; set; } = true;
    
    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;
    
    // User association
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public List<Transaction> Transactions { get; set; } = new();
}