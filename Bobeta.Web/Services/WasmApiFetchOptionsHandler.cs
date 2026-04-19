using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Bobeta.Web.Services;

/// <summary>
/// Ensures cross-origin API requests are not satisfied from the browser HTTP cache without the Authorization header
/// (which can surface as persistent 401s after login). Adds explicit no-cache request headers alongside fetch NoStore.
/// </summary>
public sealed class WasmApiFetchOptionsHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCache(BrowserRequestCache.NoStore);
        request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache, no-store, max-age=0");
        request.Headers.TryAddWithoutValidation("Pragma", "no-cache");
        return base.SendAsync(request, cancellationToken);
    }
}
