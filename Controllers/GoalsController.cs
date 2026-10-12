using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Models.ViewModels;
using System.Security.Claims;
using DoughDoodle.Services.Billing;
using Microsoft.AspNetCore.Identity;

namespace DoughDoodle.Controllers;

[Authorize]
public class GoalsController : Controller
{
    private readonly BudgetDbContext _context;
    private readonly IDoughEntitlementService _entitlements;
    private readonly UserManager<ApplicationUser> _userManager;

    public GoalsController(BudgetDbContext context, IDoughEntitlementService entitlements, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _entitlements = entitlements;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        var user = await _userManager.GetUserAsync(User);
        var tier = _entitlements.GetTier(user);
        ViewBag.MaxGoals = _entitlements.GetMaxGoals(tier);
        ViewBag.DoughTier = _entitlements.GetPackDisplayName(tier);

        var goals = await _context.FinancialGoals
            .Where(g => g.UserId == currentUserId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        var viewModel = new GoalsViewModel
        {
            Goals = goals,
            TotalGoals = goals.Count,
            ActiveGoals = goals.Count(g => g.IsActive),
            CompletedGoals = goals.Count(g => g.IsCompleted),
            TotalTargetAmount = goals.Where(g => g.IsActive).Sum(g => g.TargetAmount),
            TotalCurrentAmount = goals.Where(g => g.IsActive).Sum(g => g.CurrentAmount),
            PreferredCurrency = user?.PreferredCurrency ?? "USD"
        };

        return View(viewModel);
    }

    public IActionResult Create()
    {
        return View(new FinancialGoal());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FinancialGoal goal)
    {
        var gateUser = await _userManager.GetUserAsync(User);
        var gateTier = _entitlements.GetTier(gateUser);
        var goalCount = await _context.FinancialGoals.CountAsync(g => g.UserId == gateUser!.Id && g.IsActive);
        if (goalCount >= _entitlements.GetMaxGoals(gateTier))
        {
            TempData["Error"] = $"Goal limit reached for {_entitlements.GetPackDisplayName(gateTier)}. Upgrade dough for more goals.";
            return RedirectToAction("Index", "Billing");
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        ModelState.Remove(nameof(FinancialGoal.User));
        ModelState.Remove(nameof(FinancialGoal.UserId));

        if (ModelState.IsValid)
        {
            goal.UserId = currentUserId;
            goal.CreatedAt = DateTime.UtcNow;
            goal.UpdatedAt = DateTime.UtcNow;
            goal.IsActive = true;

            _context.FinancialGoals.Add(goal);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Financial goal created successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(goal);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var goal = await _context.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == currentUserId);

        if (goal == null) return NotFound();

        return View(goal);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FinancialGoal goal)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id != goal.Id) return NotFound();

        var existingGoal = await _context.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == currentUserId);
        
        if (existingGoal == null) return NotFound();

        if (ModelState.IsValid)
        {
            existingGoal.Name = goal.Name;
            existingGoal.Description = goal.Description;
            existingGoal.TargetAmount = goal.TargetAmount;
            existingGoal.CurrentAmount = Math.Max(0, goal.CurrentAmount);
            existingGoal.TargetDate = goal.TargetDate;
            existingGoal.GoalType = goal.GoalType;
            existingGoal.IsActive = goal.IsActive;
            existingGoal.UpdatedAt = DateTime.UtcNow;

            if (existingGoal.CurrentAmount >= existingGoal.TargetAmount && !existingGoal.IsCompleted)
            {
                existingGoal.IsCompleted = true;
                existingGoal.CompletedAt = DateTime.UtcNow;
            }
            else if (existingGoal.CurrentAmount < existingGoal.TargetAmount && existingGoal.IsCompleted)
            {
                existingGoal.IsCompleted = false;
                existingGoal.CompletedAt = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Goal updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(goal);
    }

    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var goal = await _context.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == currentUserId);

        if (goal == null) return NotFound();

        return View(goal);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProgress(int id, decimal amount)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var goal = await _context.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == currentUserId);

        if (goal == null) return NotFound();

        goal.CurrentAmount = Math.Max(0, goal.CurrentAmount + amount);
        goal.UpdatedAt = DateTime.UtcNow;

        // Check if goal is completed
        if (goal.CurrentAmount >= goal.TargetAmount && !goal.IsCompleted)
        {
            goal.IsCompleted = true;
            goal.CompletedAt = DateTime.UtcNow;
        }
        else if (goal.CurrentAmount < goal.TargetAmount && goal.IsCompleted)
        {
            goal.IsCompleted = false;
            goal.CompletedAt = null;
        }

        await _context.SaveChangesAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
            Request.Headers.Accept.Any(a => a?.Contains("application/json") == true))
        {
            return Json(new
            {
                success = true,
                currentAmount = goal.CurrentAmount,
                progressPercentage = goal.TargetAmount > 0
                    ? Math.Round((goal.CurrentAmount / goal.TargetAmount) * 100, 1)
                    : 0,
                isCompleted = goal.IsCompleted
            });
        }

        TempData["Success"] = goal.IsCompleted
            ? "Goal completed - congratulations!"
            : "Goal progress updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkComplete(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var goal = await _context.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == currentUserId);

        if (goal == null) return NotFound();

        goal.IsCompleted = true;
        goal.CompletedAt = DateTime.UtcNow;
        goal.UpdatedAt = DateTime.UtcNow;
        if (goal.CurrentAmount < goal.TargetAmount)
        {
            goal.CurrentAmount = goal.TargetAmount;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"\"{goal.Name}\" marked as complete.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var goal = await _context.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == currentUserId);

        if (goal == null) return NotFound();

        _context.FinancialGoals.Remove(goal);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Goal deleted successfully!";
        return RedirectToAction(nameof(Index));
    }
}