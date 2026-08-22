namespace Drive.Api.Features.Auth.Logout;

public sealed class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}