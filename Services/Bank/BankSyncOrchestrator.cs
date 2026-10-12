using DoughDoodle.Models;

namespace DoughDoodle.Services.Bank;

public interface IBankSyncOrchestrator
{
    IReadOnlyList<IBankSyncProvider> Providers { get; }
    IBankSyncProvider Resolve(BankSyncProviderKind kind);
    Task<BankSyncResult> SyncAsync(BankConnection connection, string userId, CancellationToken ct = default);
}

public class BankSyncOrchestrator : IBankSyncOrchestrator
{
    private readonly IEnumerable<IBankSyncProvider> _providers;

    public BankSyncOrchestrator(IEnumerable<IBankSyncProvider> providers) => _providers = providers;

    public IReadOnlyList<IBankSyncProvider> Providers => _providers.ToList();

    public IBankSyncProvider Resolve(BankSyncProviderKind kind)
        => _providers.FirstOrDefault(p => p.Kind == kind)
           ?? _providers.First(p => p.Kind == BankSyncProviderKind.ManualCsv);

    public Task<BankSyncResult> SyncAsync(BankConnection connection, string userId, CancellationToken ct = default)
        => Resolve(connection.Provider).SyncAsync(connection, userId, ct);
}
