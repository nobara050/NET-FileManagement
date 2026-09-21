using Drive.Application.Common.Interfaces;
using MediatR;

namespace Drive.Application.Features.Roles.Commands.RemoveRoleClaim;

public sealed class RemoveRoleClaimCommandHandler
    : IRequestHandler<RemoveRoleClaimCommand, bool>
{
    private readonly IRoleService _roleService;

    public RemoveRoleClaimCommandHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<bool> Handle(
        RemoveRoleClaimCommand request,
        CancellationToken cancellationToken)
    {
        return await _roleService.RemoveClaimAsync(
            request.RoleId,
            request.ClaimValue,
            cancellationToken);
    }
}
