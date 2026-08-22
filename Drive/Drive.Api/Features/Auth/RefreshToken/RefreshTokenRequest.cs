namespace Drive.Api.Features.Auth.RefreshToken;

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}