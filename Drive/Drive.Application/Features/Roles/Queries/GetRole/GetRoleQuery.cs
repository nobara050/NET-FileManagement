using Drive.Application.Features.Roles.Models;
using MediatR;

namespace Drive.Application.Features.Roles.Queries.GetRole;

public sealed record GetRoleQuery(Guid RoleId) : IRequest<RoleResult?>;
