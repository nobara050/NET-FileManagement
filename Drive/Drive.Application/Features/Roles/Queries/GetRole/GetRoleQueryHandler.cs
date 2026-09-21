using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Roles.Models;
using MediatR;

namespace Drive.Application.Features.Roles.Queries.GetRole;

public sealed class GetRoleQueryHandler
    : IRequestHandler<GetRoleQuery, RoleResult?>
{
    private readonly IRoleService _roleService;

    public GetRoleQueryHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<RoleResult?> Handle(
        GetRoleQuery request,
        CancellationToken cancellationToken)
    {
        return await _roleService.GetRoleByIdAsync(
            request.RoleId,
            cancellationToken);
    }
}
