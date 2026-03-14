using Bobeta.Domain.Enums;

namespace Bobeta.Domain.Entities;

public class Player
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public DateTime CreatedAt { get; set; }
    public bool IsVerified { get; set; }
    public PlayerStatus Status { get; set; }
}
