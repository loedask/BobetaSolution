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

    [JsonPropertyName("domino")]
    public DominoStateDto? Domino { get; set; }

    [JsonPropertyName("abbia")]
    public AbbiaStateDto? Abbia { get; set; }

    [JsonPropertyName("nzengue")]
    public NzengueStateDto? Nzengue { get; set; }

    [JsonPropertyName("yote")]
    public YoteStateDto? Yote { get; set; }

    [JsonPropertyName("isDraw")]
    public bool IsDraw { get; set; }
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

public sealed class DominoStateDto
{
    [JsonPropertyName("myHand")]
    public List<string> MyHand { get; set; } = new();

    [JsonPropertyName("opponentHandCount")]
    public int OpponentHandCount { get; set; }

    [JsonPropertyName("boneyardCount")]
    public int BoneyardCount { get; set; }

    [JsonPropertyName("chain")]
    public List<string> Chain { get; set; } = new();

    [JsonPropertyName("leftEnd")]
    public int? LeftEnd { get; set; }

    [JsonPropertyName("rightEnd")]
    public int? RightEnd { get; set; }

    [JsonPropertyName("isOpening")]
    public bool IsOpening { get; set; }

    [JsonPropertyName("openingTile")]
    public string? OpeningTile { get; set; }

    [JsonPropertyName("mustDraw")]
    public bool MustDraw { get; set; }

    [JsonPropertyName("mustPass")]
    public bool MustPass { get; set; }
}

public sealed class DominoMoveApiRequest
{
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = "play";

    [JsonPropertyName("high")]
    public int? High { get; set; }

    [JsonPropertyName("low")]
    public int? Low { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }
}

public sealed class AbbiaStateDto
{
    [JsonPropertyName("tokenCount")]
    public int TokenCount { get; set; }

    [JsonPropertyName("myTokens")]
    public List<bool>? MyTokens { get; set; }

    [JsonPropertyName("opponentTokens")]
    public List<bool>? OpponentTokens { get; set; }

    [JsonPropertyName("myCarvedUp")]
    public int? MyCarvedUp { get; set; }

    [JsonPropertyName("opponentCarvedUp")]
    public int? OpponentCarvedUp { get; set; }

    [JsonPropertyName("iHaveThrown")]
    public bool IHaveThrown { get; set; }

    [JsonPropertyName("opponentHasThrown")]
    public bool OpponentHasThrown { get; set; }

    [JsonPropertyName("canThrow")]
    public bool CanThrow { get; set; }
}

public sealed class AbbiaMoveApiRequest
{
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }
}

public sealed class NzengueStateDto
{
    [JsonPropertyName("pointCount")]
    public int PointCount { get; set; }

    [JsonPropertyName("piecesPerPlayer")]
    public int PiecesPerPlayer { get; set; }

    [JsonPropertyName("phase")]
    public string Phase { get; set; } = "place";

    [JsonPropertyName("occupancy")]
    public List<int>? Occupancy { get; set; }

    [JsonPropertyName("myPiecesToPlace")]
    public int MyPiecesToPlace { get; set; }

    [JsonPropertyName("opponentPiecesToPlace")]
    public int OpponentPiecesToPlace { get; set; }

    [JsonPropertyName("legalPlacePoints")]
    public List<int>? LegalPlacePoints { get; set; }

    [JsonPropertyName("legalMoves")]
    public List<NzengueEdgeDto>? LegalMoves { get; set; }

    [JsonPropertyName("canAct")]
    public bool CanAct { get; set; }
}

public sealed class NzengueEdgeDto
{
    [JsonPropertyName("from")]
    public int From { get; set; }

    [JsonPropertyName("to")]
    public int To { get; set; }
}

public sealed class NzengueMoveApiRequest
{
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("fromPoint")]
    public int? FromPoint { get; set; }

    [JsonPropertyName("toPoint")]
    public int ToPoint { get; set; }
}

public sealed class YoteStateDto
{
    [JsonPropertyName("rows")]
    public int Rows { get; set; }

    [JsonPropertyName("cols")]
    public int Cols { get; set; }

    [JsonPropertyName("piecesPerPlayer")]
    public int PiecesPerPlayer { get; set; }

    [JsonPropertyName("occupancy")]
    public List<int>? Occupancy { get; set; }

    [JsonPropertyName("myInHand")]
    public int MyInHand { get; set; }

    [JsonPropertyName("opponentInHand")]
    public int OpponentInHand { get; set; }

    [JsonPropertyName("legalPlaceCells")]
    public List<int>? LegalPlaceCells { get; set; }

    [JsonPropertyName("legalSlides")]
    public List<YoteEdgeDto>? LegalSlides { get; set; }

    [JsonPropertyName("legalCaptures")]
    public List<YoteCaptureDto>? LegalCaptures { get; set; }

    [JsonPropertyName("canAct")]
    public bool CanAct { get; set; }
}

public sealed class YoteEdgeDto
{
    [JsonPropertyName("from")]
    public int From { get; set; }

    [JsonPropertyName("to")]
    public int To { get; set; }
}

public sealed class YoteCaptureDto
{
    [JsonPropertyName("from")]
    public int From { get; set; }

    [JsonPropertyName("to")]
    public int To { get; set; }

    [JsonPropertyName("jumped")]
    public int Jumped { get; set; }
}

public sealed class YoteMoveApiRequest
{
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("fromCell")]
    public int? FromCell { get; set; }

    [JsonPropertyName("toCell")]
    public int ToCell { get; set; }

    [JsonPropertyName("extraRemoveCell")]
    public int? ExtraRemoveCell { get; set; }
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
