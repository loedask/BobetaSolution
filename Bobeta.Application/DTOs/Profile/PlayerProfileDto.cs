namespace Bobeta.Application.DTOs.Profile;

/// <summary>Player profile data returned to the client.</summary>
public record PlayerProfileDto(
    Guid Id,
    string PhoneNumber,
    string PlayerName,
    string Language,
    DateTime CreatedAt,
    bool IsVerified);
