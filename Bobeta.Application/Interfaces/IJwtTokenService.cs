namespace Bobeta.Application.Interfaces;

/// <summary>Generates JWT bearer tokens for authenticated players (used after OTP verify or register).</summary>
public interface IJwtTokenService
{
    /// <summary>Builds a JWT containing player id and name, with expiry from configuration.</summary>
    string GenerateToken(Guid playerId, string playerName);
}
