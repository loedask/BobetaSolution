namespace Bobeta.Client.Models.Players;

/// <summary>Player profile view model. Placeholder for API response.</summary>
public class PlayerViewModel
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public DateTime CreatedAt { get; set; }
    public bool IsVerified { get; set; }
}
