using System.Text.Json.Serialization;

namespace Bobeta.Infrastructure.MoMo;

/// <summary>Response from POST /token/ (CreateAccessToken).</summary>
internal sealed class MoMoTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

/// <summary>Party (payer or payee) for MoMo requests.</summary>
internal sealed class MoMoParty
{
    [JsonPropertyName("partyIdType")]
    public string PartyIdType { get; set; } = "MSISDN";

    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;
}

/// <summary>Request body for Collection request-to-pay.</summary>
internal sealed class MoMoRequestToPayRequest
{
    [JsonPropertyName("amount")]
    public string Amount { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "UGX";

    [JsonPropertyName("externalId")]
    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("payer")]
    public MoMoParty Payer { get; set; } = null!;

    [JsonPropertyName("payerMessage")]
    public string? PayerMessage { get; set; }

    [JsonPropertyName("payeeNote")]
    public string? PayeeNote { get; set; }
}

/// <summary>Request body for Disbursement transfer.</summary>
internal sealed class MoMoTransferRequest
{
    [JsonPropertyName("amount")]
    public string Amount { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "UGX";

    [JsonPropertyName("externalId")]
    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("payee")]
    public MoMoParty Payee { get; set; } = null!;

    [JsonPropertyName("payerMessage")]
    public string? PayerMessage { get; set; }

    [JsonPropertyName("payeeNote")]
    public string? PayeeNote { get; set; }
}

/// <summary>Request-to-pay status (GET /collection/v1_0/requesttopay/{referenceId}).</summary>
internal sealed class MoMoRequestToPayResult
{
    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("financialTransactionId")]
    public string? FinancialTransactionId { get; set; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; set; }

    [JsonPropertyName("payer")]
    public MoMoParty? Payer { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("reason")]
    public MoMoErrorReason? Reason { get; set; }
}

/// <summary>Transfer status (GET /disbursement/v1_0/transfer/{referenceId}).</summary>
internal sealed class MoMoTransferResult
{
    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("financialTransactionId")]
    public string? FinancialTransactionId { get; set; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; set; }

    [JsonPropertyName("payee")]
    public MoMoParty? Payee { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("reason")]
    public MoMoErrorReason? Reason { get; set; }
}

/// <summary>Error reason from MoMo API.</summary>
internal sealed class MoMoErrorReason
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>Callback payload sent by MoMo (referenceId in header or body; status in body).</summary>
internal sealed class MoMoCallbackPayload
{
    [JsonPropertyName("referenceId")]
    public string? ReferenceId { get; set; }

    [JsonPropertyName("financialTransactionId")]
    public string? FinancialTransactionId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("reason")]
    public MoMoErrorReason? Reason { get; set; }
}
