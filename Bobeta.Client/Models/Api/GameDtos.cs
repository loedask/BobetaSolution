using System.Text.Json.Serialization;

namespace Bobeta.Client.Models.Api;

public sealed class CreateGameApiRequest
{
    [JsonPropertyName("betAmount")]
    public double BetAmount { get; set; }

    [JsonPropertyName("variant")]
    public GameVariant Variant { get; set; } = GameVariant.Makopa;
}

public sealed class JoinGameApiRequest
{
    [JsonPropertyName("gameId")]
    public Guid GameId { get; set; }
}

public sealed class ProposeBetApiRequest
{
    [JsonPropertyName("gameId")]
    public Guid GameId { get; set; }

    [JsonPropertyName("amount")]
    public double Amount { get; set; }
}

public sealed class GameSessionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("creatorPlayerId")]
    public Guid CreatorPlayerId { get; set; }

    [JsonPropertyName("opponentPlayerId")]
    public Guid? OpponentPlayerId { get; set; }

    [JsonPropertyName("betAmount")]
    public double BetAmount { get; set; }

    [JsonPropertyName("status")]
    public GameStatus Status { get; set; }

    [JsonPropertyName("variant")]
    public GameVariant Variant { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTime? StartedAt { get; set; }

    [JsonPropertyName("finishedAt")]
    public DateTime? FinishedAt { get; set; }
}

public sealed class CardPlayDto
{
    [JsonPropertyName("suit")]
    public int Suit { get; set; }

    [JsonPropertyName("rank")]
    public int Rank { get; set; }
}

public sealed class PlayCardApiRequest
{
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("card")]
    public CardPlayDto Card { get; set; } = new();
}

public sealed class GameStateDto
{
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("myCards")]
    public List<string>? MyCards { get; set; }

    [JsonPropertyName("lastPlayedCard")]
    public string? LastPlayedCard { get; set; }

    [JsonPropertyName("currentTurnPlayerId")]
    public Guid? CurrentTurnPlayerId { get; set; }

    [JsonPropertyName("gameOver")]
    public bool GameOver { get; set; }

    [JsonPropertyName("winnerPlayerId")]
    public Guid? WinnerPlayerId { get; set; }

    [JsonPropertyName("waitingForGameStart")]
    public bool WaitingForGameStart { get; set; }

    [JsonPropertyName("lobbyPotAmount")]
    public double LobbyPotAmount { get; set; }

    [JsonPropertyName("opponentDisplayName")]
    public string? OpponentDisplayName { get; set; }

    [JsonPropertyName("lastTrickWinnerPlayerId")]
    public Guid? LastTrickWinnerPlayerId { get; set; }

    [JsonPropertyName("myRoundWins")]
    public int MyRoundWins { get; set; }

    [JsonPropertyName("opponentRoundWins")]
    public int OpponentRoundWins { get; set; }

    [JsonPropertyName("mustFollowLedSuit")]
    public bool MustFollowLedSuit { get; set; }

    [JsonPropertyName("variant")]
    public GameVariant Variant { get; set; }

    [JsonPropertyName("kopo")]
    public KopoStateDto? Kopo { get; set; }

    [JsonPropertyName("ngola")]
    public NgolaStateDto? Ngola { get; set; }
}

public sealed class KopoSquareDto
{
    [JsonPropertyName("row")]
    public int Row { get; set; }

    [JsonPropertyName("col")]
    public int Col { get; set; }
}

public sealed class KopoPieceDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("ownerId")]
    public Guid OwnerId { get; set; }

    [JsonPropertyName("row")]
    public int Row { get; set; }

    [JsonPropertyName("col")]
    public int Col { get; set; }

    [JsonPropertyName("isKing")]
    public bool IsKing { get; set; }
}

public sealed class KopoStateDto
{
    [JsonPropertyName("boardSize")]
    public int BoardSize { get; set; }

    [JsonPropertyName("pieces")]
    public List<KopoPieceDto> Pieces { get; set; } = new();

    [JsonPropertyName("mustContinueChain")]
    public bool MustContinueChain { get; set; }

    [JsonPropertyName("chainPieceId")]
    public int? ChainPieceId { get; set; }
}

public sealed class KopoMoveApiRequest
{
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("path")]
    public List<KopoSquareDto> Path { get; set; } = new();
}

public sealed class NgolaStateDto
{
    [JsonPropertyName("pitsPerPlayer")]
    public int PitsPerPlayer { get; set; }

    [JsonPropertyName("myPits")]
    public List<int> MyPits { get; set; } = new();

    [JsonPropertyName("opponentPits")]
    public List<int> OpponentPits { get; set; } = new();

    [JsonPropertyName("myScore")]
    public int MyScore { get; set; }

    [JsonPropertyName("opponentScore")]
    public int OpponentScore { get; set; }
}

public sealed class NgolaMoveApiRequest
{
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("pitIndex")]
    public int PitIndex { get; set; }
}

public sealed class GameHistoryItemDto
{
    [JsonPropertyName("gameSessionId")]
    public Guid GameSessionId { get; set; }

    [JsonPropertyName("betAmount")]
    public double BetAmount { get; set; }

    [JsonPropertyName("status")]
    public GameStatus Status { get; set; }

    [JsonPropertyName("variant")]
    public GameVariant Variant { get; set; }

    [JsonPropertyName("opponentPlayerId")]
    public Guid? OpponentPlayerId { get; set; }

    [JsonPropertyName("winnerPlayerId")]
    public Guid? WinnerPlayerId { get; set; }

    [JsonPropertyName("wonAmount")]
    public double? WonAmount { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("isCreator")]
    public bool IsCreator { get; set; }

    public string VariantName => GameVariantLabels.Name(Variant);
}
