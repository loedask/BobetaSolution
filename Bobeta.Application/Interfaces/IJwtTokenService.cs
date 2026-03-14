namespace Bobeta.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(Guid playerId, string playerName);
}
