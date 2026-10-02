namespace Akiron.Identity.Domain.Common;

/// <summary>Wall-clock time, at a precision the database can actually keep.</summary>
public static class Timestamp
{
    /// <summary>
    /// Current UTC time truncated to whole microseconds, which is what <c>timestamptz</c>
    /// stores — see Catalog's copy for the bug this prevents.
    /// </summary>
    public static DateTimeOffset UtcNow()
    {
        var now = DateTimeOffset.UtcNow;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }
}
