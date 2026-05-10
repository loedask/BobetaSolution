using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
#if ANDROID
using Xamarin.Android.Net;
#endif
using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Serialization;

namespace Bobeta.Mobile.Services.Realtime;

/// <summary>SignalR client for real-time game hub.</summary>
public class GameHubClient
{
    private HubConnection? _connection;
    private string? _currentSessionId;
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly string _hubBaseUrl;
    private readonly ILogger<GameHubClient>? _logger;
    private CancellationTokenSource? _reconnectCts;
    private bool _disposed;
    private long _gameStateReceivedVersion;

    private static readonly JsonSerializerOptions JsonProtocolOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GameHubClient(IAccessTokenProvider tokenProvider, IConfiguration configuration, ILogger<GameHubClient>? logger = null)
    {
        _tokenProvider = tokenProvider;
        _logger = logger;
        _hubBaseUrl = (configuration["ApiBaseUrl"] ?? "").TrimEnd('/');
    }

    public event Action<GameStateViewModel>? OnGameStateUpdated;
    public event Action<Guid, string>? OnOpponentMove;
    public event Action<Guid?>? OnGameResult;
    public event Action<HubConnectionState>? OnConnectionStateChanged;
    public event Action? OnReconnected;

    public event Action? OnGameStarted;

    public event Action<InactivityWarningPayload>? OnInactivityWarning;
    public event Action? OnInactivityWarningDismissed;
    public event Action? OnGameEndedByInactivity;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

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
            .WithUrl(url, options =>
            {
#if ANDROID
                options.HttpMessageHandlerFactory = _ => new AndroidMessageHandler();
#endif
            })
            .WithAutomaticReconnect()
            .AddJsonProtocol(options => options.PayloadSerializerOptions = JsonProtocolOptions)
            .Build();

        _connection.Closed += OnClosedAsync;
        _connection.Reconnecting += OnReconnecting;
        _connection.Reconnected += OnReconnectedAsync;

        _connection.On<JsonElement>("GameState", payload =>
        {
            try
            {
                var state = JsonSerializer.Deserialize(payload, GameStateSignalRJsonContext.Default.GameStateViewModel);
                if (state == null)
                {
                    _logger?.LogWarning("GameState payload deserialized to null.");
                    return;
                }

                var ver = Interlocked.Increment(ref _gameStateReceivedVersion);
                _logger?.LogDebug(
                    "GameState received v={Version} session={SessionId} turn={Turn} last={Last} gameOver={GameOver} utc={Utc:o}",
                    ver, state.SessionId, state.CurrentTurnPlayerId, state.LastPlayedCard ?? "(none)", state.GameOver, DateTime.UtcNow);
                OnGameStateUpdated?.Invoke(state);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "GameState deserialize failed (raw length {Length}).", payload.GetRawText().Length);
            }
        });

        _connection.On<Guid, string>("NotifyOpponentMove", (moverPlayerId, cardSuitRank) =>
        {
            _logger?.LogDebug("NotifyOpponentMove mover={MoverId} card={Card} utc={Utc:o}", moverPlayerId, cardSuitRank, DateTime.UtcNow);
            OnOpponentMove?.Invoke(moverPlayerId, cardSuitRank);
        });

        _connection.On<Guid?>("GameResult", winnerId =>
        {
            _logger?.LogDebug("GameResult winner={WinnerId} utc={Utc:o}", winnerId, DateTime.UtcNow);
            OnGameResult?.Invoke(winnerId);
        });

        _connection.On("GameStarted", () =>
        {
            OnGameStarted?.Invoke();
        });

        _connection.On<JsonElement>("InactivityWarning", payload =>
        {
            try
            {
                var model = JsonSerializer.Deserialize<InactivityWarningPayload>(payload, JsonProtocolOptions);
                if (model == null)
                    return;
                if (model.DecisionDeadlineUtc.Kind == DateTimeKind.Unspecified)
                    model.DecisionDeadlineUtc = DateTime.SpecifyKind(model.DecisionDeadlineUtc, DateTimeKind.Utc);
                OnInactivityWarning?.Invoke(model);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "InactivityWarning deserialize failed.");
            }
        });

        _connection.On("InactivityWarningDismissed", () => OnInactivityWarningDismissed?.Invoke());
        _connection.On("GameEndedByInactivity", () => OnGameEndedByInactivity?.Invoke());

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

    public async Task NotifyGameReadyForInactivityAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid) || _connection?.State != HubConnectionState.Connected)
            return;
        await _connection.InvokeAsync("NotifyGameReadyForInactivity", sessionGuid, cancellationToken);
    }

    public async Task PauseInactivityAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid) || _connection?.State != HubConnectionState.Connected)
            return;
        await _connection.InvokeAsync("PauseInactivity", sessionGuid, cancellationToken);
    }

    public async Task ResumeInactivityAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid) || _connection?.State != HubConnectionState.Connected)
            return;
        await _connection.InvokeAsync("ResumeInactivity", sessionGuid, cancellationToken);
    }

    public async Task InactivityContinueAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid) || _connection?.State != HubConnectionState.Connected)
            return;
        await _connection.InvokeAsync("InactivityContinue", sessionGuid, cancellationToken);
    }

    public async Task InactivityCancelGameAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid) || _connection?.State != HubConnectionState.Connected)
            return;
        await _connection.InvokeAsync("InactivityCancelGame", sessionGuid, cancellationToken);
    }

    public async Task PlayCardAsync(string sessionId, string suit, int rank, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid) || _connection?.State != HubConnectionState.Connected)
            return;
        var cardSuitRank = $"{suit}_{rank}";
        await _connection.InvokeAsync("PlayCard", sessionGuid, cardSuitRank, cancellationToken);
    }

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
