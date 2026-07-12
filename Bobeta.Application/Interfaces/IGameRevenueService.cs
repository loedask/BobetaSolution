using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

public interface IGameRevenueService
{
  Task EnrichWithPartnerShareAsync(GameResult result, Guid winnerPlayerId, CancellationToken cancellationToken = default);
}
