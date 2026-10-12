using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Models.ViewModels;
using System.Security.Claims;

namespace DoughDoodle.Controllers;

[Authorize]
public class RecurringController : Controller
{
    private readonly BudgetDbContext _context;

    public RecurringController(BudgetDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        var scheduledTransactions = await _context.ScheduledTransactions
            .Where(st => st.UserId == currentUserId)
            .Include(st => st.Category)
            .Include(st => st.Account)
            .OrderBy(st => st.NextDueDate)
            .ToListAsync();

        var dueTransactions = scheduledTransactions
            .Where(st => st.IsActive && st.NextDueDate <= DateTime.Now.AddDays(7))
            .ToList();

        var categories = await _context.Categories
            .Where(c => c.UserId == currentUserId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var accounts = await _context.Accounts
            .Where(a => a.UserId == currentUserId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        var viewModel = new RecurringTransactionsViewModel
        {
            ScheduledTransactions = scheduledTransactions,
            DueTransactions = dueTransactions,
            Categories = categories,
            Accounts = accounts,
            TotalScheduled = scheduledTransactions.Count,
            ActiveScheduled = scheduledTransactions.Count(st => st.IsActive),
            DueCount = dueTransactions.Count
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Create()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == currentUserId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Accounts = await _context.Accounts
            .Where(a => a.UserId == currentUserId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return View(new ScheduledTransaction
        {
            StartDate = DateTime.Today,
            NextDueDate = DateTime.Today,
            RecurrenceInterval = 1,
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ScheduledTransaction scheduledTransaction)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        if (ModelState.IsValid)
        {
            scheduledTransaction.UserId = currentUserId;
            scheduledTransaction.CreatedAt = DateTime.UtcNow;
            scheduledTransaction.NextDueDate = CalculateNextDueDate(scheduledTransaction.StartDate, scheduledTransaction.RecurrenceType, scheduledTransaction.RecurrenceInterval);

            _context.ScheduledTransactions.Add(scheduledTransaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Scheduled transaction created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Reload dropdowns if validation fails
        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == currentUserId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Accounts = await _context.Accounts
            .Where(a => a.UserId == currentUserId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return View(scheduledTransaction);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var scheduledTransaction = await _context.ScheduledTransactions
            .Include(st => st.Category)
            .Include(st => st.Account)
            .FirstOrDefaultAsync(st => st.Id == id && st.UserId == currentUserId);

        if (scheduledTransaction == null) return NotFound();

        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == currentUserId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Accounts = await _context.Accounts
            .Where(a => a.UserId == currentUserId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return View(scheduledTransaction);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ScheduledTransaction scheduledTransaction)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id != scheduledTransaction.Id) return NotFound();

        var existingTransaction = await _context.ScheduledTransactions
            .FirstOrDefaultAsync(st => st.Id == id && st.UserId == currentUserId);

        if (existingTransaction == null) return NotFound();

        if (ModelState.IsValid)
        {
            existingTransaction.Description = scheduledTransaction.Description;
            existingTransaction.Amount = scheduledTransaction.Amount;
            existingTransaction.TransactionType = scheduledTransaction.TransactionType;
            existingTransaction.CategoryId = scheduledTransaction.CategoryId;
            existingTransaction.AccountId = scheduledTransaction.AccountId;
            existingTransaction.RecurrenceType = scheduledTransaction.RecurrenceType;
            existingTransaction.RecurrenceInterval = scheduledTransaction.RecurrenceInterval;
            existingTransaction.StartDate = scheduledTransaction.StartDate;
            existingTransaction.EndDate = scheduledTransaction.EndDate;
            existingTransaction.IsActive = scheduledTransaction.IsActive;
            existingTransaction.AutoExecute = scheduledTransaction.AutoExecute;

            // Recalculate next due date
            existingTransaction.NextDueDate = CalculateNextDueDate(scheduledTransaction.StartDate, scheduledTransaction.RecurrenceType, scheduledTransaction.RecurrenceInterval);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Scheduled transaction updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Reload dropdowns if validation fails
        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == currentUserId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Accounts = await _context.Accounts
            .Where(a => a.UserId == currentUserId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return View(scheduledTransaction);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var scheduledTransaction = await _context.ScheduledTransactions
            .Include(st => st.Category)
            .FirstOrDefaultAsync(st => st.Id == id && st.UserId == currentUserId);

        if (scheduledTransaction == null || !scheduledTransaction.IsActive)
        {
            TempData["Error"] = "Scheduled transaction not found or inactive.";
            return RedirectToAction(nameof(Index));
        }

        // Create the actual transaction
        var transaction = new Transaction
        {
            Description = scheduledTransaction.Description,
            Amount = scheduledTransaction.Amount,
            Date = DateTime.Today,
            TransactionType = scheduledTransaction.TransactionType,
            CategoryId = scheduledTransaction.CategoryId,
            AccountId = scheduledTransaction.AccountId,
            ScheduledTransactionId = scheduledTransaction.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);

        // Update the scheduled transaction
        scheduledTransaction.LastExecuted = DateTime.UtcNow;
        scheduledTransaction.NextDueDate = CalculateNextDueDate(scheduledTransaction.NextDueDate, scheduledTransaction.RecurrenceType, scheduledTransaction.RecurrenceInterval);

        // Check if we should deactivate (if end date reached)
        if (scheduledTransaction.EndDate.HasValue && scheduledTransaction.NextDueDate > scheduledTransaction.EndDate.Value)
        {
            scheduledTransaction.IsActive = false;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Transaction executed successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Skip(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var scheduledTransaction = await _context.ScheduledTransactions
            .FirstOrDefaultAsync(st => st.Id == id && st.UserId == currentUserId);

        if (scheduledTransaction == null || !scheduledTransaction.IsActive)
        {
            TempData["Error"] = "Scheduled transaction not found or inactive.";
            return RedirectToAction(nameof(Index));
        }

        // Update next due date without creating a transaction
        scheduledTransaction.NextDueDate = CalculateNextDueDate(scheduledTransaction.NextDueDate, scheduledTransaction.RecurrenceType, scheduledTransaction.RecurrenceInterval);

        // Check if we should deactivate (if end date reached)
        if (scheduledTransaction.EndDate.HasValue && scheduledTransaction.NextDueDate > scheduledTransaction.EndDate.Value)
        {
            scheduledTransaction.IsActive = false;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Occurrence skipped.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var scheduledTransaction = await _context.ScheduledTransactions
            .FirstOrDefaultAsync(st => st.Id == id && st.UserId == currentUserId);

        if (scheduledTransaction == null) return NotFound();

        _context.ScheduledTransactions.Remove(scheduledTransaction);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Scheduled transaction deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, string? returnAction)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var scheduledTransaction = await _context.ScheduledTransactions
            .FirstOrDefaultAsync(st => st.Id == id && st.UserId == currentUserId);

        if (scheduledTransaction == null)
        {
            TempData["Error"] = "Scheduled transaction not found.";
            return RedirectToAction(nameof(Index));
        }

        scheduledTransaction.IsActive = !scheduledTransaction.IsActive;
        await _context.SaveChangesAsync();

        TempData["Success"] = scheduledTransaction.IsActive ? "Recurring transaction activated." : "Recurring transaction paused.";
        if (string.Equals(returnAction, nameof(SubscriptionHunter), StringComparison.Ordinal))
            return RedirectToAction(nameof(SubscriptionHunter));
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Surfaces recurring bills &amp; subscription-like schedules — hunter polish.</summary>
    [HttpGet]
    public async Task<IActionResult> SubscriptionHunter()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        // SQLite cannot ORDER BY decimal; sort in memory after the claim-scoped query.
        var scheduled = (await _context.ScheduledTransactions
            .Where(st => st.UserId == currentUserId && st.IsActive)
            .Include(st => st.Category)
            .Include(st => st.Account)
            .ToListAsync())
            .OrderByDescending(st => st.Amount)
            .ToList();

        var keywords = new[] { "netflix", "spotify", "disney", "apple", "google", "amazon", "prime", "subscription", "membership", "gym", "icloud", "adobe", "dropbox", "microsoft", "youtube", "hulu", "paramount" };
        var suspects = scheduled
            .Where(st =>
                st.RecurrenceType is RecurrenceType.Monthly or RecurrenceType.Annually or RecurrenceType.Quarterly
                || keywords.Any(k => (st.Description ?? "").Contains(k, StringComparison.OrdinalIgnoreCase)
                                  || (st.Category?.Name ?? "").Contains(k, StringComparison.OrdinalIgnoreCase)
                                  || (st.Category?.Name ?? "").Contains("Subscription", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var monthlyBurn = suspects.Sum(st => st.RecurrenceType switch
        {
            RecurrenceType.Annually => st.Amount / 12m,
            RecurrenceType.Quarterly => st.Amount / 3m,
            RecurrenceType.SemiAnnually => st.Amount / 6m,
            RecurrenceType.Weekly => st.Amount * 52m / 12m,
            RecurrenceType.Biweekly => st.Amount * 26m / 12m,
            _ => st.Amount
        });

        ViewBag.MonthlyBurn = decimal.Round(monthlyBurn, 2);
        ViewBag.AnnualBurn = decimal.Round(monthlyBurn * 12m, 2);
        ViewBag.AllScheduled = scheduled;
        return View(suspects);
    }

    private static DateTime CalculateNextDueDate(DateTime currentDate, RecurrenceType recurrenceType, int interval)
    {
        return recurrenceType switch
        {
            RecurrenceType.Daily => currentDate.AddDays(interval),
            RecurrenceType.Weekly => currentDate.AddDays(7 * interval),
            RecurrenceType.Biweekly => currentDate.AddDays(14 * interval),
            RecurrenceType.Monthly => currentDate.AddMonths(interval),
            RecurrenceType.Quarterly => currentDate.AddMonths(3 * interval),
            RecurrenceType.SemiAnnually => currentDate.AddMonths(6 * interval),
            RecurrenceType.Annually => currentDate.AddYears(interval),
            _ => currentDate.AddMonths(interval)
        };
    }
}