using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Auth.Models;
using Microsoft.AspNetCore.Identity;

namespace Drive.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Guid?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return null;
        }

        var passwordValid = await _userManager.CheckPasswordAsync(
            user,
            password);

        if (!passwordValid)
        {
            return null;
        }

        return user.Id;
    }

    public async Task<IdentityRegistrationResult> RegisterAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return new IdentityRegistrationResult
            {
                Succeeded = false,
                Errors = ["Email is already registered."]
            };
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName
        };

        var result = await _userManager.CreateAsync(
            user,
            password);

        if (!result.Succeeded)
        {
            return new IdentityRegistrationResult
            {
                Succeeded = false,
                Errors = result.Errors
                    .Select(x => x.Description)
                    .ToArray()
            };
        }

        return new IdentityRegistrationResult
        {
            Succeeded = true,
            UserId = user.Id
        };
    }
}