namespace Bobeta.Application.DTOs.Auth;

public record AuthResponse(string Token, Guid PlayerId, string PlayerName, bool IsNewUser);
