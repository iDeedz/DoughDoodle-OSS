using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoughDoodle.Models;

public enum ReportType
{
    NetWorth = 1,
    CashFlow = 2,
    SpendingAnalysis = 3,
    Custom = 4
}

public class SavedReport
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    public ReportType ReportType { get; set; }
    
    // Report configuration (JSON)
    public string Configuration { get; set; } = "{}";
    
    // User association
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastGeneratedAt { get; set; } = DateTime.UtcNow;
}

// For undo/redo functionality
public class UserAction
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = string.Empty; // CREATE, UPDATE, DELETE
    
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty; // Transaction, Category, etc.
    
    public int EntityId { get; set; }
    
    // Serialized entity state before action
    public string? PreviousState { get; set; }
    
    // Serialized entity state after action
    public string? NewState { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsUndone { get; set; } = false;
    public DateTime? UndoneAt { get; set; }
}

// For data import tracking
public enum ImportStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    PartiallyCompleted = 5
}

public class ImportSession
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string FileType { get; set; } = string.Empty; // CSV, QIF, OFX, etc.
    
    public ImportStatus Status { get; set; } = ImportStatus.Pending;
    
    public int TotalRecords { get; set; } = 0;
    public int ProcessedRecords { get; set; } = 0;
    public int SuccessfulRecords { get; set; } = 0;
    public int FailedRecords { get; set; } = 0;
    
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
    
    // User association
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}