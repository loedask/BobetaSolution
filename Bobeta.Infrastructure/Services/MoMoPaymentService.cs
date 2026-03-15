using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bobeta.Application.DTOs.Payment;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Infrastructure.MoMo;
using Microsoft.Extensions.Options;

namespace Bobeta.Infrastructure.Services;

/// <summary>MTN MoMo implementation of IPaymentService: request-to-pay (deposit), disbursement (withdrawal), status check, and callback handling.</summary>
public class MoMoPaymentService : IPaymentService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MoMoSettings _settings;
    private readonly IPaymentTransactionRepository _paymentRepository;
    private readonly IWalletService _walletService;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private string? _collectionToken;
    private DateTime _collectionTokenExpiry = DateTime.MinValue;
    private string? _disbursementToken;
    private DateTime _disbursementTokenExpiry = DateTime.MinValue;
    private readonly object _tokenLock = new();

    public MoMoPaymentService(
        IHttpClientFactory httpClientFactory,
        IOptions<MoMoSettings> settings,
        IPaymentTransactionRepository paymentRepository,
        IWalletService walletService)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _paymentRepository = paymentRepository;
        _walletService = walletService;
    }

    /// <inheritdoc />
    public async Task<PaymentTransactionDto> RequestDepositAsync(Guid playerId, string phoneNumber, decimal amount, CancellationToken cancellationToken = default)
    {
        NormalizePhone(ref phoneNumber);
        var id = Guid.NewGuid();
        var externalRef = id.ToString("D");
        var transaction = new PaymentTransaction
        {
            Id = id,
            PlayerId = playerId,
            Amount = amount,
            Currency = _settings.Currency,
            ExternalReference = externalRef,
            MoMoTransactionId = null,
            Type = PaymentTransactionType.Deposit,
            Status = PaymentTransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _paymentRepository.AddAsync(transaction, cancellationToken);

        var token = await GetCollectionTokenAsync(cancellationToken);
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var requestBody = new MoMoRequestToPayRequest
        {
            Amount = amount.ToString("F0"),
            Currency = _settings.Currency,
            ExternalId = externalRef,
            Payer = new MoMoParty { PartyIdType = "MSISDN", PartyId = phoneNumber },
            PayerMessage = "Bobeta deposit",
            PayeeNote = "Deposit to Bobeta wallet"
        };
        using var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/collection/v1_0/requesttopay")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody, _jsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Reference-Id", externalRef);
        request.Headers.Add("X-Target-Environment", _settings.TargetEnvironment);
        if (!string.IsNullOrEmpty(_settings.CallbackUrl))
            request.Headers.Add("X-Callback-Url", _settings.CallbackUrl);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            ThrowMappedMoMoException("Request-to-pay", response.StatusCode, errorBody);
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
        {
            return Map(transaction);
        }
        return Map(transaction);
    }

    /// <inheritdoc />
    public async Task<PaymentTransactionDto> RequestWithdrawalAsync(Guid playerId, string phoneNumber, decimal amount, CancellationToken cancellationToken = default)
    {
        NormalizePhone(ref phoneNumber);
        var balance = await _walletService.GetBalanceAsync(playerId, cancellationToken);
        if (balance.Balance < amount)
            throw new InvalidOperationException("Insufficient balance.");

        var id = Guid.NewGuid();
        var externalRef = id.ToString("D");
        var transaction = new PaymentTransaction
        {
            Id = id,
            PlayerId = playerId,
            Amount = amount,
            Currency = _settings.Currency,
            ExternalReference = externalRef,
            MoMoTransactionId = null,
            Type = PaymentTransactionType.Withdrawal,
            Status = PaymentTransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _paymentRepository.AddAsync(transaction, cancellationToken);

        var token = await GetDisbursementTokenAsync(cancellationToken);
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var requestBody = new MoMoTransferRequest
        {
            Amount = amount.ToString("F0"),
            Currency = _settings.Currency,
            ExternalId = externalRef,
            Payee = new MoMoParty { PartyIdType = "MSISDN", PartyId = phoneNumber },
            PayerMessage = "Bobeta withdrawal",
            PayeeNote = "Withdrawal from Bobeta wallet"
        };
        using var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/disbursement/v1_0/transfer")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody, _jsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Reference-Id", externalRef);
        request.Headers.Add("X-Target-Environment", _settings.TargetEnvironment);
        if (!string.IsNullOrEmpty(_settings.CallbackUrl))
            request.Headers.Add("X-Callback-Url", _settings.CallbackUrl);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            transaction.Status = PaymentTransactionStatus.Failed;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(transaction, cancellationToken);
            ThrowMappedMoMoException("Transfer", response.StatusCode, errorBody);
        }
        return Map(transaction);
    }

    /// <inheritdoc />
    public async Task<PaymentTransactionDto?> CheckTransactionStatusAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await _paymentRepository.GetByIdAsync(transactionId, cancellationToken);
        if (transaction == null) return null;
        if (transaction.Status != PaymentTransactionStatus.Pending)
            return Map(transaction);

        string? status;
        if (transaction.Type == PaymentTransactionType.Deposit)
        {
            var token = await GetCollectionTokenAsync(cancellationToken);
            status = await GetRequestToPayStatusAsync(transaction.ExternalReference, token, cancellationToken);
        }
        else
        {
            var token = await GetDisbursementTokenAsync(cancellationToken);
            status = await GetTransferStatusAsync(transaction.ExternalReference, token, cancellationToken);
        }
        if (status != null)
        {
            transaction.Status = MapMoMoStatus(status);
            if (transaction.Type == PaymentTransactionType.Deposit && transaction.Status == PaymentTransactionStatus.Success)
            {
                await _walletService.DepositAsync(transaction.PlayerId, transaction.Amount, cancellationToken);
            }
            if (transaction.Type == PaymentTransactionType.Withdrawal && transaction.Status == PaymentTransactionStatus.Success)
            {
                await _walletService.WithdrawAsync(transaction.PlayerId, transaction.Amount, cancellationToken);
            }
            transaction.UpdatedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(transaction, cancellationToken);
        }
        return Map(transaction);
    }

    /// <inheritdoc />
    public async Task HandleMoMoCallbackAsync(MoMoCallbackRequest callbackData, CancellationToken cancellationToken = default)
    {
        if (callbackData == null || string.IsNullOrEmpty(callbackData.ReferenceId))
            return;
        var transaction = await _paymentRepository.GetByExternalReferenceAsync(callbackData.ReferenceId, cancellationToken);
        if (transaction == null) return;
        if (transaction.Status != PaymentTransactionStatus.Pending) return;

        var status = callbackData.Status ?? "PENDING";
        transaction.Status = MapMoMoStatus(status);
        transaction.MoMoTransactionId = callbackData.FinancialTransactionId;
        transaction.UpdatedAt = DateTime.UtcNow;
        await _paymentRepository.UpdateAsync(transaction, cancellationToken);

        if (transaction.Type == PaymentTransactionType.Deposit && transaction.Status == PaymentTransactionStatus.Success)
        {
            await _walletService.DepositAsync(transaction.PlayerId, transaction.Amount, cancellationToken);
        }
        if (transaction.Type == PaymentTransactionType.Withdrawal && transaction.Status == PaymentTransactionStatus.Success)
        {
            await _walletService.WithdrawAsync(transaction.PlayerId, transaction.Amount, cancellationToken);
        }
    }

    private async Task<string> GetCollectionTokenAsync(CancellationToken cancellationToken)
    {
        lock (_tokenLock)
        {
            if (_collectionToken != null && _collectionTokenExpiry > DateTime.UtcNow.AddSeconds(60))
                return _collectionToken;
        }
        var token = await CreateTokenAsync("collection", _settings.CollectionPrimaryKey, cancellationToken);
        lock (_tokenLock)
        {
            _collectionToken = token;
            _collectionTokenExpiry = DateTime.UtcNow.AddSeconds(3500);
        }
        return token;
    }

    private async Task<string> GetDisbursementTokenAsync(CancellationToken cancellationToken)
    {
        lock (_tokenLock)
        {
            if (_disbursementToken != null && _disbursementTokenExpiry > DateTime.UtcNow.AddSeconds(60))
                return _disbursementToken;
        }
        var token = await CreateTokenAsync("disbursement", _settings.DisbursementPrimaryKey, cancellationToken);
        lock (_tokenLock)
        {
            _disbursementToken = token;
            _disbursementTokenExpiry = DateTime.UtcNow.AddSeconds(3500);
        }
        return token;
    }

    private async Task<string> CreateTokenAsync(string product, string subscriptionKey, CancellationToken cancellationToken)
    {
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ApiUser}:{_settings.ApiKey}"));
        using var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/{product}/token/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<MoMoTokenResponse>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Invalid token response.");
        return tokenResponse.AccessToken;
    }

    private async Task<string?> GetRequestToPayStatusAsync(string referenceId, string token, CancellationToken cancellationToken)
    {
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        using var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/collection/v1_0/requesttopay/{referenceId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Target-Environment", _settings.TargetEnvironment);
        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<MoMoRequestToPayResult>(json, _jsonOptions);
        return result?.Status;
    }

    private async Task<string?> GetTransferStatusAsync(string referenceId, string token, CancellationToken cancellationToken)
    {
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        using var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/disbursement/v1_0/transfer/{referenceId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Target-Environment", _settings.TargetEnvironment);
        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<MoMoTransferResult>(json, _jsonOptions);
        return result?.Status;
    }

    private static PaymentTransactionStatus MapMoMoStatus(string status)
    {
        return status.ToUpperInvariant() switch
        {
            "SUCCESSFUL" => PaymentTransactionStatus.Success,
            "FAILED" => PaymentTransactionStatus.Failed,
            _ => PaymentTransactionStatus.Pending
        };
    }

    private static void ThrowMappedMoMoException(string operation, System.Net.HttpStatusCode statusCode, string? errorBody)
    {
        var code = "";
        var message = "";
        if (!string.IsNullOrEmpty(errorBody))
        {
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(errorBody);
                if (doc.RootElement.TryGetProperty("code", out var c)) code = c.GetString() ?? "";
                if (doc.RootElement.TryGetProperty("message", out var m)) message = m.GetString() ?? "";
            }
            catch { /* use generic message */ }
        }
        if (string.IsNullOrEmpty(message)) message = $"{operation} failed: {statusCode}";
        switch (code.ToUpperInvariant())
        {
            case "PAYER_NOT_FOUND":
            case "PAYEE_NOT_FOUND":
                throw new InvalidOperationException("Invalid phone number or account not active on MoMo.");
            case "PAYER_LIMIT_REACHED":
            case "NOT_ENOUGH_FUNDS":
                throw new InvalidOperationException("Insufficient balance on MoMo account.");
            case "INTERNAL_PROCESSING_ERROR":
                throw new MoMoApiException("MoMo service error. Please try again later.", statusCode, errorBody);
            default:
                if (statusCode == System.Net.HttpStatusCode.RequestTimeout || statusCode == System.Net.HttpStatusCode.GatewayTimeout)
                    throw new MoMoApiException("Request timed out. Check status later.", statusCode, errorBody);
                throw new MoMoApiException(message, statusCode, errorBody);
        }
    }

    private static void NormalizePhone(ref string phone)
    {
        phone = phone.Trim();
        if (phone.StartsWith("0"))
            phone = "256" + phone.TrimStart('0');
        else if (!phone.StartsWith("+") && !phone.StartsWith("256"))
            phone = "256" + phone;
        if (phone.StartsWith("+"))
            phone = phone.TrimStart('+');
    }

    private static PaymentTransactionDto Map(PaymentTransaction t) =>
        new(t.Id, t.PlayerId, t.Amount, t.Currency, t.ExternalReference, t.MoMoTransactionId, t.Type, t.Status, t.CreatedAt, t.UpdatedAt);
}

/// <summary>Thrown when MoMo API returns an error.</summary>
public class MoMoApiException : InvalidOperationException
{
    public System.Net.HttpStatusCode StatusCode { get; }
    public string? ResponseBody { get; }

    public MoMoApiException(string message, System.Net.HttpStatusCode statusCode, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
