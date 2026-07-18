using Microsoft.JSInterop;

namespace Bobeta.Web.Tests.Infrastructure;

/// <summary>Minimal IJSRuntime for AppStateService localStorage calls in unit tests.</summary>
internal sealed class FakeJsRuntime : IJSRuntime
{
    private readonly Dictionary<string, string> _storage = new(StringComparer.Ordinal);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        object? result = identifier switch
        {
            "bobeta.getBrowserLanguage" => "en-US",
            "localStorage.getItem" => GetItem(RequireArg(args, 0)),
            "localStorage.setItem" => SetItem(RequireArg(args, 0), args is { Length: > 1 } ? args[1]?.ToString() : null),
            "localStorage.removeItem" => RemoveItem(RequireArg(args, 0)),
            _ => throw new NotSupportedException($"Unexpected JS call: {identifier}"),
        };

        if (result is null)
            return ValueTask.FromResult(default(TValue)!);

        if (result is TValue typed)
            return ValueTask.FromResult(typed);

        if (typeof(TValue) == typeof(object))
            return ValueTask.FromResult(default(TValue)!);

        throw new InvalidCastException($"Cannot cast result of '{identifier}' to {typeof(TValue).Name}.");
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
