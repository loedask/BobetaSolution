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
    if (string.IsNullOrWhiteSpace(request.DisplayName))
      throw new InvalidOperationException("Display name is required.");
    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
      throw new InvalidOperationException("Password must be at least 8 characters.");

    if (await portalUsers.GetByEmailAsync(email, cancellationToken) is not null)
      throw new InvalidOperationException("A portal user with this email already exists.");

    if (request.Role == PortalUserRole.Admin && await portalUsers.GetByIdAsync(createdById, cancellationToken) is not { Role: PortalUserRole.Admin })
      throw new UnauthorizedAccessException("Only admins can create admin accounts.");

    var user = new PortalUser
    {
      Id = Guid.NewGuid(),
      Email = email,
      DisplayName = request.DisplayName.Trim(),
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
    DisplayName = user.DisplayName,
    Role = user.Role,
    IsActive = user.IsActive,
    CreatedAt = user.CreatedAt
  };
}
