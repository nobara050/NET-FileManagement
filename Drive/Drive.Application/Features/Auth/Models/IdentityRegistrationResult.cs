namespace Drive.Application.Features.Auth.Models;

public sealed class IdentityRegistrationResult
{
    public bool Succeeded { get; init; }

    public Guid UserId { get; init; }

    public IReadOnlyCollection<string> Errors { get; init; } =
        Array.Empty<string>();
}