namespace Bobeta.Domain.Enums;

/// <summary>Lifecycle and moderation status of a player account.</summary>
public enum PlayerStatus
{
    /// <summary>Registered but not yet fully activated.</summary>
    Pending = 0,

    /// <summary>Account in good standing; can play and withdraw.</summary>
    Active = 1,

    /// <summary>Temporarily blocked (e.g. under review).</summary>
    Suspended = 2,

    /// <summary>Permanently banned.</summary>
    Banned = 3
}
