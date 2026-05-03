using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Games;

namespace Bobeta.Web.Services.Realtime;

/// <summary>SignalR client for real-time game hub: connect, join session, play card, receive state/opponent/result. Auto-reconnects and rejoins.</summary>
public class GameHubClient
{
    private HubConnection? _connection;
    private string? _currentSessionId;
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly string _hubBaseUrl;
    private CancellationTokenSource? _reconnectCts;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public GameHubClient(IAccessTokenProvider tokenProvider, IConfiguration configuration, IWebAssemblyHostEnvironment hostEnvironment)
    {
        _tokenProvider = tokenProvider;
        // Match Program.cs: same API host as HttpClient (avoid empty relative hub URL when ApiBaseUrl key is missing).
        var api = configuration["ApiBaseUrl"] ?? hostEnvironment.BaseAddress;
        if (string.IsNullOrWhiteSpace(api))
            api = hostEnvironment.BaseAddress;
        _hubBaseUrl = api.Trim().TrimEnd('/');
    }

    /// <summary>Fired when the server broadcasts full game state (e.g. after reconnect).</summary>
    public event Action<GameStateViewModel>? OnGameStateUpdated;

    /// <summary>Fired when another player plays a card (mover id + card string e.g. "Heart_2").</summary>
    public event Action<Guid, string>? OnOpponentMove;

    /// <summary>Fired when the game ends (winner player id).</summary>
    public event Action<Guid?>? OnGameResult;

    /// <summary>Fired when connection state changes (e.g. reconnecting).</summary>
    public event Action<HubConnectionState>? OnConnectionStateChanged;

    /// <summary>Fired after reconnecting; subscriber should reload game state.</summary>
    public event Action? OnReconnected;

    /// <summary>Fired when the server signals the session changed in a way that needs a full reload (e.g. game dealt after second player joined).</summary>
    public event Action? OnGameStarted;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    /// <summary>Connects to the hub (if needed), joins the session group, and subscribes to events. Automatic reconnect on disconnect.</summary>
    public async Task ConnectAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
            return;

        _currentSessionId = sessionId;
        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();

        if (_connection != null)
        {
            await _connection.StopAsync(CancellationToken.None);
            await _connection.DisposeAsync();
            _connection = null;
        }

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        var url = $"{_hubBaseUrl}/hubs/game?access_token={Uri.EscapeDataString(token ?? "")}";

        _connection = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .AddJsonProtocol(options => options.PayloadSerializerOptions = JsonOptions)
            .Build();

        _connection.Closed += OnClosedAsync;
        _connection.Reconnecting += OnReconnecting;
        _connection.Reconnected += OnReconnectedAsync;

        _connection.On<JsonElement>("GameState", payload =>
        {
            try
            {
                var state = JsonSerializer.Deserialize<GameStateViewModel>(payload.GetRawText(), JsonOptions);
                if (state != null)
                    OnGameStateUpdated?.Invoke(state);
            }
            catch { /* ignore */ }
        });

        _connection.On<Guid, string>("NotifyOpponentMove", (moverPlayerId, cardSuitRank) =>
        {
            OnOpponentMove?.Invoke(moverPlayerId, cardSuitRank);
        });

        _connection.On<Guid?>("GameResult", winnerId =>
        {
            OnGameResult?.Invoke(winnerId);
        });

        _connection.On("GameStarted", () =>
        {
            OnGameStarted?.Invoke();
        });

        await StartAndJoinAsync(sessionGuid, cancellationToken);
    }

    private async Task StartAndJoinAsync(Guid sessionGuid, CancellationToken cancellationToken)
    {
        if (_connection == null) return;
        await _connection.StartAsync(cancellationToken);
        OnConnectionStateChanged?.Invoke(_connection.State);
        await _connection.InvokeAsync("JoinGameSession", sessionGuid, cancellationToken);
    }

    private async Task OnClosedAsync(Exception? ex)
    {
        OnConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
        await TryReconnectAndRejoinAsync();
    }

    private Task OnReconnecting(Exception? ex)
    {
        OnConnectionStateChanged?.Invoke(HubConnectionState.Reconnecting);
        return Task.CompletedTask;
    }

    private async Task OnReconnectedAsync(string? arg)
    {
        OnConnectionStateChanged?.Invoke(HubConnectionState.Connected);
        if (_currentSessionId != null && Guid.TryParse(_currentSessionId, out var sessionGuid) && _connection != null)
        {
            await _connection.InvokeAsync("JoinGameSession", sessionGuid);
            OnReconnected?.Invoke();
        }
    }

    private async Task TryReconnectAndRejoinAsync()
    {
        if (_disposed || _currentSessionId == null || _reconnectCts == null) return;
        var token = _reconnectCts.Token;
        for (int i = 0; i < 5 && !token.IsCancellationRequested; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2 + i), token);
            if (token.IsCancellationRequested) return;
            try
            {
                await ConnectAsync(_currentSessionId, token);
                return;
            }
            catch { /* retry */ }
        }
    }

    /// <summary>Sends a card play to the hub (broadcasts to others). Card format e.g. "Heart_2".</summary>
    public async Task PlayCardAsync(string sessionId, string suit, int rank, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid) || _connection?.State != HubConnectionState.Connected)
            return;
        var cardSuitRank = $"{suit}_{rank}";
        await _connection.InvokeAsync("PlayCard", sessionGuid, cardSuitRank, cancellationToken);
    }

    /// <summary>Leaves the session group and optionally disposes the connection.</summary>
    public async Task DisconnectAsync()
    {
        _currentSessionId = null;
        _reconnectCts?.Cancel();
        if (_connection != null)
        {
            await _connection.StopAsync(CancellationToken.None);
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public void Dispose() => _disposed = true;
}
