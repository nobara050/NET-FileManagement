using Drive.Application.Features.Users.Models;

namespace Drive.Application.Common.Interfaces;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserResult>> ListUsersAsync(
        CancellationToken cancellationToken = default);

    Task<UserResult?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserResult?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResult>> SearchUsersAsync(
        string query,
        int limit,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAvatarAsync(
        Guid userId,
        string? avatarUrl,
        CancellationToken cancellationToken = default);
}
