using System.Security.Claims;
using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Controllers;

[Authorize]
public class TeamBudgetsController : Controller
{
    private readonly BudgetDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public TeamBudgetsController(BudgetDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        var ownerId = user.UserType == UserType.MainAccount ? user.Id : user.ParentUserId ?? user.Id;

        var shared = await _db.SharedEnvelopes
            .Include(s => s.Category)
            .Where(s => s.HouseholdOwnerId == ownerId)
            .ToListAsync();

        var categories = await _db.Categories.Where(c => c.UserId == ownerId).OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = categories;
        ViewBag.IsOwner = user.UserType == UserType.MainAccount || user.HouseholdRole == HouseholdRole.HouseholdAdmin || user.IsAdmin;
        return View(shared);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Share(int categoryId, bool allowSpend = true, bool allowEdit = false)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        if (user.UserType != UserType.MainAccount && user.HouseholdRole != HouseholdRole.HouseholdAdmin && !user.IsAdmin)
        {
            TempData["Error"] = "Only household admins can share envelopes.";
            return RedirectToAction(nameof(Index));
        }
        var ownerId = user.UserType == UserType.MainAccount ? user.Id : user.ParentUserId ?? user.Id;
        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == ownerId);
        if (cat == null) return NotFound();

        if (!await _db.SharedEnvelopes.AnyAsync(s => s.CategoryId == categoryId && s.HouseholdOwnerId == ownerId))
        {
            _db.SharedEnvelopes.Add(new SharedEnvelope
            {
                CategoryId = categoryId,
                HouseholdOwnerId = ownerId,
                AllowMemberSpend = allowSpend,
                AllowMemberEditBudget = allowEdit
            });
            cat.IsSharedWithHousehold = true;
            await _db.SaveChangesAsync();
        }
        TempData["Success"] = $"Shared envelope: {cat.Name}";
        return RedirectToAction(nameof(Index));
    }
}
