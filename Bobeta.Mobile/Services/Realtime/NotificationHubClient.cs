using System.Text.Json;
using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Notifications;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bobeta.Mobile.Services.Realtime;

/// <summary>SignalR client for the player notification inbox hub and presence heartbeats.</summary>
public sealed class NotificationHubClient : IAsyncDisposable
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(45);

    private readonly IAccessTokenProvider _tokenProvider;
    private readonly string _hubBaseUrl;
    private readonly ILogger<NotificationHubClient>? _logger;
    private HubConnection? _connection;
    private CancellationTokenSource? _heartbeatCts;
    private bool _disposed;

    public NotificationHubClient(
        IAccessTokenProvider tokenProvider,
        IConfiguration configuration,
        ILogger<NotificationHubClient>? logger = null)
    {
        _tokenProvider = tokenProvider;
        _logger = logger;
        _hubBaseUrl = (configuration["ApiBaseUrl"] ?? "").Trim().TrimEnd('/');
    }

    public event Action<NotificationViewModel>? OnNotificationReceived;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return;
        if (_connection?.State == HubConnectionState.Connected) return;

        StopHeartbeat();

        if (_connection != null)
        {
            await _connection.StopAsync(CancellationToken.None);
            await _connection.DisposeAsync();
            _connection = null;
        }

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_hubBaseUrl))
            return;

        var url = $"{_hubBaseUrl}/hubs/notifications?access_token={Uri.EscapeDataString(token)}";
        _connection = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<JsonElement>("NotificationReceived", payload =>
        {
            try
            {
                var item = MapPayload(payload);
                if (item != null)
                    OnNotificationReceived?.Invoke(item);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to deserialize NotificationReceived payload.");
            }
        });

        _connection.Reconnected += async _ =>
        {
            var connection = _connection;
            if (connection is null)
                return;
            try
            {
                await connection.InvokeAsync("Heartbeat", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Presence heartbeat after reconnect failed.");
            }
        };

        await _connection.StartAsync(cancellationToken);
        StartHeartbeat();
    }

    public async Task DisconnectAsync()
    {
        StopHeartbeat();
        if (_connection == null) return;
        await _connection.StopAsync(CancellationToken.None);
        await _connection.DisposeAsync();
        _connection = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await DisconnectAsync();
    }

    private void StartHeartbeat()
    {
        StopHeartbeat();
        if (_connection == null) return;
        _heartbeatCts = new CancellationTokenSource();
        _ = RunHeartbeatAsync(_heartbeatCts.Token);
    }

    private void StopHeartbeat()
    {
        _heartbeatCts?.Cancel();
        _heartbeatCts?.Dispose();
        _heartbeatCts = null;
    }

    private async Task RunHeartbeatAsync(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(HeartbeatInterval);
            while (await timer.WaitForNextTickAsync(token))
            {
                var connection = _connection;
                if (connection?.State != HubConnectionState.Connected)
                    continue;
                try
                {
                    await connection.InvokeAsync("Heartbeat", token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Presence heartbeat failed.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Disconnect / dispose.
        }
    }

    private static NotificationViewModel? MapPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
            return null;

        if (!payload.TryGetProperty("id", out var idEl) || !idEl.TryGetGuid(out var id) || id == Guid.Empty)
            return null;

        return new NotificationViewModel
        {
            Id = id,
            Type = ReadType(payload),
            ActorName = payload.TryGetProperty("actorName", out var actorEl) && actorEl.ValueKind == JsonValueKind.String
                ? actorEl.GetString()
                : null,
            Amount = payload.TryGetProperty("amount", out var amountEl) && amountEl.ValueKind == JsonValueKind.Number
                ? amountEl.GetDecimal()
                : null,
            RelatedEntityId = payload.TryGetProperty("relatedEntityId", out var relEl) && relEl.TryGetGuid(out var rel)
                ? rel
                : null,
            DeepLink = payload.TryGetProperty("deepLink", out var linkEl) && linkEl.ValueKind == JsonValueKind.String
                ? linkEl.GetString()
                : null,
            IsRead = payload.TryGetProperty("isRead", out var readEl) && readEl.ValueKind == JsonValueKind.True,
            CreatedAt = payload.TryGetProperty("createdAt", out var createdEl) && createdEl.TryGetDateTime(out var created)
                ? created
                : DateTime.UtcNow
        };
    }

    private static string ReadType(JsonElement payload)
    {
        if (!payload.TryGetProperty("type", out var typeEl))
            return "Unknown";
        if (typeEl.ValueKind == JsonValueKind.String)
            return typeEl.GetString() ?? "Unknown";
        if (typeEl.ValueKind == JsonValueKind.Number && typeEl.TryGetInt32(out var n))
        {
            return n switch
            {
                0 => "OpponentJoined",
                1 => "GameWon",
                2 => "GameLost",
                3 => "DepositSuccess",
                4 => "DepositFailed",
                5 => "WithdrawSuccess",
                6 => "WithdrawFailed",
                7 => "GameInvite",
                8 => "BetProposal",
                _ => "Unknown"
            };
        }

        return "Unknown";
    }
}
