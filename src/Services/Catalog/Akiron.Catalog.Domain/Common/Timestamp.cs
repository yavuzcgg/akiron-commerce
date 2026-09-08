namespace Akiron.Catalog.Domain.Common;

/// <summary>Wall-clock time, at a precision the database can actually keep.</summary>
public static class Timestamp
{
    /// <summary>
    /// Current UTC time truncated to whole microseconds.
    /// </summary>
    /// <remarks>
    /// PostgreSQL's <c>timestamptz</c> stores microseconds; .NET counts 100-nanosecond
    /// ticks. Without truncating at the source, an entity carries a more precise value
    /// in memory than the row it was written to, so the object returned by a create call
    /// and the one read back a moment later disagree — over a difference no caller can
    /// see or care about. Truncating once, here, keeps the two identical.
    /// </remarks>
    public static DateTimeOffset UtcNow()
    {
        var now = DateTimeOffset.UtcNow;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }
}
