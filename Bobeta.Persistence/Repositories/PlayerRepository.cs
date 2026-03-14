using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly BobetaDbContext _db;

    public PlayerRepository(BobetaDbContext db) => _db = db;

    public async Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Players.FindAsync([id], cancellationToken);

    public async Task<Player?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
        await _db.Players.FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber, cancellationToken);

    public async Task<Player> AddAsync(Player player, CancellationToken cancellationToken = default)
    {
        _db.Players.Add(player);
        await _db.SaveChangesAsync(cancellationToken);
        return player;
    }

    public async Task UpdateAsync(Player player, CancellationToken cancellationToken = default)
    {
        _db.Players.Update(player);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
