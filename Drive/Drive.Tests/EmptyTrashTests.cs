using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Commands.EmptyTrash;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Drive.Tests;

public class EmptyTrashTests
{
    private readonly Mock<IDriveItemRepository> _itemRepoMock = new();
    private readonly Mock<ICurrentUserService> _userServiceMock = new();
    private readonly Mock<IFileStorage> _fileStorageMock = new();
    private readonly Mock<ILogger<EmptyTrashCommandHandler>> _loggerMock = new();
    private readonly EmptyTrashCommandHandler _handler;

    public EmptyTrashTests()
    {
        _handler = new EmptyTrashCommandHandler(
            _itemRepoMock.Object,
            _userServiceMock.Object,
            _fileStorageMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNoDeletedItems_ReturnsZero()
    {
        var ownerId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _itemRepoMock.Setup(x => x.GetUserDeletedItemsAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveItem>());

        var result = await _handler.Handle(new EmptyTrashCommand(), CancellationToken.None);

        Assert.Equal(0, result);
        _itemRepoMock.Verify(x => x.HardDeleteItemsAsync(It.IsAny<IEnumerable<DriveItem>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDeletedItemsExist_PurgesS3AndDeletesFromDb()
    {
        var ownerId = Guid.NewGuid();
        var deletedFile = new DriveItem
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            ItemType = DriveItemType.File,
            IsDeleted = true,
            Versions = new List<FileVersion>
            {
                new() { S3ObjectKey = "files/trash/v1" }
            }
        };

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _itemRepoMock.Setup(x => x.GetUserDeletedItemsAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveItem> { deletedFile });

        var result = await _handler.Handle(new EmptyTrashCommand(), CancellationToken.None);

        Assert.Equal(1, result);
        _fileStorageMock.Verify(x => x.DeleteAsync("files/trash/v1", It.IsAny<CancellationToken>()), Times.Once);
        _itemRepoMock.Verify(x => x.HardDeleteItemsAsync(It.Is<IEnumerable<DriveItem>>(items => items.Contains(deletedFile)), It.IsAny<CancellationToken>()), Times.Once);
    }
}
