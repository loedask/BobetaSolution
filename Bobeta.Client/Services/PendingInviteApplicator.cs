using Bobeta.Client.Services;

namespace Bobeta.Client.Services;

/// <summary>Applies a locally stored invite code after the player authenticates.</summary>
public static class PendingInviteApplicator
{
  public static async Task TryApplyAsync(
      InfluencerService influencerService,
      string? pendingCode,
      Action clearPending,
      Func<Task> persistAsync,
      CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(pendingCode))
      return;

    var res = await influencerService.ApplyCodeAsync(pendingCode, cancellationToken).ConfigureAwait(false);
    // Clear local pending whether success or already-used — avoid retry loops.
    clearPending();
    await persistAsync().ConfigureAwait(false);
    _ = res;
  }
}
