using Bobeta.Application.DTOs.History;
using Bobeta.Application.Interfaces;

namespace Bobeta.Application.Services;

/// <summary>Application service for a player's game history: list of past sessions with opponent and result.</summary>
public class GameHistoryService(IGameSessionRepository sessionRepository) : IGameHistoryService
{
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;

    public async Task<IReadOnlyList<GameHistoryItemDto>> GetPlayerHistoryAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.GetByPlayerIdAsync(playerId, skip, take, cancellationToken);
        return sessions.Select(s =>
        {
            var result = s.GameResult;
            var opponentId = s.CreatorPlayerId == playerId ? s.OpponentPlayerId : s.CreatorPlayerId;
            var winnerId = result?.WinnerPlayerId;
            var wonAmount = result != null && result.WinnerPlayerId == playerId ? result.WinnerAmount : (decimal?)null;
            return new GameHistoryItemDto(s.Id, s.BetAmount, s.Status, opponentId, winnerId, wonAmount, s.CreatedAt);
        }).ToList();
    }
}
