using Drive.Application.Features.Auth.Models;

namespace Drive.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Guid?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<IdentityRegistrationResult> RegisterAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken = default);
}