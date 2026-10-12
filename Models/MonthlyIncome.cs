using System.ComponentModel.DataAnnotations;

namespace DoughDoodle.Models;

public class MonthlyIncome
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string MonthYear { get; set; } = string.Empty; // Format: "yyyy-MM"
    
    [Required]
    public decimal Amount { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
}