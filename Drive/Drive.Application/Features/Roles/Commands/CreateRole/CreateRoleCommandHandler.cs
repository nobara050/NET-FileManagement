using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Roles.Models;
using MediatR;

namespace Drive.Application.Features.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandHandler
    : IRequestHandler<CreateRoleCommand, RoleResult?>
{
    private readonly IRoleService _roleService;

    public CreateRoleCommandHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<RoleResult?> Handle(
        CreateRoleCommand request,
        CancellationToken cancellationToken)
    {
        return await _roleService.CreateRoleAsync(
            request.Name,
            cancellationToken);
    }
}
