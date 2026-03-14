namespace Bobeta.Application.DTOs.Auth;

public record VerifyOtpRequest(string PhoneNumber, string Code);
