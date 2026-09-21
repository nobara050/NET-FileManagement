using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Commands.RemoveAssignment;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Moq;
using Xunit;

namespace Drive.Tests;

public class RemoveAssignmentTests
{
    private readonly Mock<IDriveItemRepository> _itemRepoMock = new();
    private readonly Mock<IDriveItemRoleAssignmentRepository> _assignmentRepoMock = new();
    private readonly Mock<ICurrentUserService> _userServiceMock = new();
    private readonly Mock<IPermissionMaterializer> _materializerMock = new();
    private readonly RemoveAssignmentCommandHandler _handler;

    public RemoveAssignmentTests()
    {
        _handler = new RemoveAssignmentCommandHandler(
            _itemRepoMock.Object,
            _assignmentRepoMock.Object,
            _userServiceMock.Object,
            _materializerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenDirectAssignmentRemoved_CallsRemoveInheritedFromScopeWithCurrentItemId()
    {
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _itemRepoMock.Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem { Id = itemId, OwnerId = ownerId, ItemType = DriveItemType.Folder });

        var directAssignment = new DriveItemRoleAssignment
        {
            Id = Guid.NewGuid(),
            DriveItemId = itemId,
            UserId = targetUserId,
            RoleId = roleId,
            IsDirect = true,
            SourceItemId = null,
            CreatedBy = ownerId
        };

        _assignmentRepoMock.Setup(x => x.GetAssignmentAsync(itemId, targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directAssignment);

        var result = await _handler.Handle(new RemoveAssignmentCommand(itemId, targetUserId), CancellationToken.None);

        Assert.True(result);
        _materializerMock.Verify(x => x.RemoveInheritedFromScopeAsync(itemId, targetUserId, itemId, It.IsAny<CancellationToken>()), Times.Once);
        _assignmentRepoMock.Verify(x => x.Remove(directAssignment), Times.Once);
        _assignmentRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInheritedAssignmentRemovedOnSubfolder_CallsRemoveInheritedFromScopeWithSourceItemId()
    {
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var rootFolderId = Guid.NewGuid();
        var subfolderId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _itemRepoMock.Setup(x => x.GetByIdAsync(subfolderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem { Id = subfolderId, OwnerId = ownerId, ItemType = DriveItemType.Folder });

        var inheritedAssignment = new DriveItemRoleAssignment
        {
            Id = Guid.NewGuid(),
            DriveItemId = subfolderId,
            UserId = targetUserId,
            RoleId = roleId,
            IsDirect = false,
            SourceItemId = rootFolderId,
            CreatedBy = ownerId
        };

        _assignmentRepoMock.Setup(x => x.GetAssignmentAsync(subfolderId, targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inheritedAssignment);

        var result = await _handler.Handle(new RemoveAssignmentCommand(subfolderId, targetUserId), CancellationToken.None);

        Assert.True(result);
        _materializerMock.Verify(x => x.RemoveInheritedFromScopeAsync(subfolderId, targetUserId, rootFolderId, It.IsAny<CancellationToken>()), Times.Once);
        _assignmentRepoMock.Verify(x => x.Remove(inheritedAssignment), Times.Once);
        _assignmentRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwner_ReturnsFalse()
    {
        var ownerId = Guid.NewGuid();
        var notOwnerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(notOwnerId);
        _itemRepoMock.Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem { Id = itemId, OwnerId = ownerId, ItemType = DriveItemType.Folder });

        var result = await _handler.Handle(new RemoveAssignmentCommand(itemId, targetUserId), CancellationToken.None);

        Assert.False(result);
        _assignmentRepoMock.Verify(x => x.Remove(It.IsAny<DriveItemRoleAssignment>()), Times.Never);
    }
}
