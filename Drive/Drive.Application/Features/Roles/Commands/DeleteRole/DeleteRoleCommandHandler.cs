using Drive.Application.Common.Interfaces;
using MediatR;

namespace Drive.Application.Features.Roles.Commands.DeleteRole;

public sealed class DeleteRoleCommandHandler
    : IRequestHandler<DeleteRoleCommand, bool>
{
    private readonly IRoleService _roleService;
    private readonly IDriveItemRoleAssignmentRepository _assignmentRepository;

    public DeleteRoleCommandHandler(
        IRoleService roleService,
        IDriveItemRoleAssignmentRepository assignmentRepository)
    {
        _roleService = roleService;
        _assignmentRepository = assignmentRepository;
    }

    public async Task<bool> Handle(
        DeleteRoleCommand request,
        CancellationToken cancellationToken)
    {
        // Remove all drive-item assignments that reference this role
        // before deleting it, to avoid orphaned rows in the database.
        await _assignmentRepository.RemoveByRoleIdAsync(
            request.RoleId,
            cancellationToken);

        await _assignmentRepository.SaveChangesAsync(cancellationToken);

        return await _roleService.DeleteRoleAsync(
            request.RoleId,
            cancellationToken);
    }
}
