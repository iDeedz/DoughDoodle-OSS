using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoughDoodle.Models;
public class MonthlyBudget
{
  public int Id { get; set; }
  public int CategoryId { get; set; }
  public Category Category { get; set; } = null!;
  
  [Required]
  [MaxLength(7)] // Format: YYYY-MM
  public string MonthYear { get; set; } = string.Empty;
  
  [Column(TypeName = "decimal(18,2)")]
  public decimal Amount { get; set; }
}