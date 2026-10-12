using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using System.Security.Claims;
using DoughDoodle.Services.Billing;
using Microsoft.AspNetCore.Identity;

namespace DoughDoodle.Controllers;

[Authorize]
public class AccountsController : Controller
{
    private readonly BudgetDbContext _context;

    private readonly IDoughEntitlementService _entitlements;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountsController(BudgetDbContext context, IDoughEntitlementService entitlements, UserManager<ApplicationUser> userManager)
    {
        _context = context;
    
        _entitlements = entitlements;
        _userManager = userManager;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    // GET: Accounts
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsOnBudget)
            .ThenBy(a => a.Name)
            .ToListAsync();

        var onBudgetTotal = accounts.Where(a => a.IsOnBudget && a.IsActive).Sum(a => a.Balance);
        var offBudgetTotal = accounts.Where(a => !a.IsOnBudget && a.IsActive).Sum(a => a.Balance);
        var netWorth = onBudgetTotal + offBudgetTotal;

        ViewBag.OnBudgetTotal = onBudgetTotal;
        ViewBag.OffBudgetTotal = offBudgetTotal;
        ViewBag.NetWorth = netWorth;

        return View(accounts);
    }

    // GET: Accounts/Create
    public IActionResult Create()
    {
        var model = new Account
        {
            IsOnBudget = true,
            IsActive = true,
            AccountType = AccountType.Checking
        };
        
        return View(model);
    }

    // POST: Accounts/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Account account)
    {
        if (ModelState.IsValid)
        {
            var gateUser = await _userManager.GetUserAsync(User);
            var gateTier = _entitlements.GetTier(gateUser);
            var acctCount = await _context.Accounts.CountAsync(a => a.UserId == gateUser!.Id && a.IsActive);
            if (acctCount >= _entitlements.GetMaxAccounts(gateTier))
            {
                TempData["Error"] = $"Account limit reached for {_entitlements.GetPackDisplayName(gateTier)}. Upgrade dough for more accounts.";
                return RedirectToAction("Index", "Billing");
            }
            account.UserId = GetUserId();
            account.CreatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Account '{account.Name}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(account);
    }

    // GET: Accounts/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var userId = GetUserId();
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null) return NotFound();

        return View(account);
    }

    // POST: Accounts/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Account account)
    {
        if (id != account.Id) return NotFound();

        var userId = GetUserId();
        var existingAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (existingAccount == null) return NotFound();

        if (ModelState.IsValid)
        {
            existingAccount.Name = account.Name;
            existingAccount.AccountType = account.AccountType;
            existingAccount.IsOnBudget = account.IsOnBudget;
            existingAccount.Notes = account.Notes;
            existingAccount.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Account '{account.Name}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(account);
    }

    // GET: Accounts/UpdateBalance/5
    public async Task<IActionResult> UpdateBalance(int? id)
    {
        if (id == null) return NotFound();

        var userId = GetUserId();
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null) return NotFound();

        var model = new UpdateBalanceViewModel
        {
            AccountId = account.Id,
            AccountName = account.Name,
            CurrentBalance = account.Balance,
            NewBalance = account.Balance
        };

        return View(model);
    }

    // POST: Accounts/UpdateBalance/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBalance(UpdateBalanceViewModel model)
    {
        if (ModelState.IsValid)
        {
            var userId = GetUserId();
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == model.AccountId && a.UserId == userId);

            if (account == null) return NotFound();

            var adjustment = model.NewBalance - account.Balance;
            var currency = DoughDoodle.Helpers.CurrencyDisplay.Normalize((await _context.Users.FindAsync(userId))?.PreferredCurrency);
            
            if (adjustment != 0)
            {
                // Create a balance adjustment transaction
                var adjustmentTransaction = new Transaction
                {
                    Description = model.Description ?? "Balance adjustment",
                    Amount = adjustment,
                    Date = DateTime.UtcNow,
                    TransactionType = adjustment > 0 ? TransactionType.Income : TransactionType.Expense,
                    AccountId = account.Id,
                    Notes = $"Balance adjustment from {DoughDoodle.Helpers.CurrencyDisplay.Format(account.Balance, currency)} to {DoughDoodle.Helpers.CurrencyDisplay.Format(model.NewBalance, currency)}"
                };

                account.Balance = model.NewBalance;
                account.UpdatedAt = DateTime.UtcNow;

                _context.Transactions.Add(adjustmentTransaction);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Balance updated successfully. Adjustment: {DoughDoodle.Helpers.CurrencyDisplay.Format(adjustment, currency)}";
            }

            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // POST: Accounts/Close/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id, int? transferToAccountId)
    {
        var userId = GetUserId();
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null) return NotFound();

        var currency = DoughDoodle.Helpers.CurrencyDisplay.Normalize((await _context.Users.FindAsync(userId))?.PreferredCurrency);
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // If there's a balance and a transfer account specified, transfer the balance
            if (account.Balance != 0 && transferToAccountId.HasValue)
            {
                var transferToAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == transferToAccountId && a.UserId == userId);

                if (transferToAccount != null)
                {
                    // Create transfer transactions
                    var transferOutTransaction = new Transaction
                    {
                        Description = $"Transfer to {transferToAccount.Name} (account closure)",
                        Amount = -account.Balance,
                        Date = DateTime.UtcNow,
                        TransactionType = TransactionType.Transfer,
                        AccountId = account.Id,
                        TransferToAccountId = transferToAccount.Id,
                        Notes = $"Account closure transfer"
                    };

                    var transferInTransaction = new Transaction
                    {
                        Description = $"Transfer from {account.Name} (account closure)",
                        Amount = account.Balance,
                        Date = DateTime.UtcNow,
                        TransactionType = TransactionType.Transfer,
                        AccountId = transferToAccount.Id,
                        Notes = $"Account closure transfer from {account.Name}"
                    };

                    transferToAccount.Balance += account.Balance;
                    transferToAccount.UpdatedAt = DateTime.UtcNow;

                    _context.Transactions.Add(transferOutTransaction);
                    _context.Transactions.Add(transferInTransaction);
                }
            }

            // Close the account
            account.IsActive = false;
            account.Balance = 0; // Zero out the balance after transfer
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = $"Account '{account.Name}' closed successfully!";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Failed to close account. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Accounts/Reopen/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(int id)
    {
        var userId = GetUserId();
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null) return NotFound();

        account.IsActive = true;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Account '{account.Name}' reopened successfully!";
        return RedirectToAction(nameof(Index));
    }

    // GET: Accounts/Reconcile/5
    public async Task<IActionResult> Reconcile(int? id)
    {
        if (id == null) return NotFound();

        var userId = GetUserId();
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null) return NotFound();

        // Get unreconciled transactions from the last 30 days
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var transactions = await _context.Transactions
            .Where(t => t.AccountId == id && t.Date >= cutoffDate && !t.IsReconciled)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        var model = new ReconcileAccountViewModel
        {
            Account = account,
            UnreconciledTransactions = transactions,
            StatementBalance = account.Balance
        };

        return View(model);
    }

    // POST: Accounts/Reconcile/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reconcile(int id, decimal statementBalance, int[] reconciledTransactionIds)
    {
        var userId = GetUserId();
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null) return NotFound();

        var currency = DoughDoodle.Helpers.CurrencyDisplay.Normalize((await _context.Users.FindAsync(userId))?.PreferredCurrency);
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Mark selected transactions as reconciled
            var transactions = await _context.Transactions
                .Where(t => reconciledTransactionIds.Contains(t.Id) && t.AccountId == id)
                .ToListAsync();

            foreach (var trans in transactions)
            {
                trans.IsReconciled = true;
                trans.ReconciledDate = DateTime.UtcNow;
            }

            // Update account balance if different from statement
            if (account.Balance != statementBalance)
            {
                var adjustment = statementBalance - account.Balance;
                var adjustmentTransaction = new Transaction
                {
                    Description = "Reconciliation adjustment",
                    Amount = adjustment,
                    Date = DateTime.UtcNow,
                    TransactionType = adjustment > 0 ? TransactionType.Income : TransactionType.Expense,
                    AccountId = account.Id,
                    IsReconciled = true,
                    ReconciledDate = DateTime.UtcNow,
                    Notes = $"Reconciliation adjustment to match statement balance of {DoughDoodle.Helpers.CurrencyDisplay.Format(statementBalance, currency)}"
                };

                _context.Transactions.Add(adjustmentTransaction);
                account.Balance = statementBalance;
            }

            account.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = $"Account '{account.Name}' reconciled successfully!";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Reconciliation failed. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }
}

// View Models
public class UpdateBalanceViewModel
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal NewBalance { get; set; }
    public string? Description { get; set; }
}

public class ReconcileAccountViewModel
{
    public Account Account { get; set; } = null!;
    public List<Transaction> UnreconciledTransactions { get; set; } = new();
    public decimal StatementBalance { get; set; }
}