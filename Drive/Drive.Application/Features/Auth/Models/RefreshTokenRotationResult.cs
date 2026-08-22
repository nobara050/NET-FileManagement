namespace Drive.Application.Features.Auth.Models;

public sealed class RefreshTokenRotationResult
{
    public Guid UserId { get; init; }

    public string RefreshToken { get; init; } = string.Empty;
}