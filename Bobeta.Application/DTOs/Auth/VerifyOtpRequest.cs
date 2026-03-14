namespace Bobeta.Application.DTOs.Auth;

/// <summary>Request to verify an OTP code for a phone number.</summary>
public record VerifyOtpRequest(string PhoneNumber, string Code);
