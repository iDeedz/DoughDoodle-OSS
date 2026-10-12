using DoughDoodle.Models;

namespace DoughDoodle.Services.Bank;

public record BankSyncResult(bool Success, string Message, int ImportedCount = 0);

public interface IBankSyncProvider
{
    BankSyncProviderKind Kind { get; }
    string DisplayName { get; }
    bool IsLiveConfigured { get; }
    Task<BankSyncResult> SyncAsync(BankConnection connection, string userId, CancellationToken ct = default);
}
