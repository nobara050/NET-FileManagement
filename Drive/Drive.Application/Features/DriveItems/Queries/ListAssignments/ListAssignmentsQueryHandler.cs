using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Models;
using MediatR;

namespace Drive.Application.Features.DriveItems.Queries.ListAssignments;

public sealed class ListAssignmentsQueryHandler
    : IRequestHandler<ListAssignmentsQuery, IReadOnlyList<AssignmentResult>?>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly IDriveItemRoleAssignmentRepository _assignmentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRoleService _roleService;
    private readonly IUserManagementService _userManagementService;

    public ListAssignmentsQueryHandler(
        IDriveItemRepository driveItemRepository,
        IDriveItemRoleAssignmentRepository assignmentRepository,
        ICurrentUserService currentUserService,
        IRoleService roleService,
        IUserManagementService userManagementService)
    {
        _driveItemRepository = driveItemRepository;
        _assignmentRepository = assignmentRepository;
        _currentUserService = currentUserService;
        _roleService = roleService;
        _userManagementService = userManagementService;
    }

    public async Task<IReadOnlyList<AssignmentResult>?> Handle(
        ListAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            return null;
        }

        var item = await _driveItemRepository.GetByIdAsync(
            request.DriveItemId,
            cancellationToken);

        if (item is null || item.IsDeleted)
        {
            return null;
        }

        // Only the item owner may view assignments.
        if (item.OwnerId != userId.Value)
        {
            return null;
        }

        var assignments = await _assignmentRepository.ListByDriveItemIdAsync(
            request.DriveItemId,
            cancellationToken);

        // Build a lookup of all roles to avoid N+1 queries.
        var allRoles = await _roleService.ListRolesAsync(cancellationToken);
        var roleMap = allRoles.ToDictionary(r => r.RoleId, r => r.Name);

        // Fetch user information to display email / displayName
        var allUsers = await _userManagementService.ListUsersAsync(cancellationToken);
        var userMap = allUsers.ToDictionary(u => u.UserId, u => u);

        var results = assignments.Select(a =>
        {
            userMap.TryGetValue(a.UserId, out var userInfo);
            return new AssignmentResult
            {
                UserId = a.UserId,
                UserEmail = userInfo?.Email,
                DisplayName = userInfo?.DisplayName,
                RoleId = a.RoleId,
                RoleName = roleMap.TryGetValue(a.RoleId, out var name)
                    ? name
                    : a.RoleId.ToString(),
                IsDirect = a.IsDirect,
                SourceItemId = a.SourceItemId
            };
        }).ToList();

        return results;
    }
}
