using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Roles.Models;
using MediatR;

namespace Drive.Application.Features.Roles.Queries.ListRoles;

public sealed class ListRolesQueryHandler
    : IRequestHandler<ListRolesQuery, IReadOnlyList<RoleResult>>
{
    private readonly IRoleService _roleService;

    public ListRolesQueryHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<IReadOnlyList<RoleResult>> Handle(
        ListRolesQuery request,
        CancellationToken cancellationToken)
    {
        return await _roleService.ListRolesAsync(cancellationToken);
    }
}
