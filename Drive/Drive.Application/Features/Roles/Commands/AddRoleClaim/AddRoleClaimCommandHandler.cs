using Drive.Application.Common.Interfaces;
using MediatR;

namespace Drive.Application.Features.Roles.Commands.AddRoleClaim;

public sealed class AddRoleClaimCommandHandler
    : IRequestHandler<AddRoleClaimCommand, bool>
{
    private readonly IRoleService _roleService;

    public AddRoleClaimCommandHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<bool> Handle(
        AddRoleClaimCommand request,
        CancellationToken cancellationToken)
    {
        return await _roleService.AddClaimAsync(
            request.RoleId,
            request.ClaimValue,
            cancellationToken);
    }
}
