using Drive.Application.Features.Roles.Models;
using MediatR;

namespace Drive.Application.Features.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(string Name) : IRequest<RoleResult?>;
