using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

public class PortalUserRepository(BobetaDbContext db) : IPortalUserRepository
{
  public async Task<PortalUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
    await db.PortalUsers.FindAsync([id], cancellationToken);

  public async Task<PortalUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
  {
    var normalized = email.Trim().ToLowerInvariant();
    return await db.PortalUsers.FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
  }

  public async Task<IReadOnlyList<PortalUser>> GetAllAsync(CancellationToken cancellationToken = default) =>
    await db.PortalUsers
      .AsNoTracking()
      .OrderByDescending(u => u.CreatedAt)
      .ToListAsync(cancellationToken);

  public async Task<PortalUser> AddAsync(PortalUser user, CancellationToken cancellationToken = default)
  {
    db.PortalUsers.Add(user);
    await db.SaveChangesAsync(cancellationToken);
    return user;
  }

  public async Task UpdateAsync(PortalUser user, CancellationToken cancellationToken = default)
  {
    db.PortalUsers.Update(user);
    await db.SaveChangesAsync(cancellationToken);
  }
}
