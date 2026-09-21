using Drive.Application.Features.Roles.Models;

namespace Drive.Application.Common.Interfaces;

public interface IRoleService
{
    Task<IReadOnlyList<RoleResult>> ListRolesAsync(
        CancellationToken cancellationToken = default);

    Task<RoleResult?> GetRoleByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task<RoleResult?> CreateRoleAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<bool> RenameRoleAsync(
        Guid roleId,
        string newName,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task<bool> AddClaimAsync(
        Guid roleId,
        string claimValue,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveClaimAsync(
        Guid roleId,
        string claimValue,
        CancellationToken cancellationToken = default);
}
