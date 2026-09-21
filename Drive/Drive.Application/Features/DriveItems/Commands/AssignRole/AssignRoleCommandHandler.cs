using Drive.Application.Common.Interfaces;
using Drive.Domain.Entities;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.AssignRole;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, bool>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly IDriveItemRoleAssignmentRepository _assignmentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionMaterializer _permissionMaterializer;
    private readonly IRoleService _roleService;
    private readonly IUserManagementService _userManagementService;

    public AssignRoleCommandHandler(
        IDriveItemRepository driveItemRepository,
        IDriveItemRoleAssignmentRepository assignmentRepository,
        ICurrentUserService currentUserService,
        IPermissionMaterializer permissionMaterializer,
        IRoleService roleService,
        IUserManagementService userManagementService)
    {
        _driveItemRepository = driveItemRepository;
        _assignmentRepository = assignmentRepository;
        _currentUserService = currentUserService;
        _permissionMaterializer = permissionMaterializer;
        _roleService = roleService;
        _userManagementService = userManagementService;
    }

    public async Task<bool> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var callerId = _currentUserService.UserId;

        if (callerId is null)
        {
            return false;
        }

        var item = await _driveItemRepository.GetByIdAsync(
            request.DriveItemId,
            cancellationToken);

        if (item is null || item.IsDeleted)
        {
            return false;
        }

        // Only the item owner can assign roles.
        if (item.OwnerId != callerId.Value)
        {
            return false;
        }

        var targetUserId = request.TargetUserId;
        if (!targetUserId.HasValue || targetUserId.Value == Guid.Empty)
        {
            if (string.IsNullOrWhiteSpace(request.TargetEmail))
            {
                return false;
            }

            var targetUser = await _userManagementService.GetUserByEmailAsync(request.TargetEmail, cancellationToken);
            if (targetUser is null)
            {
                return false;
            }

            targetUserId = targetUser.UserId;
        }

        // Owner cannot assign a role to themselves — they already have implicit full access.
        if (targetUserId.Value == callerId.Value)
        {
            return false;
        }

        // Verify the role exists.
        var role = await _roleService.GetRoleByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
        {
            return false;
        }

        // Check if any assignment already exists for this user on this item.
        var existing = await _assignmentRepository.GetAssignmentAsync(
            request.DriveItemId,
            targetUserId.Value,
            cancellationToken);

        // Direct assignments block a new assignment (must use UpdateAssignment instead).
        if (existing is not null && existing.IsDirect)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;

        if (existing is not null)
        {
            // Promote inherited assignment to a direct assignment on this item.
            existing.RoleId = request.RoleId;
            existing.SourceItemId = null;
            existing.IsDirect = true;
            existing.CreatedBy = callerId.Value;
            existing.CreatedAt = now;

            _assignmentRepository.Update(existing);
        }
        else
        {
            await _assignmentRepository.AddAsync(
                new DriveItemRoleAssignment
                {
                    Id = Guid.NewGuid(),
                    DriveItemId = request.DriveItemId,
                    UserId = targetUserId.Value,
                    RoleId = request.RoleId,
                    SourceItemId = null,
                    IsDirect = true,
                    CreatedBy = callerId.Value,
                    CreatedAt = now
                },
                cancellationToken);
        }

        // Propagate inherited permissions to all descendants.
        await _permissionMaterializer.MaterializeAsync(
            request.DriveItemId,
            targetUserId.Value,
            request.RoleId,
            callerId.Value,
            cancellationToken);

        // Commit explicit assignment and all materialized descendant assignments atomically.
        await _assignmentRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
