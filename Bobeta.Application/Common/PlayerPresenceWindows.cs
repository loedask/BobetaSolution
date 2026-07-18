namespace Bobeta.Application.Common;

/// <summary>Shared thresholds for authenticated player presence metrics.</summary>
public static class PlayerPresenceWindows
{
    /// <summary>A player counts as online while LastSeenOnlineUtc is within this window.</summary>
    public static readonly TimeSpan Online = TimeSpan.FromMinutes(2);

    /// <summary>Minimum gap between persisted presence writes for one player.</summary>
    public static readonly TimeSpan MinWriteInterval = TimeSpan.FromSeconds(30);
}
