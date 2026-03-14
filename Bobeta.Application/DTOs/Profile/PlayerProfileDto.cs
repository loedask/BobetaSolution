namespace Bobeta.Application.DTOs.Profile;

public record PlayerProfileDto(
    Guid Id,
    string PhoneNumber,
    string PlayerName,
    string Language,
    DateTime CreatedAt,
    bool IsVerified);
