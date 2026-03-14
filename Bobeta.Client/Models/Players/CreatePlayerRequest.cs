namespace Bobeta.Client.Models.Players;

/// <summary>Request to register a new player. Placeholder for API request.</summary>
public class CreatePlayerRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
}
