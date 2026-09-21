using Drive.Application.Common.Interfaces;
using MediatR;

namespace Drive.Application.Features.Roles.Commands.RenameRole;

public sealed class RenameRoleCommandHandler
    : IRequestHandler<RenameRoleCommand, bool>
{
    private readonly IRoleService _roleService;

    public RenameRoleCommandHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<bool> Handle(
        RenameRoleCommand request,
        CancellationToken cancellationToken)
    {
        return await _roleService.RenameRoleAsync(
            request.RoleId,
            request.NewName,
            cancellationToken);
    }
}
