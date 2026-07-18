using Bobeta.Application.DTOs.Portal;

namespace Bobeta.Application.Interfaces;

/// <summary>Portal utility: clear game data for seeded demo players only.</summary>
public interface IDemoAccountGamesResetService
{
  Task<DemoAccountGamesResetPreviewDto> GetPreviewAsync(CancellationToken cancellationToken = default);

  Task<DemoAccountGamesResetResultDto> ClearGamesDataAsync(CancellationToken cancellationToken = default);
}
