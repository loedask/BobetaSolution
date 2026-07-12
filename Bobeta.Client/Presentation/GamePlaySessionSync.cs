using Bobeta.Client.Contracts;
using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Games;

namespace Bobeta.Client.Presentation;

/// <summary>Loads authoritative game state from the API with coalesced concurrent requests.</summary>
public sealed class GamePlaySessionSync(IGameService gameService)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<SyncResult> FetchAndApplyAsync(
        Guid sessionId,
        GamePlayTableState table,
        Guid? myPlayerId,
        bool blockInteraction,
        Func<string>? trickOutcomeYou = null,
        Func<string>? trickOutcomeOpponent = null,
        Func<int, int, string>? roundScoreFormat = null,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var res = await gameService.GetGameStateAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (!res.IsSuccess || res.Data == null)
                return new SyncResult(res.StatusCode, res.ErrorMessage, null);

            var apply = GamePlayStateApplier.ApplyAuthoritativeState(
                table, res.Data, myPlayerId, blockInteraction, trickOutcomeYou, trickOutcomeOpponent, roundScoreFormat);
            return new SyncResult(null, null, apply);
        }
        finally
        {
            _gate.Release();
        }
    }

    public readonly record struct SyncResult(int? StatusCode, string? ErrorMessage, GamePlayStateApplier.ApplyResult? Apply);
}
