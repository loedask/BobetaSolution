using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Bobeta.Web.Services;

/// <summary>
/// Ensures cross-origin API GETs are not satisfied from the browser HTTP cache without the Authorization header
/// (which can surface as persistent 401s after login). POSTs are less affected; this handler is cheap for all requests.
/// </summary>
public sealed class WasmApiFetchOptionsHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCache(BrowserRequestCache.NoStore);
        return base.SendAsync(request, cancellationToken);
    }
}
