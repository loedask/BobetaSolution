using Bobeta.Application.DTOs.Portal;

namespace Bobeta.Application.Interfaces;

/// <summary>Persistence for clearing game-related data on hard-coded demo player accounts only.</summary>
public interface IDemoAccountGamesResetRepository
{
  Task<DemoAccountGamesResetPreviewDto> GetPreviewAsync(
      IReadOnlyList<string> demoPhoneNumbers,
      CancellationToken cancellationToken = default);

  Task<DemoAccountGamesResetResultDto> ClearGamesDataAsync(
      IReadOnlyList<string> demoPhoneNumbers,
      decimal resetWalletBalance,
      CancellationToken cancellationToken = default);
}
