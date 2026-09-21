using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Queries.ListAssignments;
using Drive.Application.Features.Roles.Models;
using Drive.Application.Features.Users.Models;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Moq;
using Xunit;

namespace Drive.Tests;

public class ListAssignmentsTests
{
    private readonly Mock<IDriveItemRepository> _driveItemRepoMock = new();
    private readonly Mock<IDriveItemRoleAssignmentRepository> _assignmentRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IRoleService> _roleServiceMock = new();
    private readonly Mock<IUserManagementService> _userManagementServiceMock = new();
    private readonly ListAssignmentsQueryHandler _handler;

    public ListAssignmentsTests()
    {
        _handler = new ListAssignmentsQueryHandler(
            _driveItemRepoMock.Object,
            _assignmentRepoMock.Object,
            _currentUserServiceMock.Object,
            _roleServiceMock.Object,
            _userManagementServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidOwner_ReturnsAssignmentsWithEmailAndDisplayName()
    {
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var driveItemId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(ownerId);

        _driveItemRepoMock.Setup(x => x.GetByIdAsync(driveItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem
            {
                Id = driveItemId,
                OwnerId = ownerId,
                ItemType = DriveItemType.File,
                IsDeleted = false
            });

        _assignmentRepoMock.Setup(x => x.ListByDriveItemIdAsync(driveItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveItemRoleAssignment>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    DriveItemId = driveItemId,
                    UserId = targetUserId,
                    RoleId = roleId,
                    IsDirect = true,
                    CreatedBy = ownerId
                }
            });

        _roleServiceMock.Setup(x => x.ListRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoleResult>
            {
                new() { RoleId = roleId, Name = "Downloader" }
            });

        _userManagementServiceMock.Setup(x => x.ListUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserResult>
            {
                new() { UserId = targetUserId, Email = "downloader@test", DisplayName = "Downloader User" }
            });

        var result = await _handler.Handle(new ListAssignmentsQuery(driveItemId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(targetUserId, result[0].UserId);
        Assert.Equal("downloader@test", result[0].UserEmail);
        Assert.Equal("Downloader User", result[0].DisplayName);
        Assert.Equal("Downloader", result[0].RoleName);
    }
}
