namespace Bobeta.Mobile.State;

/// <summary>Global application state. Persisted via <see cref="Services.PreferencesStorageService"/>.</summary>
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

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);
}
