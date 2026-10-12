using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Models.ViewModels;
using System.Security.Claims;
using DoughDoodle.Services.Security;
using Microsoft.AspNetCore.Identity;

namespace DoughDoodle.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly BudgetDbContext _context;
    private readonly ITotpService _totp;
    private readonly UserManager<ApplicationUser> _userManager;

    public SettingsController(BudgetDbContext context, ITotpService totp, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _totp = totp;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        // Get existing settings from database
        var existingBudgetSettings = await _context.BudgetSettings
            .FirstOrDefaultAsync(s => s.UserId == currentUserId);
        
        var existingNotificationSettings = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == currentUserId);

        // Create default settings if they don't exist
        var budgetSettings = existingBudgetSettings ?? new BudgetSettings
        {
            UserId = currentUserId,
            AutoRolloverSurplus = true,
            AutoRolloverOverspend = true,
            AllowCoverOverspendFromOtherCategories = true,
            DefaultIncomeAllocation = IncomeAllocation.Immediate,
            CopyLastMonthBudgets = false,
            FillBudgetsFromAccountBalances = false,
            EnableDarkMode = false,
            ColorTheme = "default",
            MinimalUI = false
        };

        var notificationSettings = existingNotificationSettings ?? new NotificationSettings
        {
            UserId = currentUserId,
            BudgetAlerts = true,
            GoalReminders = true,
            ScheduledTransactionReminders = true,
            MonthlyReports = true,
            EmailNotifications = true,
            BudgetThreshold = 80
        };

        var user = await _userManager.FindByIdAsync(currentUserId);
        var viewModel = new SettingsViewModel
        {
            PreferredCurrency = DoughDoodle.Helpers.CurrencyDisplay.Normalize(user?.PreferredCurrency),
            BudgetSettings = budgetSettings,
            NotificationSettings = notificationSettings
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCurrency(string preferredCurrency)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        var user = await _userManager.FindByIdAsync(currentUserId);
        if (user == null) return NotFound();

        var normalized = DoughDoodle.Helpers.CurrencyDisplay.Normalize(preferredCurrency);
        var supported = new[] { "USD", "ZAR", "EUR", "GBP", "AUD", "CAD", "JPY" };
        if (!supported.Contains(normalized))
        {
            TempData["Error"] = "Choose a supported currency.";
            return RedirectToAction(nameof(Index));
        }

        user.PreferredCurrency = normalized;
        await _userManager.UpdateAsync(user);
        TempData["Success"] = $"Currency updated to {normalized}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBudgetSettings(BudgetSettings settings)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        var existingSettings = await _context.BudgetSettings
            .FirstOrDefaultAsync(s => s.UserId == currentUserId);

        if (existingSettings == null) return NotFound();

        existingSettings.AutoRolloverSurplus = settings.AutoRolloverSurplus;
        existingSettings.AutoRolloverOverspend = settings.AutoRolloverOverspend;
        existingSettings.AllowCoverOverspendFromOtherCategories = settings.AllowCoverOverspendFromOtherCategories;
        existingSettings.DefaultIncomeAllocation = settings.DefaultIncomeAllocation;
        existingSettings.AutoHoldIncomeForNextMonth = settings.AutoHoldIncomeForNextMonth;
        existingSettings.CopyLastMonthBudgets = settings.CopyLastMonthBudgets;
        existingSettings.FillBudgetsFromAccountBalances = settings.FillBudgetsFromAccountBalances;
        existingSettings.EnableDarkMode = settings.EnableDarkMode;
        existingSettings.ColorTheme = settings.ColorTheme;
        existingSettings.MinimalUI = settings.MinimalUI;
        existingSettings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Budget settings updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateNotificationSettings(NotificationSettings settings)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        var existingSettings = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == currentUserId);

        if (existingSettings == null) return NotFound();

        existingSettings.BudgetAlerts = settings.BudgetAlerts;
        existingSettings.GoalReminders = settings.GoalReminders;
        existingSettings.ScheduledTransactionReminders = settings.ScheduledTransactionReminders;
        existingSettings.MonthlyReports = settings.MonthlyReports;
        existingSettings.EmailNotifications = settings.EmailNotifications;
        existingSettings.BudgetThreshold = settings.BudgetThreshold;
        existingSettings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Notification settings updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Security()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        var user = await _context.Users.FindAsync(currentUserId);
        if (user == null) return NotFound();

        var securityLogs = await _context.SecurityLogs
            .Where(sl => sl.UserId == currentUserId)
            .OrderByDescending(sl => sl.Timestamp)
            .Take(20)
            .ToListAsync();

        var viewModel = new SecurityViewModel
        {
            User = user,
            RecentSecurityLogs = securityLogs,
            TwoFactorEnabled = user.TwoFactorEnabled
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> LogSecurityEvent(string action, string details)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return Json(new { success = false });

        var securityLog = new SecurityLog
        {
            UserId = currentUserId,
            Action = action,
            Details = details,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            UserAgent = Request.Headers["User-Agent"].ToString(),
            Timestamp = DateTime.UtcNow
        };

        _context.SecurityLogs.Add(securityLog);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    public IActionResult ExportData()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        // This would typically generate a comprehensive export file
        // For now, return success message
        TempData["Success"] = "Data export initiated. You will receive an email when ready.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BeginTotpSetup()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        var (secret, uri) = await _totp.BeginSetupAsync(user);
        TempData["TotpSecret"] = secret;
        TempData["TotpUri"] = uri;
        return RedirectToAction(nameof(Security));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmTotp(string code)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        var secret = TempData["TotpSecret"] as string;
        if (string.IsNullOrEmpty(secret))
        {
            TempData["Error"] = "Start 2FA setup again.";
            return RedirectToAction(nameof(Security));
        }
        var ok = await _totp.EnableAsync(user, secret, code);
        TempData[ok ? "Success" : "Error"] = ok ? "Authenticator 2FA enabled." : "Invalid code — try again.";
        if (!ok) { TempData["TotpSecret"] = secret; TempData["TotpUri"] = _totp.GetAuthenticatorUri(user.Email ?? user.UserName ?? user.Id, secret); }
        return RedirectToAction(nameof(Security));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableTotp()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        await _totp.DisableAsync(user);
        TempData["Success"] = "2FA disabled.";
        return RedirectToAction(nameof(Security));
    }
}
