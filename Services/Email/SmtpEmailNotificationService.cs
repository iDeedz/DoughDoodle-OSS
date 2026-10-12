using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace DoughDoodle.Services.Email;

public class SmtpOptions
{
    public const string SectionName = "Smtp";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "noreply@doughdoodle.local";
    public string FromName { get; set; } = "DoughDoodle";
    public bool UseStartTls { get; set; } = true;
}

public class SmtpEmailNotificationService : IEmailNotificationService
{
    private readonly SmtpOptions _opts;
    private readonly ILogger<SmtpEmailNotificationService> _log;

    public SmtpEmailNotificationService(IOptions<SmtpOptions> opts, ILogger<SmtpEmailNotificationService> log)
    {
        _opts = opts.Value;
        _log = log;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_opts.Host) && !string.IsNullOrWhiteSpace(_opts.FromAddress);

    public async Task<bool> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _log.LogInformation("SMTP not configured — skipping email to {To}: {Subject}", toEmail, subject);
            return false;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_opts.FromName, _opts.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        var secure = _opts.UseStartTls ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.Auto;
        await client.ConnectAsync(_opts.Host, _opts.Port, secure, ct);
        if (!string.IsNullOrWhiteSpace(_opts.UserName))
            await client.AuthenticateAsync(_opts.UserName, _opts.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
        return true;
    }

    public Task NotifyBillDueAsync(string toEmail, string billName, decimal amount, DateTime dueDate, CancellationToken ct = default)
    {
        var subject = $"Bill due: {billName}";
        var body = $"<p>Your bill <strong>{System.Net.WebUtility.HtmlEncode(billName)}</strong> " +
                   $"for <strong>{amount:C}</strong> is due on <strong>{dueDate:yyyy-MM-dd}</strong>.</p>" +
                   "<p>— DoughDoodle</p>";
        return SendAsync(toEmail, subject, body, ct);
    }

    public Task NotifyBudgetAlertAsync(string toEmail, string categoryName, decimal percentUsed, CancellationToken ct = default)
    {
        var subject = $"Budget alert: {categoryName}";
        var body = $"<p>Envelope <strong>{System.Net.WebUtility.HtmlEncode(categoryName)}</strong> " +
                   $"is at <strong>{percentUsed:0.#}%</strong> of its monthly dough.</p>" +
                   "<p>— DoughDoodle</p>";
        return SendAsync(toEmail, subject, body, ct);
    }
}
