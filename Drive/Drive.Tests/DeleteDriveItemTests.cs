using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Commands.DeleteDriveItem;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Moq;

namespace Drive.Tests;

public class DeleteDriveItemTests
{
    private readonly Mock<IDriveItemRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _userServiceMock = new();
    private readonly Mock<IPermissionService> _permissionServiceMock = new();
    private readonly DeleteDriveItemCommandHandler _handler;

    public DeleteDriveItemTests()
    {
        _handler = new DeleteDriveItemCommandHandler(
            _repoMock.Object,
            _userServiceMock.Object,
            _permissionServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCallerNotAuthenticated_ThrowsUnauthorizedException()
    {
        _userServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<Drive.Application.Common.Exceptions.UnauthorizedException>(() =>
            _handler.Handle(new DeleteDriveItemCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenItemNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.UserId).Returns(userId);
        _repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DriveItem?)null);

        await Assert.ThrowsAsync<Drive.Application.Common.Exceptions.NotFoundException>(() =>
            _handler.Handle(new DeleteDriveItemCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenItemAlreadyDeleted_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var item = new DriveItem
        {
            Id = itemId,
            OwnerId = userId,
            Name = "file.txt",
            ItemType = DriveItemType.File,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId);
        _repoMock.Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        await Assert.ThrowsAsync<Drive.Application.Common.Exceptions.NotFoundException>(() =>
            _handler.Handle(new DeleteDriveItemCommand(itemId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenItemIsFile_SoftDeletesFile()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var file = new DriveItem
        {
            Id = itemId,
            OwnerId = userId,
            Name = "document.pdf",
            ItemType = DriveItemType.File,
            IsDeleted = false
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId);
        _repoMock.Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        var result = await _handler.Handle(new DeleteDriveItemCommand(itemId), CancellationToken.None);

        Assert.True(result);
        Assert.True(file.IsDeleted);
        Assert.NotNull(file.DeletedAt);
        _repoMock.Verify(x => x.Update(file), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenItemIsFolder_SoftDeletesFolderAndCascadesToDescendants()
    {
        var userId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var folder = new DriveItem
        {
            Id = folderId,
            OwnerId = userId,
            Name = "Projects",
            ItemType = DriveItemType.Folder,
            IsDeleted = false
        };

        var childSubfolder = new DriveItem
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            ParentId = folderId,
            Name = "2026",
            ItemType = DriveItemType.Folder,
            IsDeleted = false
        };

        var nestedFile = new DriveItem
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            ParentId = childSubfolder.Id,
            Name = "notes.txt",
            ItemType = DriveItemType.File,
            IsDeleted = false
        };

        var descendants = new List<DriveItem> { childSubfolder, nestedFile };

        _userServiceMock.Setup(x => x.UserId).Returns(userId);
        _repoMock.Setup(x => x.GetByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        _repoMock.Setup(x => x.GetActiveDescendantsAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(descendants);

        var result = await _handler.Handle(new DeleteDriveItemCommand(folderId), CancellationToken.None);

        Assert.True(result);
        Assert.True(folder.IsDeleted);
        Assert.NotNull(folder.DeletedAt);

        // Descendants are also soft-deleted with the same timestamp
        Assert.True(childSubfolder.IsDeleted);
        Assert.Equal(folder.DeletedAt, childSubfolder.DeletedAt);
        Assert.True(nestedFile.IsDeleted);
        Assert.Equal(folder.DeletedAt, nestedFile.DeletedAt);

        _repoMock.Verify(x => x.Update(childSubfolder), Times.Once);
        _repoMock.Verify(x => x.Update(nestedFile), Times.Once);
        _repoMock.Verify(x => x.Update(folder), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
