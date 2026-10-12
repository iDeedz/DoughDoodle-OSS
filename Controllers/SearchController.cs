using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Models.ViewModels;
using System.Security.Claims;

namespace DoughDoodle.Controllers;

[Authorize]
public class SearchController : Controller
{
    private readonly BudgetDbContext _context;

    public SearchController(BudgetDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(SearchViewModel model)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        // Load categories and accounts for filters
        model.Categories = await _context.Categories
            .Where(c => c.UserId == currentUserId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        model.Accounts = await _context.Accounts
            .Where(a => a.UserId == currentUserId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        if (!string.IsNullOrEmpty(model.Query) || HasFilters(model))
        {
            var query = _context.Transactions
                .Where(t => t.Category!.UserId == currentUserId)
                .Include(t => t.Category)
                .Include(t => t.Account)
                .AsQueryable();

            // Apply search query
            if (!string.IsNullOrEmpty(model.Query))
            {
                var searchTerm = model.Query.ToLower();
                query = query.Where(t => t.Description.ToLower().Contains(searchTerm) ||
                                       (t.Category != null && t.Category.Name.ToLower().Contains(searchTerm)) ||
                                       (t.Account != null && t.Account.Name.ToLower().Contains(searchTerm)));
            }

            // Apply filters
            if (model.StartDate.HasValue)
                query = query.Where(t => t.Date >= model.StartDate.Value);

            if (model.EndDate.HasValue)
                query = query.Where(t => t.Date <= model.EndDate.Value);

            if (!string.IsNullOrEmpty(model.CategoryFilter))
                query = query.Where(t => t.Category != null && t.Category.Name == model.CategoryFilter);

            if (!string.IsNullOrEmpty(model.AccountFilter))
                query = query.Where(t => t.Account != null && t.Account.Name == model.AccountFilter);

            if (model.TypeFilter.HasValue)
                query = query.Where(t => t.TransactionType == model.TypeFilter.Value);

            if (model.MinAmount.HasValue)
                query = query.Where(t => t.Amount >= model.MinAmount.Value);

            if (model.MaxAmount.HasValue)
                query = query.Where(t => t.Amount <= model.MaxAmount.Value);

            // Get total count for pagination
            model.TotalResults = await query.CountAsync();

            // Apply pagination and ordering
            model.Results = await query
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((model.CurrentPage - 1) * model.PageSize)
                .Take(model.PageSize)
                .ToListAsync();
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> QuickSearch(string term)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null || string.IsNullOrEmpty(term))
        {
            return Json(new { results = new List<object>() });
        }

        var searchTerm = term.ToLower();
        var limit = 10;
        var currentUser = await _context.Users.FindAsync(currentUserId);
        var currencySymbol = DoughDoodle.Helpers.CurrencyDisplay.Symbol(currentUser?.PreferredCurrency);

        // Search transactions
        var transactions = await _context.Transactions
            .Where(t => t.Category!.UserId == currentUserId &&
                       t.Description.ToLower().Contains(searchTerm))
            .Include(t => t.Category)
            .Include(t => t.Account)
            .OrderByDescending(t => t.Date)
            .Take(limit)
            .Select(t => new
            {
                type = "transaction",
                id = t.Id,
                title = t.Description,
                subtitle = $"{t.Category!.Name} • {currencySymbol}{t.Amount:F2} • {t.Date:MMM dd}",
                url = Url.Action("EditTransaction", "Home", new { id = t.Id })
            })
            .ToListAsync();

        // Search categories
        var categories = await _context.Categories
            .Where(c => c.UserId == currentUserId &&
                       c.Name.ToLower().Contains(searchTerm))
            .Take(limit)
            .Select(c => new
            {
                type = "category",
                id = c.Id,
                title = c.Name,
                subtitle = "Budget Category",
                url = Url.Action("EditCategory", "Home", new { id = c.Id })
            })
            .ToListAsync();

        // Search accounts
        var accounts = await _context.Accounts
            .Where(a => a.UserId == currentUserId &&
                       a.Name.ToLower().Contains(searchTerm))
            .Take(limit)
            .Select(a => new
            {
                type = "account",
                id = a.Id,
                title = a.Name,
                subtitle = $"{a.AccountType} • {currencySymbol}{a.Balance:F2}",
                url = Url.Action("Index", "Accounts")
            })
            .ToListAsync();

        var results = new List<object>();
        results.AddRange(transactions);
        results.AddRange(categories);
        results.AddRange(accounts);

        return Json(new { results = results.Take(limit) });
    }

    [HttpGet]
    public async Task<IActionResult> Suggestions(string field, string term)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null || string.IsNullOrEmpty(term))
        {
            return Json(new List<string>());
        }

        var searchTerm = term.ToLower();
        var suggestions = new List<string>();

        switch (field.ToLower())
        {
            case "description":
                suggestions = await _context.Transactions
                    .Where(t => t.Category!.UserId == currentUserId &&
                               t.Description.ToLower().Contains(searchTerm))
                    .Select(t => t.Description)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();
                break;

            case "category":
                suggestions = await _context.Categories
                    .Where(c => c.UserId == currentUserId &&
                               c.Name.ToLower().Contains(searchTerm))
                    .Select(c => c.Name)
                    .Take(10)
                    .ToListAsync();
                break;

            case "account":
                suggestions = await _context.Accounts
                    .Where(a => a.UserId == currentUserId &&
                               a.Name.ToLower().Contains(searchTerm))
                    .Select(a => a.Name)
                    .Take(10)
                    .ToListAsync();
                break;
        }

        return Json(suggestions);
    }

    private static bool HasFilters(SearchViewModel model)
    {
        return model.StartDate.HasValue ||
               model.EndDate.HasValue ||
               !string.IsNullOrEmpty(model.CategoryFilter) ||
               !string.IsNullOrEmpty(model.AccountFilter) ||
               model.TypeFilter.HasValue ||
               model.MinAmount.HasValue ||
               model.MaxAmount.HasValue;
    }
}