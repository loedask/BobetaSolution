using Bobeta.Domain.Enums;

namespace Bobeta.Domain.Entities;

/// <summary>
/// Represents a registered player on the Bobeta gaming platform.
/// Players authenticate via Mobile Money phone number and OTP.
/// </summary>
public class Player
{
    /// <summary>Unique identifier for the player.</summary>
    public Guid Id { get; set; }

    /// <summary>Mobile Money phone number used for registration and login.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Display name chosen by the player.</summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>Preferred UI language (e.g. "en", "fr").</summary>
    public string Language { get; set; } = "en";

    /// <summary>When the player account was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Whether the player has completed OTP verification.</summary>
    public bool IsVerified { get; set; }

    /// <summary>Current account status (e.g. Active, Suspended).</summary>
    public PlayerStatus Status { get; set; }
}
