using AutoMapper;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Commands.RenameDriveItem;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Moq;
using Xunit;

namespace Drive.Tests;

public class RenameDriveItemTests
{
    private readonly Mock<IDriveItemRepository> _itemRepoMock = new();
    private readonly Mock<ICurrentUserService> _userServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly RenameDriveItemCommandHandler _handler;

    public RenameDriveItemTests()
    {
        _handler = new RenameDriveItemCommandHandler(
            _itemRepoMock.Object,
            _userServiceMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCallerNotOwner_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(callerId);
        _itemRepoMock.Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem { Id = itemId, OwnerId = ownerId, ItemType = DriveItemType.File });

        await Assert.ThrowsAsync<Drive.Application.Common.Exceptions.ForbiddenException>(() =>
            _handler.Handle(new RenameDriveItemCommand(itemId, "new_name.txt"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenDuplicateNameExists_ThrowsConflictException()
    {
        var ownerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _itemRepoMock.Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem { Id = itemId, OwnerId = ownerId, ItemType = DriveItemType.File, Name = "old.txt" });
        _itemRepoMock.Setup(x => x.IsDuplicateNameAsync(ownerId, null, "new.txt", itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<Drive.Application.Common.Exceptions.ConflictException>(() =>
            _handler.Handle(new RenameDriveItemCommand(itemId, "new.txt"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenValid_RenamesItemAndSaves()
    {
        var ownerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var item = new DriveItem { Id = itemId, OwnerId = ownerId, ItemType = DriveItemType.File, Name = "old.txt" };

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _itemRepoMock.Setup(x => x.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _itemRepoMock.Setup(x => x.IsDuplicateNameAsync(ownerId, null, "new.txt", itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mapperMock.Setup(x => x.Map<DriveItemResult>(It.IsAny<DriveItem>()))
            .Returns((DriveItem i) => new DriveItemResult { Id = i.Id, Name = i.Name });

        var result = await _handler.Handle(new RenameDriveItemCommand(itemId, "new.txt"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("new.txt", result.Name);
        Assert.Equal("new.txt", item.Name);

        _itemRepoMock.Verify(x => x.Update(item), Times.Once);
        _itemRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
