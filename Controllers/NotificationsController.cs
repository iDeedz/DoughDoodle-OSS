using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Models.ViewModels;
using System.Security.Claims;
using DoughDoodle.Services.Email;

namespace DoughDoodle.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly IAlertDispatchService _alerts;
    private readonly BudgetDbContext _context;

    public NotificationsController(BudgetDbContext context, IAlertDispatchService alerts)
    {
        _context = context;
    
        _alerts = alerts;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        var notifications = await _context.Notifications
            .Where(n => n.UserId == currentUserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        var viewModel = new NotificationsViewModel
        {
            Notifications = notifications,
            UnreadCount = notifications.Count(n => !n.IsRead)
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUserId);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == currentUserId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return Json(new { count = 0 });

        var count = await _context.Notifications
            .CountAsync(n => n.UserId == currentUserId && !n.IsRead);

        return Json(new { count });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUserId);

        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    // Background service will call this to create notifications
    public async Task CreateBudgetAlert(string userId, string categoryName, decimal spentAmount, decimal budgetAmount, decimal percentage)
    {
        var currency = DoughDoodle.Helpers.CurrencyDisplay.Normalize((await _context.Users.FindAsync(userId))?.PreferredCurrency);
        var notification = new Notification
        {
            UserId = userId,
            Type = NotificationType.BudgetAlert,
            Title = $"Budget Alert: {categoryName}",
            Message = $"You've spent {DoughDoodle.Helpers.CurrencyDisplay.Format(spentAmount, currency)} ({percentage:F1}%) of your {DoughDoodle.Helpers.CurrencyDisplay.Format(budgetAmount, currency)} budget for {categoryName}.",
            Priority = percentage >= 100 ? NotificationPriority.High : NotificationPriority.Medium,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task CreateGoalAchievement(string userId, string goalName)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = NotificationType.GoalAchievement,
            Title = "🎉 Goal Achieved!",
            Message = $"Congratulations! You've achieved your goal: {goalName}",
            Priority = NotificationPriority.High,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task CreateScheduledTransactionDue(string userId, string description, decimal amount, DateTime dueDate)
    {
        var currency = DoughDoodle.Helpers.CurrencyDisplay.Normalize((await _context.Users.FindAsync(userId))?.PreferredCurrency);
        var notification = new Notification
        {
            UserId = userId,
            Type = NotificationType.ScheduledTransactionDue,
            Title = "Scheduled Transaction Due",
            Message = $"'{description}' for {DoughDoodle.Helpers.CurrencyDisplay.Format(amount, currency)} is due on {dueDate:MMM dd, yyyy}",
            Priority = NotificationPriority.Medium,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DispatchAlerts()
    {
        await _alerts.DispatchDueBillsAndBudgetAlertsAsync();
        TempData["Success"] = "Alerts dispatched (email sent only when SMTP is configured).";
        return RedirectToAction(nameof(Index));
    }
}
