using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;

namespace Bobeta.Client.Presentation;

/// <summary>Mutable table UI state updated only from authoritative <see cref="GameStateViewModel"/> snapshots.</summary>
public sealed class GamePlayTableState
{
    public GameVariant Variant { get; set; } = GameVariant.Makopa;
    public KopoStateDto? Kopo { get; set; }
    public NgolaStateDto? Ngola { get; set; }
    public DominoStateDto? Domino { get; set; }
    public AbbiaStateDto? Abbia { get; set; }
    public NzengueStateDto? Nzengue { get; set; }
    public bool IsPlayerTurn { get; set; }
    public decimal PotAmount { get; set; }
    public string? OpponentDisplayName { get; set; }
    public bool WaitingForOpponent { get; set; }
    public List<CardViewModel> PlayerCards { get; set; } = new();
    public CardViewModel? LastPlayedCard { get; set; }
    public Guid? CurrentPlayerId { get; set; }
    public bool MustFollowLedSuit { get; set; }
    public string? TrickOutcomeMessage { get; set; }
    public bool CanTakeCard { get; set; }
    public int MyRoundWins { get; set; }
    public int OpponentRoundWins { get; set; }
    public string? MatchRoundScoreText { get; set; }
    public bool ShowGameResult { get; set; }
    public bool IsDraw { get; set; }
    public bool EndedByForfeit { get; set; }
    public string? WinnerPlayerName { get; set; }
}
