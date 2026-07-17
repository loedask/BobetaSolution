using Bobeta.Client.Models.Api;

namespace Bobeta.Client.Models.Games;

/// <summary>Current game state view model.</summary>
public class GameStateViewModel
{
    public Guid SessionId { get; set; }
    public GameVariant Variant { get; set; }
    /// <summary>Seat hand from API/SignalR. List for reliable System.Text.Json deserialization (incl. WASM/AOT).</summary>
    public List<string> MyCards { get; set; } = new();
    public string? LastPlayedCard { get; set; }
    public Guid? CurrentTurnPlayerId { get; set; }
    public bool GameOver { get; set; }
    public Guid? WinnerPlayerId { get; set; }

    public bool WaitingForGameStart { get; set; }

    public decimal LobbyPotAmount { get; set; }

    /// <summary>Other player's display name when the table has two seats; null while waiting for an opponent.</summary>
    public string? OpponentDisplayName { get; set; }

    /// <summary>Winner of the last resolved trick; null after the next trick begins.</summary>
    public Guid? LastTrickWinnerPlayerId { get; set; }

    public int MyRoundWins { get; set; }
    public int OpponentRoundWins { get; set; }

    /// <summary>From server: we are responding to opponent&apos;s lead and must follow suit (or Take if void).</summary>
    public bool MustFollowLedSuit { get; set; }

    public KopoStateDto? Kopo { get; set; }
    public NgolaStateDto? Ngola { get; set; }
}
