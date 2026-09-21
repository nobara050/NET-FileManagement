using MediatR;

namespace Drive.Application.Features.Roles.Commands.DeleteRole;

public sealed record DeleteRoleCommand(Guid RoleId) : IRequest<bool>;
