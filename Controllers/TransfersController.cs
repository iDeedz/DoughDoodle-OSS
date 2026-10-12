using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;

namespace DoughDoodle.Controllers;

[Authorize]
public class TransfersController : Controller
{
    private readonly BudgetDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public TransfersController(BudgetDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // GET: Transfers
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var transfers = await _db.ScheduledTransfers
            .Where(t => t.UserId == currentUser.Id)
            .Include(t => t.FromCategory)
            .Include(t => t.ToCategory)
            .OrderBy(t => t.NextTransferDate)
            .ToListAsync();

        return View(transfers);
    }

    // GET: Transfers/Create
    public async Task<IActionResult> Create()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var categories = await _db.Categories
            .Where(c => c.UserId == currentUser.Id)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = categories;
        return View();
    }

    // POST: Transfers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ScheduledTransfer transfer)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        transfer.UserId = currentUser.Id;
        transfer.CreatedAt = DateTime.UtcNow;
        transfer.IsActive = true;
        transfer.TransfersCompleted = 0;

        _db.ScheduledTransfers.Add(transfer);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Scheduled transfer created successfully!";
        return RedirectToAction(nameof(Index));
    }

    // GET: Transfers/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var transfer = await _db.ScheduledTransfers
            .Include(t => t.FromCategory)
            .Include(t => t.ToCategory)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUser.Id);

        if (transfer == null)
            return NotFound();

        var categories = await _db.Categories
            .Where(c => c.UserId == currentUser.Id)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = categories;
        return View(transfer);
    }

    // POST: Transfers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ScheduledTransfer transfer)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var existingTransfer = await _db.ScheduledTransfers
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUser.Id);

        if (existingTransfer == null)
            return NotFound();

        existingTransfer.Name = transfer.Name;
        existingTransfer.Description = transfer.Description;
        existingTransfer.Amount = transfer.Amount;
        existingTransfer.FromCategoryId = transfer.FromCategoryId;
        existingTransfer.ToCategoryId = transfer.ToCategoryId;
        existingTransfer.Frequency = transfer.Frequency;
        existingTransfer.NextTransferDate = transfer.NextTransferDate;
        existingTransfer.IsActive = transfer.IsActive;
        existingTransfer.AutoExecute = transfer.AutoExecute;
        existingTransfer.EndDate = transfer.EndDate;
        existingTransfer.MaxTransfers = transfer.MaxTransfers;
        existingTransfer.Notes = transfer.Notes;

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Scheduled transfer updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Transfers/Execute/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var transfer = await _db.ScheduledTransfers
            .Include(t => t.FromCategory)
            .Include(t => t.ToCategory)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUser.Id);

        if (transfer == null)
            return NotFound();

        if (!transfer.IsActive)
        {
            TempData["ErrorMessage"] = "Cannot execute inactive transfer!";
            return RedirectToAction(nameof(Index));
        }

        // Execute the transfer
        transfer.LastTransferDate = DateTime.UtcNow;
        transfer.TransfersCompleted++;

        // Calculate next transfer date based on frequency
        transfer.NextTransferDate = CalculateNextTransferDate(transfer.NextTransferDate, transfer.Frequency);

        // Check if we've reached max transfers or end date
        if ((transfer.MaxTransfers.HasValue && transfer.TransfersCompleted >= transfer.MaxTransfers) ||
            (transfer.EndDate.HasValue && DateTime.UtcNow >= transfer.EndDate))
        {
            transfer.IsActive = false;
        }

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Transfer of {DoughDoodle.Helpers.CurrencyDisplay.Format(transfer.Amount, currentUser.PreferredCurrency)} executed successfully!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Transfers/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var transfer = await _db.ScheduledTransfers
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUser.Id);

        if (transfer == null)
            return NotFound();

        _db.ScheduledTransfers.Remove(transfer);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Scheduled transfer deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    private DateTime CalculateNextTransferDate(DateTime currentDate, TransferFrequency frequency)
    {
        return frequency switch
        {
            TransferFrequency.Daily => currentDate.AddDays(1),
            TransferFrequency.Weekly => currentDate.AddDays(7),
            TransferFrequency.BiWeekly => currentDate.AddDays(14),
            TransferFrequency.Monthly => currentDate.AddMonths(1),
            TransferFrequency.Quarterly => currentDate.AddMonths(3),
            TransferFrequency.Annually => currentDate.AddYears(1),
            _ => currentDate
        };
    }
}
