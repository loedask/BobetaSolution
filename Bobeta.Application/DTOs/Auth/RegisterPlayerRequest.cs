namespace Bobeta.Application.DTOs.Auth;

/// <summary>Request to register a new player after OTP verification.</summary>
public record RegisterPlayerRequest(string PhoneNumber, string PlayerName);
