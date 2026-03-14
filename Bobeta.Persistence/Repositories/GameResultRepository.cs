using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Persistence.Context;

namespace Bobeta.Persistence.Repositories;

public class GameResultRepository : IGameResultRepository
{
    private readonly BobetaDbContext _db;

    public GameResultRepository(BobetaDbContext db) => _db = db;

    public async Task<GameResult> AddAsync(GameResult result, CancellationToken cancellationToken = default)
    {
        _db.GameResults.Add(result);
        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }
}
