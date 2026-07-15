namespace Bobeta.Web.Shared.State;

/// <summary>Global application state. Persist key properties in localStorage via <see cref="Services.LocalStorageService"/>.</summary>
public class AppState
{
    public string? CurrentPlayerName { get; set; }
    public Guid? CurrentPlayerId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AccessToken { get; set; }

    public decimal WalletBalance { get; set; }
    public decimal LockedBalance { get; set; }

    public Guid? ActiveGameSessionId { get; set; }

    public string SelectedLanguage { get; set; } = "en";

    /// <summary>Invite code captured from /invite/{code} before login; kept across logout.</summary>
    public string? PendingInviteCode { get; set; }

    /// <summary>User dismissed the home invite tip; kept across logout.</summary>
    public bool InvitePromptDismissed { get; set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);
}
