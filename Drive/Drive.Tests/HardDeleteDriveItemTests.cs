using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Commands.HardDeleteDriveItem;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Drive.Tests;

public class HardDeleteDriveItemTests
{
    private readonly Mock<IDriveItemRepository> _itemRepoMock = new();
    private readonly Mock<ICurrentUserService> _userServiceMock = new();
    private readonly Mock<IFileStorage> _fileStorageMock = new();
    private readonly Mock<ILogger<HardDeleteDriveItemCommandHandler>> _loggerMock = new();
    private readonly HardDeleteDriveItemCommandHandler _handler;

    public HardDeleteDriveItemTests()
    {
        _handler = new HardDeleteDriveItemCommandHandler(
            _itemRepoMock.Object,
            _userServiceMock.Object,
            _fileStorageMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenItemNotDeleted_ReturnsFalse()
    {
        var ownerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _itemRepoMock.Setup(x => x.GetDeletedByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem { Id = itemId, OwnerId = ownerId, IsDeleted = false });

        var result = await _handler.Handle(new HardDeleteDriveItemCommand(itemId), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_WhenCallerNotOwner_ReturnsFalse()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(callerId);
        _itemRepoMock.Setup(x => x.GetDeletedByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem { Id = itemId, OwnerId = ownerId, IsDeleted = true });

        var result = await _handler.Handle(new HardDeleteDriveItemCommand(itemId), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_WhenValidOwner_PurgesS3AndDatabase()
    {
        var ownerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var fileItem = new DriveItem
        {
            Id = itemId,
            OwnerId = ownerId,
            ItemType = DriveItemType.File,
            IsDeleted = true,
            Versions = new List<FileVersion>
            {
                new() { S3ObjectKey = "files/test/v1" }
            }
        };

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _itemRepoMock.Setup(x => x.GetDeletedByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileItem);

        var result = await _handler.Handle(new HardDeleteDriveItemCommand(itemId), CancellationToken.None);

        Assert.True(result);
        _fileStorageMock.Verify(x => x.DeleteAsync("files/test/v1", It.IsAny<CancellationToken>()), Times.Once);
        _itemRepoMock.Verify(x => x.HardDeleteItemsAsync(It.Is<IEnumerable<DriveItem>>(items => items.Contains(fileItem)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenS3DeleteFails_ThrowsExceptionAndDoesNotPurgeDatabase()
    {
        var ownerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var fileItem = new DriveItem
        {
            Id = itemId,
            OwnerId = ownerId,
            ItemType = DriveItemType.File,
            IsDeleted = true,
            Versions = new List<FileVersion>
            {
                new() { S3ObjectKey = "files/test/v1" }
            }
        };

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _itemRepoMock.Setup(x => x.GetDeletedByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileItem);
        _fileStorageMock.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("S3 is unreachable"));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _handler.Handle(new HardDeleteDriveItemCommand(itemId), CancellationToken.None));

        _itemRepoMock.Verify(x => x.HardDeleteItemsAsync(It.IsAny<IEnumerable<DriveItem>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
