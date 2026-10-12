using DoughDoodle.Models;

namespace DoughDoodle.Models.ViewModels;

public class UserManagementViewModel
{
    public ApplicationUser CurrentUser { get; set; } = null!;
    public List<ApplicationUser> SubAccounts { get; set; } = new();
    public bool CanCreateSubAccount { get; set; }
    public int SubAccountLimit { get; set; }
    public int UsedSubAccounts { get; set; }
    public string PlanDisplayName { get; set; } = "Pocket";
    public string PreferredCurrency { get; set; } = "USD";
}

public class BudgetAccessViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public List<Category> Categories { get; set; } = new();
    public decimal TotalBudgeted { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalRemaining { get; set; }
    public string CurrentMonth { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; } // For parent viewing sub-account budgets
}

public class SubAccountBudgetSummary
{
    public ApplicationUser SubAccount { get; set; } = null!;
    public decimal TotalBudgeted { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalRemaining { get; set; }
    public int CategoryCount { get; set; }
    public DateTime LastActivity { get; set; }
}