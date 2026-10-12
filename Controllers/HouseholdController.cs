using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Services.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Controllers;

/// <summary>Productized household / parent-sub sharing (couples & family).</summary>
[Authorize]
public class HouseholdController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly BudgetDbContext _db;
    private readonly IDoughEntitlementService _entitlements;

    public HouseholdController(
        UserManager<ApplicationUser> userManager,
        BudgetDbContext db,
        IDoughEntitlementService entitlements)
    {
        _userManager = userManager;
        _db = db;
        _entitlements = entitlements;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var tier = _entitlements.GetTier(user);
        if (!_entitlements.CanUseHouseholdSharing(tier) && !user.IsAdmin)
        {
            ViewBag.UpgradeRequired = true;
            ViewBag.RequiredTier = "Bankroll";
        }

        var members = await _db.Users
            .Where(u => u.ParentUserId == user.Id || u.Id == user.Id)
            .OrderBy(u => u.UserType)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        ViewBag.Tier = tier;
        ViewBag.SubAccountLimit = _entitlements.GetSubAccountLimit(tier);
        ViewBag.CanAdd = user.UserType == UserType.MainAccount
            && (user.IsAdmin || _entitlements.CanUseHouseholdSharing(tier))
            && members.Count(m => m.UserType == UserType.SubAccount && m.IsActive) < _entitlements.GetSubAccountLimit(tier);

        return View(members);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(string email, string firstName, string lastName, string password)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        if (user.UserType != UserType.MainAccount)
        {
            TempData["ErrorMessage"] = "Only the household owner (main account holder) can invite members.";
            return RedirectToAction(nameof(Index));
        }

        var tier = _entitlements.GetTier(user);
        if (!_entitlements.CanUseHouseholdSharing(tier) && !user.IsAdmin)
        {
            TempData["ErrorMessage"] = "Household sharing unlocks on Bankroll and above.";
            return RedirectToAction("Index", "Billing");
        }

        var activeSubs = await _db.Users.CountAsync(u => u.ParentUserId == user.Id && u.IsActive);
        if (activeSubs >= _entitlements.GetSubAccountLimit(tier) && !user.IsAdmin)
        {
            TempData["ErrorMessage"] = "You've reached the household seat limit for your dough tier.";
            return RedirectToAction(nameof(Index));
        }

        var member = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            UserType = UserType.SubAccount,
            ParentUserId = user.Id,
            EmailConfirmed = true,
            DoughTier = DoughTier.Pocket,
            SubscriptionPlan = SubscriptionPlan.Pocket,
            OnboardingCompleted = false
        };

        var result = await _userManager.CreateAsync(member, password);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = $"{firstName} joined the household.";
        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string memberId, HouseholdRole role)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        if (user.UserType != UserType.MainAccount && !user.IsAdmin)
        {
            TempData["ErrorMessage"] = "Only the household owner can change roles.";
            return RedirectToAction(nameof(Index));
        }
        var member = await _db.Users.FirstOrDefaultAsync(u => u.Id == memberId && u.ParentUserId == user.Id);
        if (member == null) return NotFound();
        member.HouseholdRole = role;
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"{member.FirstName} is now {role}.";
        return RedirectToAction(nameof(Index));
    }
}
