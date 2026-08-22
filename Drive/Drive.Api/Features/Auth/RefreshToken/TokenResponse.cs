namespace Drive.Api.Features.Auth.RefreshToken;

public sealed class TokenResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;
}