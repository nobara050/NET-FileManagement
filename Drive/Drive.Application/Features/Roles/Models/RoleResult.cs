namespace Drive.Application.Features.Roles.Models;

public sealed class RoleResult
{
    public Guid RoleId { get; init; }

    public string Name { get; init; } = null!;

    public IReadOnlyList<string> Claims { get; init; } = [];
}
