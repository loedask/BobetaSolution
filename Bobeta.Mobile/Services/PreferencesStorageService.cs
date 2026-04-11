using System.Text.Json;
using Bobeta.Mobile.State;

namespace Bobeta.Mobile.Services;

public class PreferencesStorageService
{
    private const string KeyPrefix = "bobeta_";
    private const string StateKey = KeyPrefix + "app_state";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task SaveStateAsync(AppState state, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        Preferences.Default.Set(StateKey, json);
        return Task.CompletedTask;
    }

    public Task<AppState?> LoadStateAsync(CancellationToken ct = default)
    {
        var json = Preferences.Default.Get(StateKey, (string?)null);
        if (string.IsNullOrEmpty(json))
            return Task.FromResult<AppState?>(null);
        try
        {
            return Task.FromResult(JsonSerializer.Deserialize<AppState>(json, JsonOptions));
        }
        catch
        {
            return Task.FromResult<AppState?>(null);
        }
    }
}
