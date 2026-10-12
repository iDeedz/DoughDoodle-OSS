using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoughDoodle.Models;
public class Category
{
  public int Id { get; set; }
  
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;
  
  // User association
  [Required]
  public string UserId { get; set; } = string.Empty;
  
  [ForeignKey("UserId")]
  public ApplicationUser User { get; set; } = null!;
  
  public List<Transaction> Transactions { get; set; } = new();
  public List<MonthlyBudget> Budgets { get; set; } = new();

  /// <summary>When true, household members can see this envelope (team budgets).</summary>
  public bool IsSharedWithHousehold { get; set; } = false;

  /// <summary>Optional business/project tag for expense tracking.</summary>
  public int? ExpenseProjectId { get; set; }

}