using Bobeta.Application.DTOs.Portal;

namespace Bobeta.Application.Interfaces;

public interface IPortalUserService
{
  Task<IReadOnlyList<PortalUserListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
  Task<PortalUserListItemDto> RegisterAsync(RegisterPortalUserRequest request, Guid createdById, CancellationToken cancellationToken = default);
}
