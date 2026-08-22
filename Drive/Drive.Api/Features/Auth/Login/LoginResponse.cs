namespace Drive.Api.Features.Auth.Login;

public sealed class LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;
}