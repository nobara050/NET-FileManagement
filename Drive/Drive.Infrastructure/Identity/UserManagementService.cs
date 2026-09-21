using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Users.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Identity;

public sealed class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementService(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<UserResult>> ListUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new UserResult
            {
                UserId = u.Id,
                Email = u.Email!,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl
            })
            .ToListAsync(cancellationToken);

        return users;
    }

    public async Task<UserResult?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserResult
            {
                UserId = u.Id,
                Email = u.Email!,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }

    public async Task<UserResult?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLower();

        var user = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.Email!.ToLower() == normalizedEmail)
            .Select(u => new UserResult
            {
                UserId = u.Id,
                Email = u.Email!,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }

    public async Task<bool> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return false;
        }

        var result = await _userManager.DeleteAsync(user);

        return result.Succeeded;
    }

    public async Task<IReadOnlyList<UserResult>> SearchUsersAsync(
        string query,
        int limit,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim().ToLower();

        var users = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.Id != currentUserId &&
                (u.Email!.ToLower().Contains(normalizedQuery) ||
                 u.DisplayName.ToLower().Contains(normalizedQuery)))
            .OrderBy(u => u.DisplayName)
            .Take(limit)
            .Select(u => new UserResult
            {
                UserId = u.Id,
                Email = u.Email!,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl
            })
            .ToListAsync(cancellationToken);

        return users;
    }

    public async Task<bool> UpdateAvatarAsync(
        Guid userId,
        string? avatarUrl,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        user.AvatarUrl = avatarUrl;
        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded;
    }
}
