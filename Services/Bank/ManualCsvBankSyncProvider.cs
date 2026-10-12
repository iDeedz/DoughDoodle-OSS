using DoughDoodle.Models;

namespace DoughDoodle.Services.Bank;

/// <summary>Honest default provider — directs users to CSV/OFX import; never fakes a live bank link.</summary>
public class ManualCsvBankSyncProvider : IBankSyncProvider
{
    public BankSyncProviderKind Kind => BankSyncProviderKind.ManualCsv;
    public string DisplayName => "Manual / CSV import";
    public bool IsLiveConfigured => true; // always available

    public Task<BankSyncResult> SyncAsync(BankConnection connection, string userId, CancellationToken ct = default)
        => Task.FromResult(new BankSyncResult(
            true,
            "Use Import / Export to upload CSV, OFX, or QFX. Live bank APIs require provider API keys.",
            0));
}
