using Bobeta.Application.DTOs.Portal;
using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

public interface IPortalAuthService
{
  Task<PortalUser?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
}
