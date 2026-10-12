using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Services.Undo;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DoughDoodle.Controllers;

public class HomeController : Controller
{
  private readonly BudgetDbContext _db;
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly ITransactionUndoService _undo;

  public HomeController(BudgetDbContext db, UserManager<ApplicationUser> userManager, ITransactionUndoService undo) 
  { 
    _db = db; 
    _userManager = userManager;
    _undo = undo;
  }

  public async Task<IActionResult> Index(string? month = null)
  {
    if (User.Identity?.IsAuthenticated == true)
    {
      var currentUser = await _userManager.GetUserAsync(User);
      if (currentUser == null)
      {
        return RedirectToAction("Login", "Account");
      }
      if (!currentUser.OnboardingCompleted)
      {
        return RedirectToAction("Index", "Onboarding");
      }

      month ??= DateTime.Now.ToString("yyyy-MM");
      ViewBag.CurrentMonth = month;
      
      // Get monthly income for the current month
      var monthlyIncome = await _db.MonthlyIncomes
          .FirstOrDefaultAsync(mi => mi.UserId == currentUser.Id && mi.MonthYear == month);
      ViewBag.MonthlyIncome = monthlyIncome?.Amount ?? 0m;
      
      var categories = await _db.Categories
          .Where(c => c.UserId == currentUser.Id)
          .Include(c => c.Transactions)
          .Include(c => c.Budgets)
          .ToListAsync();
      
      return View(categories);
    }
    else
    {
      // Show a landing page for non-authenticated users
      return View("Landing");
    }
  }

  [Authorize]
  [HttpGet] 
  public IActionResult AddCategory() => View();

  [Authorize]
  [HttpPost]
  public async Task<IActionResult> AddCategory(string name)
  {
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null || string.IsNullOrEmpty(name)) 
      return BadRequest();
    
    _db.Categories.Add(new Category { Name = name, UserId = currentUser.Id });
    await _db.SaveChangesAsync();
    return RedirectToAction("Index");
  }

  [Authorize]
  [HttpGet]
  public async Task<IActionResult> EditCategory(int id)
  {
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null) return RedirectToAction("Login", "Account");

    var category = await _db.Categories
      .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUser.Id);
    
    if (category == null) return NotFound();
    return View(category);
  }

  [Authorize]
  [HttpPost]
  public async Task<IActionResult> EditCategory(int id, string name)
  {
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null) return RedirectToAction("Login", "Account");

    var category = await _db.Categories
      .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUser.Id);
    
    if (category == null) return NotFound();
    
    category.Name = name;
    await _db.SaveChangesAsync();
    return RedirectToAction("Index");
  }

  [Authorize]
  [HttpPost]
  public async Task<IActionResult> SetMonthlyBudget(int categoryId, string monthYear, decimal amount)
  {
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null || amount < 0) return BadRequest();

    var category = await _db.Categories
      .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == currentUser.Id);
    
    if (category == null) return BadRequest();

    var budget = await _db.MonthlyBudgets
      .FirstOrDefaultAsync(b => b.CategoryId == categoryId && b.MonthYear == monthYear);
    
    if (budget == null)
    {
      budget = new MonthlyBudget { CategoryId = categoryId, MonthYear = monthYear, Amount = amount };
      _db.MonthlyBudgets.Add(budget);
    }
    else 
    { 
      budget.Amount = amount; 
    }
    
    await _db.SaveChangesAsync();
    return RedirectToAction("Index", new { month = monthYear });
  }

  [Authorize]
  [HttpGet]
  public async Task<IActionResult> AddTransaction()
  {
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null) return RedirectToAction("Login", "Account");

    ViewBag.Categories = await _db.Categories
      .Where(c => c.UserId == currentUser.Id)
      .ToListAsync();
    
    return View();
  }

  [Authorize]
  [HttpPost]
  public async Task<IActionResult> AddTransaction(string description, decimal amount, int categoryId, DateTime date)
  {
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null || string.IsNullOrEmpty(description) || amount <= 0 || categoryId <= 0) 
      return BadRequest();

    var category = await _db.Categories
      .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == currentUser.Id);
    
    if (category == null) return BadRequest();

    var created = new Transaction 
    { 
      Description = description, 
      Amount = amount, 
      CategoryId = categoryId, 
      Date = date 
    };
    _db.Transactions.Add(created);
    await _db.SaveChangesAsync();
    await _undo.RecordAsync(currentUser.Id, "CREATE", created, created);
    return RedirectToAction("Index");
  }

  [Authorize]
  [HttpGet]
  public async Task<IActionResult> EditTransaction(int id)
  {
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null) return RedirectToAction("Login", "Account");

    var transaction = await _db.Transactions
      .Include(t => t.Category)
      .FirstOrDefaultAsync(t => t.Id == id && t.Category!.UserId == currentUser.Id);
    
    if (transaction == null) return NotFound();
    
    ViewBag.Categories = await _db.Categories
      .Where(c => c.UserId == currentUser.Id)
      .ToListAsync();
    
    return View(transaction);
  }

  [Authorize]
  [HttpPost]
  public async Task<IActionResult> EditTransaction(int id, string description, decimal amount, int categoryId, DateTime date)
  {
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null) return RedirectToAction("Login", "Account");

    var transaction = await _db.Transactions
      .Include(t => t.Category)
      .FirstOrDefaultAsync(t => t.Id == id && t.Category!.UserId == currentUser.Id);
    
    if (transaction == null) return NotFound();
    
    var category = await _db.Categories
      .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == currentUser.Id);
    
    if (category == null) return BadRequest();

    var before = new Transaction
    {
      Id = transaction.Id,
      Description = transaction.Description,
      Amount = transaction.Amount,
      CategoryId = transaction.CategoryId,
      Date = transaction.Date,
      TransactionType = transaction.TransactionType,
      AccountId = transaction.AccountId,
      Notes = transaction.Notes
    };
    transaction.Description = description;
    transaction.Amount = amount;
    transaction.CategoryId = categoryId;
    transaction.Date = date;
    await _db.SaveChangesAsync();
    await _undo.RecordAsync(currentUser.Id, "UPDATE", before, transaction);
    return RedirectToAction("Index");
  }

  [Authorize]
  [HttpPost]
  public async Task<IActionResult> SetMonthlyIncome(string monthYear, decimal amount)
  {
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null || amount < 0) return BadRequest();

    var monthlyIncome = await _db.MonthlyIncomes
        .FirstOrDefaultAsync(mi => mi.UserId == currentUser.Id && mi.MonthYear == monthYear);
    
    if (monthlyIncome == null)
    {
      monthlyIncome = new MonthlyIncome 
      { 
        UserId = currentUser.Id, 
        MonthYear = monthYear, 
        Amount = amount 
      };
      _db.MonthlyIncomes.Add(monthlyIncome);
    }
    else 
    { 
      monthlyIncome.Amount = amount;
      monthlyIncome.UpdatedAt = DateTime.UtcNow;
    }
    
    await _db.SaveChangesAsync();
    return RedirectToAction("Index", new { month = monthYear });
  }

  [Authorize]
  [HttpPost]
  public async Task<IActionResult> DeleteCategory(int id)
  {
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null) return RedirectToAction("Login", "Account");

    var category = await _db.Categories
      .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUser.Id);
    
    if (category == null) return NotFound();
    
    _db.Categories.Remove(category);
    await _db.SaveChangesAsync();
    return RedirectToAction("Index");
  }

  [Authorize]
  [HttpPost]
  public async Task<IActionResult> DeleteTransaction(int id)
  {
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null) return RedirectToAction("Login", "Account");

    var transaction = await _db.Transactions
      .Include(t => t.Category)
      .FirstOrDefaultAsync(t => t.Id == id && t.Category!.UserId == currentUser.Id);
    
    if (transaction == null) return NotFound();
    
    await _undo.RecordAsync(currentUser.Id, "DELETE", transaction, null);
    _db.Transactions.Remove(transaction);
    await _db.SaveChangesAsync();
    return RedirectToAction("Index");
  }

  public IActionResult Privacy()
  {
    return View();
  }

  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
  public IActionResult Error()
  {
    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
  }
}