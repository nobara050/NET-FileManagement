using AutoMapper;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Commands.MoveDriveItem;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Moq;
using Xunit;

namespace Drive.Tests;

public class MoveDriveItemTests
{
    private readonly Mock<IDriveItemRepository> _itemRepoMock = new();
    private readonly Mock<IDriveItemRoleAssignmentRepository> _assignmentRepoMock = new();
    private readonly Mock<ICurrentUserService> _userServiceMock = new();
    private readonly Mock<IPermissionService> _permissionServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly MoveDriveItemCommandHandler _handler;

    public MoveDriveItemTests()
    {
        _handler = new MoveDriveItemCommandHandler(
            _itemRepoMock.Object,
            _assignmentRepoMock.Object,
            _userServiceMock.Object,
            _permissionServiceMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCallerNotOwnerAndNoPermission_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(callerId);
        _itemRepoMock.Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem { Id = itemId, OwnerId = ownerId, ItemType = DriveItemType.File });
        _permissionServiceMock.Setup(x => x.HasPermissionAsync(callerId, itemId, Drive.Application.Common.Authorization.Permissions.DriveMove, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<Drive.Application.Common.Exceptions.ForbiddenException>(() =>
            _handler.Handle(new MoveDriveItemCommand(itemId, null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenMovingFolderIntoItsChild_ThrowsBadRequestException()
    {
        var ownerId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var childFolderId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _itemRepoMock.Setup(x => x.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem { Id = folderId, OwnerId = ownerId, ItemType = DriveItemType.Folder });
        _itemRepoMock.Setup(x => x.GetByIdAsync(childFolderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem { Id = childFolderId, OwnerId = ownerId, ItemType = DriveItemType.Folder, ParentId = folderId });
        _itemRepoMock.Setup(x => x.GetActiveDescendantsAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveItem> { new DriveItem { Id = childFolderId, ParentId = folderId } });

        await Assert.ThrowsAsync<Drive.Application.Common.Exceptions.BadRequestException>(() =>
            _handler.Handle(new MoveDriveItemCommand(folderId, childFolderId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenValidMove_PreservesExplicitRolesAndInheritsNewParentRoles()
    {
        var ownerId = Guid.NewGuid();
        var targetParentId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var collaboratorA = Guid.NewGuid();
        var collaboratorB = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        var item = new DriveItem { Id = itemId, OwnerId = ownerId, ItemType = DriveItemType.File, Name = "test.txt" };
        _itemRepoMock.Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _itemRepoMock.Setup(x => x.GetByIdAsync(targetParentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem { Id = targetParentId, OwnerId = ownerId, ItemType = DriveItemType.Folder });
        _itemRepoMock.Setup(x => x.IsDuplicateNameAsync(ownerId, targetParentId, "test.txt", itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // New parent has role assignment for collaborator A and collaborator B
        _assignmentRepoMock.Setup(x => x.ListByDriveItemIdAsync(targetParentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveItemRoleAssignment>
            {
                new() { UserId = collaboratorA, RoleId = roleId, CreatedBy = ownerId },
                new() { UserId = collaboratorB, RoleId = roleId, CreatedBy = ownerId }
            });

        // Collaborator A already has a direct assignment on item -> should be kept and not overwritten
        _assignmentRepoMock.Setup(x => x.ExistsDirectAssignmentAsync(itemId, collaboratorA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _assignmentRepoMock.Setup(x => x.ExistsDirectAssignmentAsync(itemId, collaboratorB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mapperMock.Setup(x => x.Map<DriveItemResult>(It.IsAny<DriveItem>()))
            .Returns(new DriveItemResult { Id = itemId, Name = "test.txt" });

        var result = await _handler.Handle(new MoveDriveItemCommand(itemId, targetParentId), CancellationToken.None);

        Assert.NotNull(result);
        // Inherited assignments on subtree must have been purged
        _assignmentRepoMock.Verify(x => x.RemoveInheritedByItemIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(itemId)), It.IsAny<CancellationToken>()), Times.Once);
        // Only collaborator B gets new inherited assignment (collaborator A skipped because direct role exists)
        _assignmentRepoMock.Verify(x => x.AddAsync(It.Is<DriveItemRoleAssignment>(a => a.UserId == collaboratorB && !a.IsDirect), It.IsAny<CancellationToken>()), Times.Once);
        _assignmentRepoMock.Verify(x => x.AddAsync(It.Is<DriveItemRoleAssignment>(a => a.UserId == collaboratorA), It.IsAny<CancellationToken>()), Times.Never);
        _itemRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNonOwnerHasDriveMovePermission_MovesSuccessfully()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var targetParentId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(callerId);
        var item = new DriveItem { Id = itemId, OwnerId = ownerId, ItemType = DriveItemType.File, Name = "doc.pdf" };
        _itemRepoMock.Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _itemRepoMock.Setup(x => x.GetByIdAsync(targetParentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem { Id = targetParentId, OwnerId = ownerId, ItemType = DriveItemType.Folder });
        _itemRepoMock.Setup(x => x.IsDuplicateNameAsync(callerId, targetParentId, "doc.pdf", itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Caller has drive.move on item and drive.create on targetParent
        _permissionServiceMock.Setup(x => x.HasPermissionAsync(callerId, itemId, Drive.Application.Common.Authorization.Permissions.DriveMove, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _permissionServiceMock.Setup(x => x.HasPermissionAsync(callerId, targetParentId, Drive.Application.Common.Authorization.Permissions.DriveCreate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _assignmentRepoMock.Setup(x => x.ListByDriveItemIdAsync(targetParentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveItemRoleAssignment>());

        _mapperMock.Setup(x => x.Map<DriveItemResult>(It.IsAny<DriveItem>()))
            .Returns(new DriveItemResult { Id = itemId, Name = "doc.pdf" });

        var result = await _handler.Handle(new MoveDriveItemCommand(itemId, targetParentId), CancellationToken.None);

        Assert.NotNull(result);
        _itemRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
