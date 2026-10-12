using DoughDoodle.Models;
using Microsoft.Extensions.Options;

namespace DoughDoodle.Services.Bank;

public class PlaidOptions
{
    public const string SectionName = "Plaid";
    public string ClientId { get; set; } = "";
    public string Secret { get; set; } = "";
    public string Environment { get; set; } = "sandbox";
}

/// <summary>Stub that only activates when Plaid ClientId+Secret are configured. No fake sync.</summary>
public class PlaidBankSyncProvider : IBankSyncProvider
{
    private readonly PlaidOptions _opts;

    public PlaidBankSyncProvider(IOptions<PlaidOptions> opts) => _opts = opts.Value;

    public BankSyncProviderKind Kind => BankSyncProviderKind.Plaid;
    public string DisplayName => "Plaid (when API keys configured)";
    public bool IsLiveConfigured =>
        !string.IsNullOrWhiteSpace(_opts.ClientId) && !string.IsNullOrWhiteSpace(_opts.Secret);

    public Task<BankSyncResult> SyncAsync(BankConnection connection, string userId, CancellationToken ct = default)
    {
        if (!IsLiveConfigured)
        {
            return Task.FromResult(new BankSyncResult(
                false,
                "Plaid keys are not configured. Set Plaid:ClientId and Plaid:Secret, or use Manual/CSV import."));
        }

        // Honest stub: keys present but Link/token exchange not wired in this build.
        return Task.FromResult(new BankSyncResult(
            false,
            "Plaid keys detected, but live Link sync is not enabled in this build. Use CSV/OFX until Link is wired."));
    }
}
