using Bobeta.Client.Contracts;
using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Services;
using Bobeta.Web.Shared.Services;
using Bobeta.Web.Shared.Services.Realtime;
using Bobeta.Web.Tests.Infrastructure;
using Bobeta.Web.ViewModels.Games;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bobeta.Web.Tests.Games;

public sealed class GamePlayViewModelForfeitTests
{
    [Fact]
    public async Task RequestForfeitConfirm_WhenInProgressMatch_ShowsConfirm()
    {
        await using var harness = await ForfeitHarness.CreateInProgressAsync();

        Assert.True(harness.Vm.ShouldConfirmLeave);
        harness.Vm.RequestForfeitConfirm();
        Assert.True(harness.Vm.ShowForfeitConfirm);

        harness.Vm.DismissForfeitConfirm();
        Assert.False(harness.Vm.ShowForfeitConfirm);
        Assert.True(harness.Vm.ShouldConfirmLeave);
    }

    [Fact]
    public async Task RequestForfeitConfirm_WhenWaitingForOpponent_DoesNotShowConfirm()
    {
        await using var harness = await ForfeitHarness.CreateWaitingAsync();

        Assert.False(harness.Vm.ShouldConfirmLeave);
        harness.Vm.RequestForfeitConfirm();
        Assert.False(harness.Vm.ShowForfeitConfirm);
    }

    [Fact]
    public async Task ConfirmForfeitAndLeaveAsync_CallsForfeitAndNavigatesWithForfeit()
    {
        await using var harness = await ForfeitHarness.CreateInProgressAsync();
        harness.Vm.RequestForfeitConfirm();

        await harness.Vm.ConfirmForfeitAndLeaveAsync();

        Assert.True(harness.Http.ForfeitCalled);
        Assert.False(harness.Vm.ShowForfeitConfirm);
        Assert.False(harness.Vm.ShouldConfirmLeave);
        Assert.Contains("ended=forfeit", harness.Nav.LastUri, StringComparison.Ordinal);
        Assert.Contains("forfeit", harness.Nav.LastUri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HubForfeit_WhenIAmLoser_NavigatesWithForfeit()
    {
        await using var harness = await ForfeitHarness.CreateInProgressAsync(withHub: true);
        var winnerId = Guid.NewGuid();

        harness.Hub!.RaiseGameEndedByForfeitForTests(winnerId, harness.MyPlayerId);
        await WaitUntilAsync(() => harness.Nav.LastUri is not null);

        Assert.Contains("ended=forfeit", harness.Nav.LastUri!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HubForfeit_WhenIAmWinner_SetsEndedByForfeitAndAllowsLeave()
    {
        await using var harness = await ForfeitHarness.CreateInProgressAsync(withHub: true);
        var loserId = Guid.NewGuid();
        harness.Games.NextState = new GameStateViewModel
        {
            SessionId = harness.SessionId,
            Variant = GameVariant.Makopa,
            WaitingForGameStart = false,
            GameOver = true,
            WinnerPlayerId = harness.MyPlayerId,
            LobbyPotAmount = 400m,
            OpponentDisplayName = "Rival",
            MyCards = []
        };

        harness.Hub!.RaiseGameEndedByForfeitForTests(harness.MyPlayerId, loserId);
        await WaitUntilAsync(() => harness.Vm.EndedByForfeit);

        Assert.True(harness.Vm.EndedByForfeit);
        Assert.False(harness.Vm.ShouldConfirmLeave);
        Assert.Null(harness.Nav.LastUri);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;
            await Task.Delay(20);
        }

        Assert.True(condition(), "Timed out waiting for condition.");
    }

    private sealed class ForfeitHarness : IAsyncDisposable
    {
        public required GamePlayViewModel Vm { get; init; }
        public required FakeNavigationManager Nav { get; init; }
        public required RecordingHttpHandler Http { get; init; }
        public required FakeGameService Games { get; init; }
        public required Guid MyPlayerId { get; init; }
        public required Guid SessionId { get; init; }
        public GameHubClient? Hub { get; init; }

        public static Task<ForfeitHarness> CreateInProgressAsync(bool withHub = false) =>
            CreateAsync(waiting: false, withHub);

        public static Task<ForfeitHarness> CreateWaitingAsync() =>
            CreateAsync(waiting: true, withHub: false);

        private static async Task<ForfeitHarness> CreateAsync(bool waiting, bool withHub)
        {
            var myId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();
            var games = new FakeGameService
            {
                NextState = new GameStateViewModel
                {
                    SessionId = sessionId,
                    Variant = GameVariant.Makopa,
                    WaitingForGameStart = waiting,
                    GameOver = false,
                    LobbyPotAmount = 400m,
                    OpponentDisplayName = waiting ? null : "Rival",
                    CurrentTurnPlayerId = myId,
                    MyCards = waiting ? [] : ["Heart_2", "Spade_3"]
                }
            };
            var http = new RecordingHttpHandler();
            var gamePlay = new GamePlayService(new HttpClient(http) { BaseAddress = new Uri("https://api.test/") });
            var js = new FakeJsRuntime();
            var appState = new AppStateService(new LocalStorageService(js));
            appState.SetPlayer(myId, "Me", "token");
            var nav = new FakeNavigationManager();

            GameHubClient? hub = null;
            if (withHub)
            {
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ApiBaseUrl"] = "https://api.test/"
                    })
                    .Build();
                hub = new GameHubClient(
                    new FakeTokenProvider(),
                    config,
                    new FakeWasmHostEnvironment(),
                    NullLogger<GameHubClient>.Instance);
            }

            var vm = new GamePlayViewModel(gamePlay, games, appState, nav, hub);
            await vm.LoadGameAsync(sessionId.ToString("D"));

            return new ForfeitHarness
            {
                Vm = vm,
                Nav = nav,
                Http = http,
                Games = games,
                MyPlayerId = myId,
                SessionId = sessionId,
                Hub = hub
            };
        }

        public async ValueTask DisposeAsync() => await Vm.DisposeAsync();
    }

    private sealed class FakeNavigationManager : NavigationManager
    {
        public string? LastUri { get; private set; }

        public FakeNavigationManager() => Initialize("https://app.test/", "https://app.test/");

        protected override void NavigateToCore(string uri, bool forceLoad) => LastUri = uri;
    }

    private sealed class RecordingHttpHandler : HttpMessageHandler
    {
        public bool ForfeitCalled { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery ?? "";
            if (path.Contains("/forfeit", StringComparison.OrdinalIgnoreCase))
            {
                ForfeitCalled = true;
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("true")
                });
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        }
    }

    private sealed class FakeGameService : IGameService
    {
        public GameStateViewModel? NextState { get; set; }

        public Task<Response<GameStateViewModel?>> GetGameStateAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Response<GameStateViewModel?>.Success(NextState));

        public Task<Response<GameSessionViewModel?>> CreateGameAsync(CreateGameRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Response<GameSessionViewModel?>> JoinGameAsync(JoinGameRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Response<bool>> ProposeBetAsync(Guid gameId, double amount, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Response<bool>> AcceptBetChangeAsync(Guid gameId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Response<IReadOnlyList<GameSessionViewModel>>> GetOpenGamesAsync(GameVariant? variant = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Response<IReadOnlyList<GameSessionViewModel>>> GetMyWaitingGamesAsync(GameVariant? variant = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Response<bool>> CancelWaitingGameAsync(Guid gameId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeTokenProvider : IAccessTokenProvider
    {
        public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>("test-token");
    }

    private sealed class FakeWasmHostEnvironment : IWebAssemblyHostEnvironment
    {
        public string Environment => "Development";
        public string BaseAddress => "https://api.test/";
    }
}
