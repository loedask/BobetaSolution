using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

/// <summary>Persistence contract for player entities (lookup by id or phone, add, update).</summary>
public interface IPlayerRepository
{
    /// <summary>Gets a player by unique identifier, or null if not found.</summary>
    Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets a player by Mobile Money phone number, or null if not found.</summary>
    Task<Player?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>Adds a new player and returns the same instance (with Id set).</summary>
    Task<Player> AddAsync(Player player, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing player.</summary>
    Task UpdateAsync(Player player, CancellationToken cancellationToken = default);
}
