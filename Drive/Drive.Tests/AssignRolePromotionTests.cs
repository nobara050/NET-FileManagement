using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Commands.AssignRole;
using Drive.Application.Features.Roles.Models;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Moq;
using Xunit;

namespace Drive.Tests;

public class AssignRolePromotionTests
{
    private readonly Mock<IDriveItemRepository> _itemRepoMock = new();
    private readonly Mock<IDriveItemRoleAssignmentRepository> _assignmentRepoMock = new();
    private readonly Mock<ICurrentUserService> _userServiceMock = new();
    private readonly Mock<IPermissionMaterializer> _materializerMock = new();
    private readonly Mock<IRoleService> _roleServiceMock = new();
    private readonly Mock<IUserManagementService> _userManagementServiceMock = new();
    private readonly AssignRoleCommandHandler _handler;

    public AssignRolePromotionTests()
    {
        _handler = new AssignRoleCommandHandler(
            _itemRepoMock.Object,
            _assignmentRepoMock.Object,
            _userServiceMock.Object,
            _materializerMock.Object,
            _roleServiceMock.Object,
            _userManagementServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenInheritedAssignmentExists_PromotesToDirect()
    {
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _itemRepoMock.Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem { Id = itemId, OwnerId = ownerId, ItemType = DriveItemType.Folder });
        _roleServiceMock.Setup(x => x.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleResult { RoleId = roleId, Name = "Editor" });

        var inheritedAssignment = new DriveItemRoleAssignment
        {
            Id = Guid.NewGuid(),
            DriveItemId = itemId,
            UserId = targetUserId,
            RoleId = Guid.NewGuid(), // previously Viewer
            IsDirect = false,
            SourceItemId = Guid.NewGuid()
        };

        _assignmentRepoMock.Setup(x => x.GetAssignmentAsync(itemId, targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inheritedAssignment);

        var result = await _handler.Handle(new AssignRoleCommand(itemId, targetUserId, roleId), CancellationToken.None);

        Assert.True(result);
        Assert.True(inheritedAssignment.IsDirect);
        Assert.Equal(roleId, inheritedAssignment.RoleId);
        Assert.Null(inheritedAssignment.SourceItemId);
        Assert.Equal(ownerId, inheritedAssignment.CreatedBy);

        _assignmentRepoMock.Verify(x => x.Update(inheritedAssignment), Times.Once);
        _materializerMock.Verify(x => x.MaterializeAsync(itemId, targetUserId, roleId, ownerId, It.IsAny<CancellationToken>()), Times.Once);
        _assignmentRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDirectAssignmentAlreadyExists_ReturnsFalse()
    {
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _itemRepoMock.Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem { Id = itemId, OwnerId = ownerId, ItemType = DriveItemType.Folder });
        _roleServiceMock.Setup(x => x.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleResult { RoleId = roleId, Name = "Editor" });

        var explicitAssignment = new DriveItemRoleAssignment
        {
            Id = Guid.NewGuid(),
            DriveItemId = itemId,
            UserId = targetUserId,
            RoleId = roleId,
            IsDirect = true
        };

        _assignmentRepoMock.Setup(x => x.GetAssignmentAsync(itemId, targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(explicitAssignment);

        var result = await _handler.Handle(new AssignRoleCommand(itemId, targetUserId, roleId), CancellationToken.None);

        Assert.False(result);
        _assignmentRepoMock.Verify(x => x.Update(It.IsAny<DriveItemRoleAssignment>()), Times.Never);
        _assignmentRepoMock.Verify(x => x.AddAsync(It.IsAny<DriveItemRoleAssignment>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
