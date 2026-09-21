using Drive.Application.Common.Interfaces;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.RemoveAssignment;

public sealed class RemoveAssignmentCommandHandler : IRequestHandler<RemoveAssignmentCommand, bool>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly IDriveItemRoleAssignmentRepository _assignmentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionMaterializer _permissionMaterializer;

    public RemoveAssignmentCommandHandler(
        IDriveItemRepository driveItemRepository,
        IDriveItemRoleAssignmentRepository assignmentRepository,
        ICurrentUserService currentUserService,
        IPermissionMaterializer permissionMaterializer)
    {
        _driveItemRepository = driveItemRepository;
        _assignmentRepository = assignmentRepository;
        _currentUserService = currentUserService;
        _permissionMaterializer = permissionMaterializer;
    }

    public async Task<bool> Handle(RemoveAssignmentCommand request, CancellationToken cancellationToken)
    {
        var callerId = _currentUserService.UserId;

        if (callerId is null)
        {
            return false;
        }

        // Verify item exists and caller is the owner.
        var item = await _driveItemRepository.GetByIdAsync(
            request.DriveItemId,
            cancellationToken);

        if (item is null || item.IsDeleted || item.OwnerId != callerId.Value)
        {
            return false;
        }

        // Check if an assignment exists for this user on this item (either direct or inherited).
        var assignment = await _assignmentRepository.GetAssignmentAsync(
            request.DriveItemId,
            request.TargetUserId,
            cancellationToken);

        if (assignment is null)
        {
            return false;
        }

        // Determine the source item ID from which inherited permissions in this scope originated.
        var sourceItemId = assignment.IsDirect
            ? request.DriveItemId
            : (assignment.SourceItemId ?? request.DriveItemId);

        // Remove inherited assignments in the descendant subtree for this source,
        // preserving any direct assignments on descendants.
        await _permissionMaterializer.RemoveInheritedFromScopeAsync(
            request.DriveItemId,
            request.TargetUserId,
            sourceItemId,
            cancellationToken);

        // Remove the assignment on the current item itself.
        _assignmentRepository.Remove(assignment);

        await _assignmentRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
