namespace Drive.Application.Features.Auth.Models;

public sealed record AuthTokenResult(
    string AccessToken,
    string RefreshToken);
