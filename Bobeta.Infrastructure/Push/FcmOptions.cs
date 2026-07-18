namespace Bobeta.Infrastructure.Push;

/// <summary>Firebase Cloud Messaging (HTTP v1) settings. When disabled or unconfigured, push is a no-op.</summary>
public class FcmOptions
{
    public const string SectionName = "Fcm";

    /// <summary>When false, push notifications are not sent.</summary>
    public bool Enabled { get; set; }

    /// <summary>Firebase / Google Cloud project id.</summary>
    public string? ProjectId { get; set; }

    /// <summary>Optional path to a service account JSON file.</summary>
    public string? CredentialsPath { get; set; }

    /// <summary>Inline service account JSON (prefer user-secrets / Key Vault over committing secrets).</summary>
    public string? CredentialsJson { get; set; }
}
