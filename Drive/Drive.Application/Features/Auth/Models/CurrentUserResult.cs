namespace Drive.Application.Features.Auth.Models;

public sealed class CurrentUserResult
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
    public IList<string> Roles { get; init; } = new List<string>();
    public bool IsAdmin => Roles.Contains("Admin");
}

