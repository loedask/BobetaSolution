using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

public interface IPortalUserRepository
{
  Task<PortalUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<PortalUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<PortalUser>> GetAllAsync(CancellationToken cancellationToken = default);
  Task<PortalUser> AddAsync(PortalUser user, CancellationToken cancellationToken = default);
  Task UpdateAsync(PortalUser user, CancellationToken cancellationToken = default);
}
