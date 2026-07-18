using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bobeta.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bobeta.Infrastructure.Push;

/// <summary>
/// Sends FCM HTTP v1 messages (Android + iOS when APNs is linked in the Firebase project).
/// Uses a service account JWT; no FirebaseAdmin package required.
/// </summary>
public sealed class FcmPushNotificationSender(
    IHttpClientFactory httpClientFactory,
    IOptions<FcmOptions> options,
    ILogger<FcmPushNotificationSender> logger) : IPushNotificationSender
{
    public const string HttpClientName = "Fcm";

    private static readonly ConcurrentDictionary<string, (string AccessToken, DateTimeOffset ExpiresAt)> TokenCache = new();

    public async Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        if (!opts.Enabled || string.IsNullOrWhiteSpace(opts.ProjectId))
            return Array.Empty<string>();

        var distinct = tokens.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.Ordinal).ToList();
        if (distinct.Count == 0)
            return Array.Empty<string>();

        string accessToken;
        try
        {
            accessToken = await GetAccessTokenAsync(opts, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FCM access token failed; push skipped.");
            return Array.Empty<string>();
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        var invalid = new List<string>();
        var url = $"https://fcm.googleapis.com/v1/projects/{opts.ProjectId.Trim()}/messages:send";

        foreach (var token in distinct)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = JsonContent.Create(new FcmSendRequest
                {
                    Message = new FcmMessage
                    {
                        Token = token,
                        Notification = new FcmNotification { Title = title, Body = body },
                        Data = data is null ? null : new Dictionary<string, string>(data),
                        Android = new FcmAndroidConfig { Priority = "high" },
                        Apns = new FcmApnsConfig
                        {
                            Headers = new Dictionary<string, string> { ["apns-priority"] = "10" },
                            Payload = new FcmApnsPayload
                            {
                                Aps = new FcmAps { Sound = "default" }
                            }
                        }
                    }
                });

                using var response = await client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                    continue;

                var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (IsInvalidTokenError(errBody))
                {
                    invalid.Add(token);
                    logger.LogInformation("FCM token invalid/unregistered; will remove. Status={Status}", response.StatusCode);
                }
                else
                {
                    logger.LogWarning("FCM send failed status={Status} body={Body}", response.StatusCode, Truncate(errBody));
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "FCM send threw for one token.");
            }
        }

        return invalid;
    }

    private async Task<string> GetAccessTokenAsync(FcmOptions opts, CancellationToken cancellationToken)
    {
        var cacheKey = opts.ProjectId ?? "";
        if (TokenCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
            return cached.AccessToken;

        var credentialsJson = await ResolveCredentialsJsonAsync(opts, cancellationToken);
        using var credDoc = JsonDocument.Parse(credentialsJson);
        var root = credDoc.RootElement;
        var clientEmail = root.GetProperty("client_email").GetString()
            ?? throw new InvalidOperationException("FCM credentials missing client_email.");
        var privateKeyPem = root.GetProperty("private_key").GetString()
            ?? throw new InvalidOperationException("FCM credentials missing private_key.");
        var tokenUri = root.TryGetProperty("token_uri", out var tokenUriEl)
            ? tokenUriEl.GetString() ?? "https://oauth2.googleapis.com/token"
            : "https://oauth2.googleapis.com/token";

        var now = DateTimeOffset.UtcNow;
        var assertion = CreateServiceAccountJwt(clientEmail, privateKeyPem, tokenUri, now);

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUri)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = assertion
            })
        };

        using var tokenResponse = await client.SendAsync(tokenRequest, cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();
        await using var stream = await tokenResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var tokenDoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("FCM token response missing access_token.");
        var expiresIn = tokenDoc.RootElement.TryGetProperty("expires_in", out var expEl) ? expEl.GetInt32() : 3600;

        TokenCache[cacheKey] = (accessToken, now.AddSeconds(expiresIn));
        return accessToken;
    }

    private static async Task<string> ResolveCredentialsJsonAsync(FcmOptions opts, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(opts.CredentialsJson))
            return opts.CredentialsJson;

        if (!string.IsNullOrWhiteSpace(opts.CredentialsPath))
        {
            var path = opts.CredentialsPath.Trim();
            if (!File.Exists(path))
                throw new FileNotFoundException("FCM credentials file not found.", path);
            return await File.ReadAllTextAsync(path, cancellationToken);
        }

        throw new InvalidOperationException("FCM is enabled but CredentialsJson/CredentialsPath is not set.");
    }

    private static string CreateServiceAccountJwt(string clientEmail, string privateKeyPem, string tokenUri, DateTimeOffset now)
    {
        var headerJson = """{"alg":"RS256","typ":"JWT"}""";
        var iat = now.ToUnixTimeSeconds();
        var exp = iat + 3600;
        var payloadJson =
            $"{{\"iss\":\"{EscapeJson(clientEmail)}\",\"scope\":\"https://www.googleapis.com/auth/firebase.messaging\",\"aud\":\"{EscapeJson(tokenUri)}\",\"iat\":{iat},\"exp\":{exp}}}";

        var header = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signingInput = $"{header}.{payload}";

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem.AsSpan());
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static bool IsInvalidTokenError(string body) =>
        body.Contains("UNREGISTERED", StringComparison.OrdinalIgnoreCase)
        || body.Contains("INVALID_ARGUMENT", StringComparison.OrdinalIgnoreCase)
            && body.Contains("token", StringComparison.OrdinalIgnoreCase)
        || body.Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase);

    private static string Truncate(string s) => s.Length <= 400 ? s : s[..400];

    private static string EscapeJson(string s) => s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class FcmSendRequest
    {
        [JsonPropertyName("message")]
        public FcmMessage Message { get; set; } = new();
    }

    private sealed class FcmMessage
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = "";

        [JsonPropertyName("notification")]
        public FcmNotification? Notification { get; set; }

        [JsonPropertyName("data")]
        public Dictionary<string, string>? Data { get; set; }

        [JsonPropertyName("android")]
        public FcmAndroidConfig? Android { get; set; }

        [JsonPropertyName("apns")]
        public FcmApnsConfig? Apns { get; set; }
    }

    private sealed class FcmNotification
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("body")]
        public string Body { get; set; } = "";
    }

    private sealed class FcmAndroidConfig
    {
        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "high";
    }

    private sealed class FcmApnsConfig
    {
        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("payload")]
        public FcmApnsPayload? Payload { get; set; }
    }

    private sealed class FcmApnsPayload
    {
        [JsonPropertyName("aps")]
        public FcmAps? Aps { get; set; }
    }

    private sealed class FcmAps
    {
        [JsonPropertyName("sound")]
        public string Sound { get; set; } = "default";
    }
}
