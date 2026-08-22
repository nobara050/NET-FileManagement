using Drive.Application.Features.Auth.Models;

namespace Drive.Application.Common.Interfaces;

public interface IRefreshTokenService
{
    Task<string> CreateAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Guid?> ValidateAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<RefreshTokenRotationResult?> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}