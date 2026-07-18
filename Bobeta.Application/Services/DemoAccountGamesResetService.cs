using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;

namespace Bobeta.Application.Services;

/// <summary>Clears game data for <see cref="DemoAccountConstants"/> phones only. Never touches other players.</summary>
public sealed class DemoAccountGamesResetService(
    IDemoAccountGamesResetRepository repository) : IDemoAccountGamesResetService
{
  public Task<DemoAccountGamesResetPreviewDto> GetPreviewAsync(CancellationToken cancellationToken = default) =>
      repository.GetPreviewAsync(DemoAccountConstants.PhoneNumbers.ToList(), cancellationToken);

  public Task<DemoAccountGamesResetResultDto> ClearGamesDataAsync(CancellationToken cancellationToken = default) =>
      repository.ClearGamesDataAsync(
          DemoAccountConstants.PhoneNumbers.ToList(),
          DemoAccountConstants.DemoWalletBalance,
          cancellationToken);
}
