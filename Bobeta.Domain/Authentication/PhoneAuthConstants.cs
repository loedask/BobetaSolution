namespace Bobeta.Domain.Authentication;

/// <summary>Phone OTP length used when generating codes on the server and constraining client input.</summary>
public static class PhoneAuthConstants
{
    public const int OtpDigitLength = 6;
}
