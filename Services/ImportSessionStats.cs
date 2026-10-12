using DoughDoodle.Models;

namespace DoughDoodle.Services;

/// <summary>
/// Honest ImportSessions aggregates. Never invent marketing counts.
/// </summary>
public sealed class ImportSessionSummary
{
    public int SessionCount { get; init; }
    public int RecordsImported { get; init; }
    public int FailedRecords { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public string LastActivityLabel { get; init; } = EmptyLastActivityLabel;

    public const string EmptyLastActivityLabel = "Never";
}

public static class ImportSessionStats
{
    public static ImportSessionSummary From(IEnumerable<ImportSession> sessions, DateTime? nowUtc = null)
    {
        var list = sessions as IList<ImportSession> ?? sessions.ToList();
        var last = list.Count == 0
            ? (DateTime?)null
            : list.Max(s => s.CompletedAt ?? s.CreatedAt);

        return new ImportSessionSummary
        {
            SessionCount = list.Count,
            RecordsImported = list.Sum(s => s.SuccessfulRecords),
            FailedRecords = list.Sum(s => s.FailedRecords),
            LastActivityAt = last,
            LastActivityLabel = FormatLastActivity(last, nowUtc ?? DateTime.UtcNow)
        };
    }

    public static string FormatLastActivity(DateTime? lastUtc, DateTime nowUtc)
    {
        if (lastUtc == null)
            return ImportSessionSummary.EmptyLastActivityLabel;

        var span = nowUtc - lastUtc.Value;
        if (span.TotalSeconds < 60)
            return "just now";
        if (span.TotalHours < 1)
            return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalDays < 1)
            return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 14)
            return $"{(int)span.TotalDays}d ago";
        return lastUtc.Value.ToString("MMM d");
    }
}
