namespace Drive.Application.Features.Users.Models;

public sealed class UserResult
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = null!;

    public string DisplayName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
}
