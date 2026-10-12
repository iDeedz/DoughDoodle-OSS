using System.Text.Json;
using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Services.Undo;

public interface ITransactionUndoService
{
    Task RecordAsync(string userId, string actionType, Transaction before, Transaction? after, CancellationToken ct = default);
    Task<(bool ok, string message)> UndoLastAsync(string userId, CancellationToken ct = default);
    Task<(bool ok, string message)> RedoLastAsync(string userId, CancellationToken ct = default);
}

public class TransactionUndoService : ITransactionUndoService
{
    private readonly BudgetDbContext _db;
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public TransactionUndoService(BudgetDbContext db) => _db = db;

    public async Task RecordAsync(string userId, string actionType, Transaction before, Transaction? after, CancellationToken ct = default)
    {
        var action = new UserAction
        {
            UserId = userId,
            ActionType = actionType,
            EntityType = "Transaction",
            EntityId = before.Id != 0 ? before.Id : after?.Id ?? 0,
            PreviousState = JsonSerializer.Serialize(Snapshot(before), JsonOpts),
            NewState = after == null ? null : JsonSerializer.Serialize(Snapshot(after), JsonOpts),
            CreatedAt = DateTime.UtcNow
        };
        _db.UserActions.Add(action);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(bool ok, string message)> UndoLastAsync(string userId, CancellationToken ct = default)
    {
        var action = await _db.UserActions
            .Where(a => a.UserId == userId && a.EntityType == "Transaction" && !a.IsUndone)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (action == null) return (false, "Nothing to undo.");

        if (action.ActionType == "UPDATE" && action.PreviousState != null)
        {
            var snap = JsonSerializer.Deserialize<TxSnap>(action.PreviousState);
            var tx = await _db.Transactions.FindAsync(new object[] { action.EntityId }, ct);
            if (tx == null || snap == null) return (false, "Original transaction missing.");
            Apply(tx, snap);
            tx.UpdatedAt = DateTime.UtcNow;
        }
        else if (action.ActionType == "DELETE" && action.PreviousState != null)
        {
            var snap = JsonSerializer.Deserialize<TxSnap>(action.PreviousState);
            if (snap == null) return (false, "Cannot restore.");
            _db.Transactions.Add(FromSnap(snap));
        }
        else if (action.ActionType == "CREATE")
        {
            var tx = await _db.Transactions.FindAsync(new object[] { action.EntityId }, ct);
            if (tx != null) _db.Transactions.Remove(tx);
        }

        action.IsUndone = true;
        action.UndoneAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return (true, "Undid last transaction change.");
    }

    public async Task<(bool ok, string message)> RedoLastAsync(string userId, CancellationToken ct = default)
    {
        var action = await _db.UserActions
            .Where(a => a.UserId == userId && a.EntityType == "Transaction" && a.IsUndone)
            .OrderByDescending(a => a.UndoneAt)
            .FirstOrDefaultAsync(ct);
        if (action == null) return (false, "Nothing to redo.");

        if (action.ActionType == "UPDATE" && action.NewState != null)
        {
            var snap = JsonSerializer.Deserialize<TxSnap>(action.NewState);
            var tx = await _db.Transactions.FindAsync(new object[] { action.EntityId }, ct);
            if (tx == null || snap == null) return (false, "Transaction missing.");
            Apply(tx, snap);
        }
        else if (action.ActionType == "CREATE" && action.NewState != null)
        {
            var snap = JsonSerializer.Deserialize<TxSnap>(action.NewState);
            if (snap != null) _db.Transactions.Add(FromSnap(snap));
        }
        else if (action.ActionType == "DELETE")
        {
            var tx = await _db.Transactions.FindAsync(new object[] { action.EntityId }, ct);
            if (tx != null) _db.Transactions.Remove(tx);
        }

        action.IsUndone = false;
        action.UndoneAt = null;
        await _db.SaveChangesAsync(ct);
        return (true, "Redid last transaction change.");
    }

    private static TxSnap Snapshot(Transaction t) => new(
        t.Id, t.Description, t.Amount, t.Date, t.TransactionType, t.CategoryId, t.AccountId, t.Notes);

    private static void Apply(Transaction t, TxSnap s)
    {
        t.Description = s.Description;
        t.Amount = s.Amount;
        t.Date = s.Date;
        t.TransactionType = s.TransactionType;
        t.CategoryId = s.CategoryId;
        t.AccountId = s.AccountId;
        t.Notes = s.Notes ?? "";
    }

    private static Transaction FromSnap(TxSnap s) => new()
    {
        Id = s.Id,
        Description = s.Description,
        Amount = s.Amount,
        Date = s.Date,
        TransactionType = s.TransactionType,
        CategoryId = s.CategoryId,
        AccountId = s.AccountId,
        Notes = s.Notes ?? "",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private record TxSnap(int Id, string Description, decimal Amount, DateTime Date, TransactionType TransactionType, int? CategoryId, int? AccountId, string? Notes);
}
