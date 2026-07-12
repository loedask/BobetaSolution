using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

public sealed class PortalUserService(
    IPortalUserRepository portalUsers,
    PortalPasswordHasher passwordHasher) : IPortalUserService
{
  public async Task<IReadOnlyList<PortalUserListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
  {
    var users = await portalUsers.GetAllAsync(cancellationToken);
    return users.Select(ToListItem).ToList();
  }

  public async Task<PortalUserListItemDto> RegisterAsync(
      RegisterPortalUserRequest request,
      Guid createdById,
      CancellationToken cancellationToken = default)
  {
    var email = request.Email.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(email))
      throw new InvalidOperationException("Email is required.");
    if (string.IsNullOrWhiteSpace(request.FirstName))
      throw new InvalidOperationException("First name is required.");
    if (string.IsNullOrWhiteSpace(request.LastName))
      throw new InvalidOperationException("Last name is required.");
    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
      throw new InvalidOperationException("Password must be at least 8 characters.");

    if (await portalUsers.GetByEmailAsync(email, cancellationToken) is not null)
      throw new InvalidOperationException("A portal user with this email already exists.");

    if (request.Role == PortalUserRole.PlatformOwner && await portalUsers.GetByIdAsync(createdById, cancellationToken) is not { Role: PortalUserRole.PlatformOwner })
      throw new UnauthorizedAccessException("Only platform owners can create platform owner accounts.");

    if (request.Role == PortalUserRole.LicensePartner)
      throw new InvalidOperationException("License partners must be registered via the license partner flow.");

    if (request.Role == PortalUserRole.Influencer)
      throw new InvalidOperationException("Influencers must be registered via the influencer flow.");

    var user = new PortalUser
    {
      Id = Guid.NewGuid(),
      Email = email,
      FirstName = request.FirstName.Trim(),
      LastName = request.LastName.Trim(),
      Role = request.Role,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      CreatedById = createdById
    };
    user.PasswordHash = passwordHasher.Hash(user, request.Password);

    await portalUsers.AddAsync(user, cancellationToken);
    return ToListItem(user);
  }

  private static PortalUserListItemDto ToListItem(PortalUser user) => new()
  {
    Id = user.Id,
    Email = user.Email,
    FirstName = user.FirstName,
    LastName = user.LastName,
    Role = user.Role,
    IsActive = user.IsActive,
    CreatedAt = user.CreatedAt
  };
}
