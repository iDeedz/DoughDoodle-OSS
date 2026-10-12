using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Models.ViewModels;
using DoughDoodle.Services.Billing;

namespace DoughDoodle.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly BudgetDbContext _context;
    private readonly IDoughEntitlementService _entitlements;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        BudgetDbContext context,
        IDoughEntitlementService entitlements)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _entitlements = entitlements;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserType = model.UserType,
                ParentUserId = model.ParentUserId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Validate sub-account creation limits
            if (user.UserType == UserType.SubAccount && !string.IsNullOrEmpty(model.ParentUserId))
            {
                var parentUser = await _userManager.FindByIdAsync(model.ParentUserId);
                if (parentUser == null || !parentUser.CanCreateSubAccount)
                {
                    ModelState.AddModelError(string.Empty, "The parent account has reached the sub-account limit or is invalid.");
                    return View(model);
                }
            }

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Envelopes are planted in Onboarding QuickStart from starterSet.
                // Seeding here made "I'll add my own later" a no-op.
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && user.IsActive)
                {
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    await _signInManager.SignOutAsync();
                    ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact support.");
                    return View(model);
                }
            }
            
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> UserManagement()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return RedirectToAction("Login");

        var subAccounts = await _context.Users
            .Where(u => u.ParentUserId == currentUser.Id && u.IsActive)
            .ToListAsync();

        var tier = _entitlements.GetTier(currentUser);
        var viewModel = new UserManagementViewModel
        {
            CurrentUser = currentUser,
            SubAccounts = subAccounts,
            CanCreateSubAccount = currentUser.CanCreateSubAccount,
            SubAccountLimit = currentUser.SubAccountLimit,
            UsedSubAccounts = subAccounts.Count,
            PlanDisplayName = _entitlements.GetPackDisplayName(tier),
            PreferredCurrency = currentUser.PreferredCurrency ?? "USD"
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize]
    public IActionResult CreateSubAccount()
    {
        var currentUser = _userManager.GetUserAsync(User).Result;
        if (currentUser == null || !currentUser.CanCreateSubAccount)
        {
            return RedirectToAction("UserManagement");
        }

        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubAccount(CreateSubAccountViewModel model)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null || !currentUser.CanCreateSubAccount)
        {
            return RedirectToAction("UserManagement");
        }

        if (ModelState.IsValid)
        {
            var subAccount = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserType = UserType.SubAccount,
                ParentUserId = currentUser.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(subAccount, model.Password);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Sub-account '{subAccount.FullName}' has been created successfully.";
                return RedirectToAction("UserManagement");
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateSubAccount(string subAccountId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var subAccount = await _context.Users.FindAsync(subAccountId);

        if (currentUser == null || subAccount == null || subAccount.ParentUserId != currentUser.Id)
        {
            return NotFound();
        }

        subAccount.IsActive = false;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Sub-account '{subAccount.FullName}' has been deactivated.";
        return RedirectToAction("UserManagement");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ViewSubAccountBudget(string subAccountId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var subAccount = await _context.Users
            .Include(u => u.Categories)
                .ThenInclude(c => c.Budgets)
            .Include(u => u.Categories)
                .ThenInclude(c => c.Transactions)
            .FirstOrDefaultAsync(u => u.Id == subAccountId);

        if (currentUser == null || subAccount == null || subAccount.ParentUserId != currentUser.Id)
        {
            return NotFound();
        }

        var currentMonth = DateTime.Now.ToString("yyyy-MM");
        var totalBudgeted = subAccount.Categories.Sum(c => 
            c.Budgets.FirstOrDefault(b => b.MonthYear == currentMonth)?.Amount ?? 0);
        var totalSpent = subAccount.Categories.Sum(c => 
            c.Transactions.Where(t => t.Date.ToString("yyyy-MM") == currentMonth).Sum(t => t.Amount));

        var viewModel = new BudgetAccessViewModel
        {
            User = subAccount,
            Categories = subAccount.Categories.ToList(),
            TotalBudgeted = totalBudgeted,
            TotalSpent = totalSpent,
            TotalRemaining = totalBudgeted - totalSpent,
            CurrentMonth = currentMonth,
            IsReadOnly = true
        };

        return View(viewModel);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        else
            return RedirectToAction("Index", "Home");
    }
}