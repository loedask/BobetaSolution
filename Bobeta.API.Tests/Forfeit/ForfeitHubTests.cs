using System.Text.Json;
using Bobeta.API.Tests.Infrastructure;
using Bobeta.Application.DTOs.Game;
using Bobeta.Domain.Enums;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Bobeta.API.Tests.Forfeit;

public sealed class ForfeitHubTests(BobetaApiFactory factory) : IClassFixture<BobetaApiFactory>
{
    private static readonly Guid LoserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid WinnerId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public async Task ForfeitGame_WhenOutcomeAvailable_BroadcastsForfeitAndResult()
    {
        var games = factory.GameSessions;
        games.ResetForfeitTracking();
        var sessionId = Guid.NewGuid();
        games.NextForfeitOutcome = new ForfeitGameOutcome(
            sessionId, WinnerId, LoserId, 150m, GameVariant.Makopa);

        await using var connection = await CreateConnectedHubAsync(LoserId);
        var forfeitTcs = new TaskCompletionSource<(Guid Winner, Guid Loser)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resultTcs = new TaskCompletionSource<Guid?>(TaskCreationOptions.RunContinuationsAsynchronously);

        connection.On<JsonElement>("GameEndedByForfeit", payload =>
        {
            var winner = payload.GetProperty("winnerPlayerId").GetGuid();
            var loser = payload.GetProperty("loserPlayerId").GetGuid();
            forfeitTcs.TrySetResult((winner, loser));
        });
        connection.On<Guid?>("GameResult", winnerId => resultTcs.TrySetResult(winnerId));

        await connection.InvokeAsync("JoinGameSession", sessionId);
        await connection.InvokeAsync("ForfeitGame", sessionId);

        var forfeit = await forfeitTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var resultWinner = await resultTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(WinnerId, forfeit.Winner);
        Assert.Equal(LoserId, forfeit.Loser);
        Assert.Equal(WinnerId, resultWinner);
        Assert.Equal(LoserId, games.LastForfeitLoserId);
        Assert.Equal(sessionId, games.LastForfeitSessionId);
    }

    [Fact]
    public async Task ForfeitGame_WhenCannotForfeit_ThrowsHubException()
    {
        var games = factory.GameSessions;
        games.ResetForfeitTracking();
        games.NextForfeitOutcome = null;
        var sessionId = Guid.NewGuid();

        await using var connection = await CreateConnectedHubAsync(LoserId);
        await connection.InvokeAsync("JoinGameSession", sessionId);

        var ex = await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(
            () => connection.InvokeAsync("ForfeitGame", sessionId));

        Assert.Contains("cannot be forfeited", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(LoserId, games.LastForfeitLoserId);
        Assert.Equal(sessionId, games.LastForfeitSessionId);
    }

    private async Task<HubConnection> CreateConnectedHubAsync(Guid playerId)
    {
        var token = TestJwtTokens.ForPlayer(playerId);
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress!, "hubs/game"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        await connection.StartAsync();
        return connection;
    }
}
