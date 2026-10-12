using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Services.Email;

public interface IAlertDispatchService
{
    Task DispatchDueBillsAndBudgetAlertsAsync(CancellationToken ct = default);
}

public class AlertDispatchService : IAlertDispatchService
{
    private readonly BudgetDbContext _db;
    private readonly IEmailNotificationService _email;
    private readonly UserManager<ApplicationUser> _users;

    public AlertDispatchService(BudgetDbContext db, IEmailNotificationService email, UserManager<ApplicationUser> users)
    {
        _db = db;
        _email = email;
        _users = users;
    }

    public async Task DispatchDueBillsAndBudgetAlertsAsync(CancellationToken ct = default)
    {
        var settings = await _db.NotificationSettings.Where(s => s.EmailNotifications).ToListAsync(ct);
        var soon = DateTime.UtcNow.Date.AddDays(3);

        foreach (var s in settings)
        {
            var user = await _users.FindByIdAsync(s.UserId);
            if (user?.Email == null) continue;

            if (s.BudgetAlerts)
            {
                var month = DateTime.UtcNow.ToString("yyyy-MM");
                var cats = await _db.Categories.Where(c => c.UserId == s.UserId).ToListAsync(ct);
                foreach (var cat in cats)
                {
                    var budget = await _db.MonthlyBudgets.FirstOrDefaultAsync(b => b.CategoryId == cat.Id && b.MonthYear == month, ct);
                    if (budget == null || budget.Amount <= 0) continue;
                    var spent = await _db.Transactions
                        .Where(t => t.CategoryId == cat.Id && t.TransactionType == TransactionType.Expense &&
                                    t.Date.Year == DateTime.UtcNow.Year && t.Date.Month == DateTime.UtcNow.Month)
                        .SumAsync(t => Math.Abs(t.Amount), ct);
                    var pct = spent / budget.Amount * 100m;
                    if (pct >= s.BudgetThreshold)
                    {
                        _db.Notifications.Add(new Notification
                        {
                            UserId = s.UserId,
                            Title = $"Budget alert: {cat.Name}",
                            Message = $"{cat.Name} is at {pct:0.#}% of budget.",
                            Type = NotificationType.BudgetAlert,
                            Priority = NotificationPriority.High,
                            ActionUrl = "/Budget",
                            CreatedAt = DateTime.UtcNow
                        });
                        if (_email.IsConfigured)
                            await _email.NotifyBudgetAlertAsync(user.Email, cat.Name, pct, ct);
                    }
                }
            }

            var bills = await _db.Bills
                .Where(b => b.UserId == s.UserId && b.DueDate <= soon && b.DueDate >= DateTime.UtcNow.Date)
                .ToListAsync(ct);
            foreach (var bill in bills)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = s.UserId,
                    Title = $"Bill due: {bill.Name}",
                    Message = $"{bill.Name} due {bill.DueDate:yyyy-MM-dd} for {bill.Amount:C}.",
                    Type = NotificationType.ScheduledTransactionDue,
                    Priority = NotificationPriority.High,
                    ActionUrl = "/Bills",
                    CreatedAt = DateTime.UtcNow
                });
                if (_email.IsConfigured)
                    await _email.NotifyBillDueAsync(user.Email, bill.Name, bill.Amount, bill.DueDate, ct);
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
