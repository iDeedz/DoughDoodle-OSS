using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Services.Billing;

namespace DoughDoodle.Controllers;

[Authorize]
public class InvestmentsController : Controller
{
    private readonly BudgetDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDoughEntitlementService _entitlements;

    public InvestmentsController(BudgetDbContext db, UserManager<ApplicationUser> userManager, IDoughEntitlementService entitlements)
    {
        _db = db;
        _userManager = userManager;
        _entitlements = entitlements;
    }

    // GET: Investments
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var invTier = _entitlements.GetTier(currentUser);
        if (!_entitlements.CanUseInvestments(invTier) && !currentUser.IsAdmin)
        {
            TempData["ErrorMessage"] = "Investments unlock on Vault and above.";
            return RedirectToAction("Index", "Billing");
        }

        var accounts = await _db.InvestmentAccounts
            .Where(a => a.UserId == currentUser.Id && a.IsActive)
            .Include(a => a.Holdings)
            .OrderBy(a => a.Name)
            .ToListAsync();

        var totalValue = accounts.Sum(a => a.CurrentValue);
        var totalCost = accounts.Sum(a => a.CostBasis);
        var totalGainLoss = totalValue - totalCost;

        ViewBag.TotalValue = totalValue;
        ViewBag.TotalCost = totalCost;
        ViewBag.TotalGainLoss = totalGainLoss;
        ViewBag.TotalGainLossPercentage = totalCost > 0 ? Math.Round((totalGainLoss / totalCost) * 100, 2) : 0;

        return View(accounts);
    }

    // GET: Investments/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Investments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InvestmentAccount account)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        account.UserId = currentUser.Id;
        account.CreatedAt = DateTime.UtcNow;
        account.LastUpdated = DateTime.UtcNow;
        account.IsActive = true;

        _db.InvestmentAccounts.Add(account);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Investment account created successfully!";
        return RedirectToAction(nameof(Index));
    }

    // GET: Investments/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var account = await _db.InvestmentAccounts
            .Include(a => a.Holdings)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == currentUser.Id);

        if (account == null)
            return NotFound();

        return View(account);
    }

    // POST: Investments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InvestmentAccount account)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var existingAccount = await _db.InvestmentAccounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == currentUser.Id);

        if (existingAccount == null)
            return NotFound();

        existingAccount.Name = account.Name;
        existingAccount.Description = account.Description;
        existingAccount.InvestmentType = account.InvestmentType;
        existingAccount.CurrentValue = account.CurrentValue;
        existingAccount.CostBasis = account.CostBasis;
        existingAccount.AccountNumber = account.AccountNumber;
        existingAccount.InstitutionName = account.InstitutionName;
        existingAccount.Notes = account.Notes;
        existingAccount.LastUpdated = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Investment account updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Investments/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var account = await _db.InvestmentAccounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == currentUser.Id);

        if (account == null)
            return NotFound();

        account.IsActive = false;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Investment account deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    // GET: Investments/AddHolding/5
    public async Task<IActionResult> AddHolding(int accountId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var account = await _db.InvestmentAccounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == currentUser.Id);

        if (account == null)
            return NotFound();

        ViewBag.AccountId = accountId;
        ViewBag.AccountName = account.Name;

        return View();
    }

    // POST: Investments/AddHolding
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddHolding(PortfolioHolding holding)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var account = await _db.InvestmentAccounts
            .FirstOrDefaultAsync(a => a.Id == holding.InvestmentAccountId && a.UserId == currentUser.Id);

        if (account == null)
            return NotFound();

        holding.LastPriceUpdate = DateTime.UtcNow;
        _db.PortfolioHoldings.Add(holding);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Holding added successfully!";
        return RedirectToAction(nameof(Edit), new { id = holding.InvestmentAccountId });
    }

    // POST: Investments/DeleteHolding/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHolding(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login", "Account");

        var holding = await _db.PortfolioHoldings
            .Include(h => h.InvestmentAccount)
            .FirstOrDefaultAsync(h => h.Id == id && h.InvestmentAccount.UserId == currentUser.Id);

        if (holding == null)
            return NotFound();

        var accountId = holding.InvestmentAccountId;
        _db.PortfolioHoldings.Remove(holding);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Holding removed successfully!";
        return RedirectToAction(nameof(Edit), new { id = accountId });
    }
}
