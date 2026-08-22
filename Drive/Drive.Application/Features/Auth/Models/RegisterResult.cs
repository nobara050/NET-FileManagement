namespace Drive.Application.Features.Auth.Models;

public sealed class RegisterResult
{
    public bool Succeeded { get; init; }

    public Guid UserId { get; init; }

    public bool IsEmailConflict { get; init; }

    public IReadOnlyCollection<string> Errors { get; init; } =
        Array.Empty<string>();
}
