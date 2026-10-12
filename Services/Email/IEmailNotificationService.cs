namespace DoughDoodle.Services.Email;

public interface IEmailNotificationService
{
    bool IsConfigured { get; }
    Task<bool> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
    Task NotifyBillDueAsync(string toEmail, string billName, decimal amount, DateTime dueDate, CancellationToken ct = default);
    Task NotifyBudgetAlertAsync(string toEmail, string categoryName, decimal percentUsed, CancellationToken ct = default);
}
