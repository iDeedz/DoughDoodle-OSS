using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoughDoodle.Models;

/// <summary>Household permission beyond IsAdmin — household owner can elevate members.</summary>
public enum HouseholdRole
{
    Member = 0,
    Editor = 1,
    HouseholdAdmin = 2
}

/// <summary>Pluggable bank sync provider kinds. Live Plaid requires configured keys.</summary>
public enum BankSyncProviderKind
{
    ManualCsv = 0,
    Plaid = 1
}

public class BankConnection
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    public BankSyncProviderKind Provider { get; set; } = BankSyncProviderKind.ManualCsv;

    [MaxLength(100)]
    public string DisplayName { get; set; } = "Manual / CSV";

    [MaxLength(100)]
    public string? InstitutionName { get; set; }

    public int? LinkedAccountId { get; set; }

    [ForeignKey(nameof(LinkedAccountId))]
    public Account? LinkedAccount { get; set; }

    /// <summary>Opaque provider item id when a live API is configured; never store secrets here.</summary>
    [MaxLength(200)]
    public string? ExternalItemId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ExchangeRate
{
    public int Id { get; set; }

    [Required, MaxLength(3)]
    public string BaseCurrency { get; set; } = "USD";

    [Required, MaxLength(3)]
    public string QuoteCurrency { get; set; } = "EUR";

    [Column(TypeName = "decimal(18,6)")]
    public decimal Rate { get; set; } = 1m;

    public DateTime AsOfUtc { get; set; } = DateTime.UtcNow;

    /// <summary>manual | api</summary>
    [MaxLength(20)]
    public string Source { get; set; } = "manual";

    public string? UpdatedByUserId { get; set; }
}

public class ExpenseProject
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsBusiness { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CollaborativeComment
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    /// <summary>Budget | Goal | Transaction | Category</summary>
    [Required, MaxLength(40)]
    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    [Required, MaxLength(2000)]
    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
}

/// <summary>Shared envelope visibility for household team budgeting.</summary>
public class SharedEnvelope
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category Category { get; set; } = null!;

    /// <summary>Household owner (main account) UserId.</summary>
    [Required]
    public string HouseholdOwnerId { get; set; } = string.Empty;

    public bool AllowMemberSpend { get; set; } = true;
    public bool AllowMemberEditBudget { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ImportColumnMapping
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>Bank template key e.g. generic-csv, chase, ofx-default</summary>
    [MaxLength(40)]
    public string BankTemplate { get; set; } = "generic-csv";

    /// <summary>JSON map: date,amount,description,type columns</summary>
    public string MappingJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
