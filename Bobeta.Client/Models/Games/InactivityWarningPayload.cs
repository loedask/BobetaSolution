namespace Bobeta.Client.Models.Games;

/// <summary>SignalR broadcast when an AFK warning opens for the session.</summary>
public sealed class InactivityWarningPayload
{
    public int Phase { get; set; }
    public DateTime DecisionDeadlineUtc { get; set; }
    public bool ShowButtons { get; set; }
}
