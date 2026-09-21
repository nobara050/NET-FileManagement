using MediatR;

namespace Drive.Application.Features.Roles.Commands.RenameRole;

public sealed record RenameRoleCommand(
    Guid RoleId,
    string NewName) : IRequest<bool>;
