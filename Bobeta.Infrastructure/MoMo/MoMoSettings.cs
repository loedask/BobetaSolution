namespace Bobeta.Infrastructure.MoMo;

/// <summary>MTN MoMo API configuration (sandbox or production).</summary>
public class MoMoSettings
{
    public const string SectionName = "MoMo";

    /// <summary>Base URL (e.g. https://sandbox.momodeveloper.mtn.com for sandbox).</summary>
    public string BaseUrl { get; set; } = "https://sandbox.momodeveloper.mtn.com";

    /// <summary>Subscription key from MoMo developer portal.</summary>
    public string SubscriptionKey { get; set; } = string.Empty;

    /// <summary>API user (created under the subscription).</summary>
    public string ApiUser { get; set; } = string.Empty;

    /// <summary>API key for the API user.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Primary key for Collection (request-to-pay) product.</summary>
    public string CollectionPrimaryKey { get; set; } = string.Empty;

    /// <summary>Primary key for Disbursement product.</summary>
    public string DisbursementPrimaryKey { get; set; } = string.Empty;

    /// <summary>Callback URL for payment notifications (e.g. https://yourapi.com/api/payments/momo/callback).</summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>Target environment (e.g. mtncongo for Republic of the Congo). Validated on callback via X-Target-Environment header.</summary>
    public string TargetEnvironment { get; set; } = "mtncongo";

    /// <summary>Subscription key to validate on callback (Ocp-Apim-Subscription-Key header). Set to the key MTN uses when calling your callback (e.g. same as Collection primary key).</summary>
    public string CallbackSubscriptionKey { get; set; } = string.Empty;

    /// <summary>Default currency for requests (e.g. XAF for Republic of the Congo).</summary>
    public string Currency { get; set; } = "XAF";

    /// <summary>When true, use sandbox base URL and target environment.</summary>
    public bool UseSandbox { get; set; } = true;
}
