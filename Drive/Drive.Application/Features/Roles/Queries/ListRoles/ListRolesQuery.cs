using Drive.Application.Features.Roles.Models;
using MediatR;

namespace Drive.Application.Features.Roles.Queries.ListRoles;

public sealed record ListRolesQuery() : IRequest<IReadOnlyList<RoleResult>>;
