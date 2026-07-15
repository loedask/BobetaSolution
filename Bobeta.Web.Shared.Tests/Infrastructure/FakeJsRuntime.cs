using System.Text.Json;
using Microsoft.JSInterop;

namespace Bobeta.Web.Shared.Tests.Infrastructure;

/// <summary>Minimal IJSRuntime for localStorage + browser language used by AppStateService.</summary>
public sealed class FakeJsRuntime : IJSRuntime
{
    private readonly Dictionary<string, string> _storage = new(StringComparer.Ordinal);

    public string? BrowserLanguage { get; set; } = "en-US";

    public IReadOnlyDictionary<string, string> Storage => _storage;

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        object? result = identifier switch
        {
            "bobeta.getBrowserLanguage" => BrowserLanguage,
            "localStorage.getItem" => GetItem(RequireArg(args, 0)),
            "localStorage.setItem" => SetItem(RequireArg(args, 0), args is { Length: > 1 } ? args[1]?.ToString() : null),
            "localStorage.removeItem" => RemoveItem(RequireArg(args, 0)),
            _ => throw new NotSupportedException($"Unexpected JS call: {identifier}"),
        };

        if (result is null)
            return ValueTask.FromResult(default(TValue)!);

        if (result is TValue typed)
            return ValueTask.FromResult(typed);

        // InvokeVoidAsync uses object as TValue
        if (typeof(TValue) == typeof(object))
            return ValueTask.FromResult(default(TValue)!);

        throw new InvalidCastException($"Cannot cast result of '{identifier}' to {typeof(TValue).Name}.");
    }

    public void SetStoredJson(string key, object value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        _storage[key] = json;
    }

    private string? GetItem(string key) =>
        _storage.TryGetValue(key, out var value) ? value : null;

    private object? SetItem(string key, string? value)
    {
        if (value is null)
            _storage.Remove(key);
        else
            _storage[key] = value;
        return null;
    }

    private object? RemoveItem(string key)
    {
        _storage.Remove(key);
        return null;
    }

    private static string RequireArg(object?[]? args, int index)
    {
        if (args is null || args.Length <= index || args[index] is null)
            throw new ArgumentException($"Missing JS argument at index {index}.");
        return args[index]!.ToString()!;
    }
}
