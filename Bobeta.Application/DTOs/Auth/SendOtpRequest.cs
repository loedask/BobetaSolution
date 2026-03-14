namespace Bobeta.Application.DTOs.Auth;

/// <summary>Request to send an OTP to a phone number for login or registration.</summary>
/// <param name="PhoneNumber">Mobile Money phone number.</param>
public record SendOtpRequest(string PhoneNumber);
