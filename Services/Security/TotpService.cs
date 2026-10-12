using OtpNet;
using DoughDoodle.Models;
using Microsoft.AspNetCore.Identity;

namespace DoughDoodle.Services.Security;

public interface ITotpService
{
    string GenerateSecret();
    string GetAuthenticatorUri(string email, string secret, string issuer = "DoughDoodle");
    bool VerifyCode(string secret, string code);
    Task<(string secret, string uri)> BeginSetupAsync(ApplicationUser user);
    Task<bool> EnableAsync(ApplicationUser user, string secret, string code);
    Task DisableAsync(ApplicationUser user);
}

public class TotpService : ITotpService
{
    private readonly UserManager<ApplicationUser> _users;

    public TotpService(UserManager<ApplicationUser> users) => _users = users;

    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string GetAuthenticatorUri(string email, string secret, string issuer = "DoughDoodle")
        => $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code)) return false;
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(1, 1));
    }

    public Task<(string secret, string uri)> BeginSetupAsync(ApplicationUser user)
    {
        var secret = GenerateSecret();
        var uri = GetAuthenticatorUri(user.Email ?? user.UserName ?? user.Id, secret);
        return Task.FromResult((secret, uri));
    }

    public async Task<bool> EnableAsync(ApplicationUser user, string secret, string code)
    {
        if (!VerifyCode(secret, code)) return false;
        user.AuthenticatorKey = secret;
        await _users.SetTwoFactorEnabledAsync(user, true);
        await _users.UpdateAsync(user);
        return true;
    }

    public async Task DisableAsync(ApplicationUser user)
    {
        user.AuthenticatorKey = null;
        await _users.SetTwoFactorEnabledAsync(user, false);
        await _users.UpdateAsync(user);
    }
}
