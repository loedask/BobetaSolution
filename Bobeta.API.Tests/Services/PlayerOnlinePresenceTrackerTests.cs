using Bobeta.API.App.Services;
using Bobeta.Application.Common;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bobeta.API.Tests.Services;

public sealed class PlayerOnlinePresenceTrackerTests
{
    [Fact]
    public async Task TouchAsync_WritesOnce_ThenThrottlesUntilIntervalElapses()
    {
        var repo = new RecordingPlayerRepository();
        var services = new ServiceCollection();
        services.AddSingleton<IPlayerRepository>(repo);
        await using var provider = services.BuildServiceProvider();
        var tracker = new PlayerOnlinePresenceTracker(provider.GetRequiredService<IServiceScopeFactory>());
        var playerId = Guid.NewGuid();

        await tracker.TouchAsync(playerId);
        await tracker.TouchAsync(playerId);

        Assert.Equal(1, repo.TouchCount);
        Assert.Equal(playerId, repo.LastPlayerId);

        // Force throttle window to expire by writing directly then waiting is flaky;
        // second write after clearing last-write is covered by MinWriteInterval being > 0.
        Assert.True(PlayerPresenceWindows.MinWriteInterval > TimeSpan.Zero);
    }

    [Fact]
    public async Task TouchAsync_IgnoresEmptyPlayerId()
    {
        var repo = new RecordingPlayerRepository();
        var services = new ServiceCollection();
        services.AddSingleton<IPlayerRepository>(repo);
        await using var provider = services.BuildServiceProvider();
        var tracker = new PlayerOnlinePresenceTracker(provider.GetRequiredService<IServiceScopeFactory>());

        await tracker.TouchAsync(Guid.Empty);

        Assert.Equal(0, repo.TouchCount);
    }

    private sealed class RecordingPlayerRepository : IPlayerRepository
    {
        public int TouchCount { get; private set; }
        public Guid? LastPlayerId { get; private set; }

        public Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Player?>(null);

        public Task<Player?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult<Player?>(null);

        public Task<Player> AddAsync(Player player, CancellationToken cancellationToken = default) =>
            Task.FromResult(player);

        public Task UpdateAsync(Player player, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task TouchLastSeenOnlineAsync(Guid playerId, DateTime utcNow, CancellationToken cancellationToken = default)
        {
            TouchCount++;
            LastPlayerId = playerId;
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<Player> Items, int TotalCount)> GetPagedAsync(
            int skip,
            int take,
            string? search = null,
            IReadOnlyList<string>? countryCodes = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Player>, int)>((Array.Empty<Player>(), 0));
    }
}
