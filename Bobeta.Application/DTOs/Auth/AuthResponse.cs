namespace Bobeta.Application.DTOs.Auth;

/// <summary>Response after successful registration or login: JWT and basic profile.</summary>
public record AuthResponse(string Token, Guid PlayerId, string PlayerName, bool IsNewUser);
