using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

/// <summary>EF repository for <see cref="PlayerDeviceToken"/>.</summary>
public class PlayerDeviceTokenRepository(BobetaDbContext db) : IPlayerDeviceTokenRepository
{
    public async Task<IReadOnlyList<PlayerDeviceToken>> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        await db.PlayerDeviceTokens
            .Where(t => t.PlayerId == playerId)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);

    public Task<PlayerDeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        db.PlayerDeviceTokens.FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

    public async Task<PlayerDeviceToken> AddAsync(PlayerDeviceToken token, CancellationToken cancellationToken = default)
    {
        db.PlayerDeviceTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task UpdateAsync(PlayerDeviceToken token, CancellationToken cancellationToken = default)
    {
        db.PlayerDeviceTokens.Update(token);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        await db.PlayerDeviceTokens.Where(t => t.Token == token).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteByTokensAsync(IReadOnlyList<string> tokens, CancellationToken cancellationToken = default)
    {
        if (tokens.Count == 0)
            return;
        await db.PlayerDeviceTokens.Where(t => tokens.Contains(t.Token)).ExecuteDeleteAsync(cancellationToken);
    }
}
