namespace Bobeta.Application.Games.Abbia;

/// <summary>Persisted Abbia match state (1v1 token-flip chance game).</summary>
public sealed class AbbiaGameState
{
    public Guid? CurrentTurnPlayerId { get; set; }

    /// <summary>True after the creator has thrown their tokens.</summary>
    public bool CreatorThrown { get; set; }

    /// <summary>True after the opponent has thrown their tokens.</summary>
    public bool OpponentThrown { get; set; }

    /// <summary>Creator token faces after throw; true = carved side up. Empty until thrown.</summary>
    public List<bool> CreatorTokens { get; set; } = new();

    /// <summary>Opponent token faces after throw; true = carved side up. Empty until thrown.</summary>
    public List<bool> OpponentTokens { get; set; } = new();
}
