using Bobeta.Client.Contracts;

namespace Bobeta.Client.Services.Base;

/// <summary>
/// Delegating handler that attaches the current bearer token to outbound HTTP requests.
/// Register when using AddBobetaClient with useBearerToken: true and IAccessTokenProvider registered.
/// </summary>
public class BearerTokenHandler : DelegatingHandler
{
    private readonly IAccessTokenProvider _tokenProvider;

    public BearerTokenHandler(IAccessTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
