using Drive.Application.Common.Interfaces;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.UpdateAssignment;

public class UpdateAssignmentCommandHandler : IRequestHandler<UpdateAssignmentCommand, bool>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly IDriveItemRoleAssignmentRepository _assignmentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionMaterializer _permissionMaterializer;
    private readonly IRoleService _roleService;

    public UpdateAssignmentCommandHandler(
        IDriveItemRepository driveItemRepository,
        IDriveItemRoleAssignmentRepository assignmentRepository,
        ICurrentUserService currentUserService,
        IPermissionMaterializer permissionMaterializer,
        IRoleService roleService)
    {
        _driveItemRepository = driveItemRepository;
        _assignmentRepository = assignmentRepository;
        _currentUserService = currentUserService;
        _permissionMaterializer = permissionMaterializer;
        _roleService = roleService;
    }

    public async Task<bool> Handle(UpdateAssignmentCommand request, CancellationToken cancellationToken)
    {
        var callerId = _currentUserService.UserId;

        if (callerId is null)
        {
            return false;
        }

        // Verify item exists and caller is owner.
        var item = await _driveItemRepository.GetByIdAsync(
            request.DriveItemId,
            cancellationToken);

        if (item is null || item.IsDeleted || item.OwnerId != callerId.Value)
        {
            return false;
        }

        // Verify the new role exists.
        var role = await _roleService.GetRoleByIdAsync(
            request.NewRoleId,
            cancellationToken);

        if (role is null)
        {
            return false;
        }

        // Find the existing direct assignment.
        var assignment = await _assignmentRepository.GetDirectAssignmentAsync(
            request.DriveItemId,
            request.TargetUserId,
            cancellationToken);

        if (assignment is null)
        {
            return false;
        }

        assignment.RoleId = request.NewRoleId;

        _assignmentRepository.Update(assignment);

        // Remove old inherited assignments and re-materialise with new role.
        await _permissionMaterializer.RemoveInheritedAsync(
            request.DriveItemId,
            request.TargetUserId,
            cancellationToken);

        await _permissionMaterializer.MaterializeAsync(
            request.DriveItemId,
            request.TargetUserId,
            request.NewRoleId,
            callerId.Value,
            cancellationToken);

        // Commit updated assignment and all descendant updates atomically.
        await _assignmentRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
