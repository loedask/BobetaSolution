using Microsoft.JSInterop;
using System.Text.Json;
using Bobeta.Web.Shared.State;

namespace Bobeta.Web.Shared.Services;

public class LocalStorageService
{
    private readonly IJSRuntime _js;
    private const string KeyPrefix = "bobeta_";
    private const string StateKey = KeyPrefix + "app_state";
    private const string LocaleKey = KeyPrefix + "locale";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public LocalStorageService(IJSRuntime js) => _js = js;

    public async Task SaveStateAsync(AppState state, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        await _js.InvokeVoidAsync("localStorage.setItem", ct, StateKey, json);
    }

    public async Task<AppState?> LoadStateAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", ct, StateKey);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<AppState>(json, JsonOptions);
        }
        catch { return null; }
    }

    public async Task SetLocaleAsync(string locale, CancellationToken ct = default)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", ct, LocaleKey, locale);
    }

    public async Task<string?> GetLocaleAsync(CancellationToken ct = default)
    {
        return await _js.InvokeAsync<string?>("localStorage.getItem", ct, LocaleKey);
    }

    /// <summary>Reads the browser UI language (navigator.language).</summary>
    public async Task<string?> GetBrowserLanguageAsync(CancellationToken ct = default)
    {
        try
        {
            return await _js.InvokeAsync<string?>("bobeta.getBrowserLanguage", ct);
        }
        catch
        {
            return null;
        }
    }
}
