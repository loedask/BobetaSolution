namespace Bobeta.Web;

/// <summary>Routes whose components live in the lazy-loaded <c>Bobeta.Web.Deferred</c> assembly.</summary>
internal static class LazyRouteHelper
{
    internal const string DeferredAssemblyFileName = "Bobeta.Web.Deferred.dll";

    internal static bool IsDeferredRoute(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        var segment = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault()?.ToLowerInvariant();
        return segment is "dashboard" or "history" or "join" or "create-game" or "deposit" or "withdraw" or "profile";
    }
}
