using AutoMapper;
using Drive.Application;
using Drive.Application.Common.Exceptions;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Commands.RecoverDriveItem;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Drive.Tests;

public class RecoverDriveItemTests
{
    private readonly Mock<IDriveItemRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _userServiceMock = new();
    private readonly IMapper _mapper;
    private readonly RecoverDriveItemCommandHandler _handler;

    public RecoverDriveItemTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        var sp = services.BuildServiceProvider();
        _mapper = sp.GetRequiredService<IMapper>();
        _handler = new RecoverDriveItemCommandHandler(_repoMock.Object, _userServiceMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_WhenCallerNotAuthenticated_ThrowsUnauthorizedException()
    {
        _userServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _handler.Handle(new RecoverDriveItemCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenItemNotFoundInTrash_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.UserId).Returns(userId);
        _repoMock.Setup(x => x.GetDeletedByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DriveItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new RecoverDriveItemCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwner_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var item = new DriveItem
        {
            Id = itemId,
            OwnerId = ownerId,
            Name = "secret.docx",
            ItemType = DriveItemType.File,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        _userServiceMock.Setup(x => x.UserId).Returns(callerId);
        _repoMock.Setup(x => x.GetDeletedByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new RecoverDriveItemCommand(itemId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenParentFolderIsDeleted_ThrowsConflictException()
    {
        var ownerId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var item = new DriveItem
        {
            Id = itemId,
            OwnerId = ownerId,
            ParentId = parentId,
            Name = "child.txt",
            ItemType = DriveItemType.File,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        var parent = new DriveItem
        {
            Id = parentId,
            OwnerId = ownerId,
            Name = "ParentFolder",
            ItemType = DriveItemType.Folder,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _repoMock.Setup(x => x.GetDeletedByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _repoMock.Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(new RecoverDriveItemCommand(itemId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenActiveDuplicateNameExistsInDestination_ThrowsConflictException()
    {
        var ownerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var item = new DriveItem
        {
            Id = itemId,
            OwnerId = ownerId,
            ParentId = null,
            Name = "Report.pdf",
            ItemType = DriveItemType.File,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _repoMock.Setup(x => x.GetDeletedByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _repoMock.Setup(x => x.IsDuplicateNameAsync(ownerId, null, "Report.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(new RecoverDriveItemCommand(itemId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenDuplicateNameExistsOnlyInTrash_RecoversSuccessfully()
    {
        var ownerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var item = new DriveItem
        {
            Id = itemId,
            OwnerId = ownerId,
            ParentId = null,
            Name = "Photo.jpg",
            ItemType = DriveItemType.File,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _repoMock.Setup(x => x.GetDeletedByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _repoMock.Setup(x => x.IsDuplicateNameAsync(ownerId, null, "Photo.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new RecoverDriveItemCommand(itemId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Photo.jpg", result.Name);
        Assert.False(item.IsDeleted);
        Assert.Null(item.DeletedAt);
        _repoMock.Verify(x => x.Update(item), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFolderIsRecovered_RestoresFolderAndCascadedDescendants()
    {
        var ownerId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var deletedTimestamp = DateTimeOffset.UtcNow.AddDays(-2);

        var folder = new DriveItem
        {
            Id = folderId,
            OwnerId = ownerId,
            ParentId = null,
            Name = "MyFolder",
            ItemType = DriveItemType.Folder,
            IsDeleted = true,
            DeletedAt = deletedTimestamp
        };

        var cascadedFile = new DriveItem
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            ParentId = folderId,
            Name = "inner.txt",
            ItemType = DriveItemType.File,
            IsDeleted = true,
            DeletedAt = deletedTimestamp
        };

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);
        _repoMock.Setup(x => x.GetDeletedByIdAsync(folderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        _repoMock.Setup(x => x.IsDuplicateNameAsync(ownerId, null, "MyFolder", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock.Setup(x => x.GetCascadedDeletedDescendantsAsync(folderId, deletedTimestamp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveItem> { cascadedFile });

        var result = await _handler.Handle(new RecoverDriveItemCommand(folderId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("MyFolder", result.Name);
        Assert.False(folder.IsDeleted);
        Assert.Null(folder.DeletedAt);
        Assert.False(cascadedFile.IsDeleted);
        Assert.Null(cascadedFile.DeletedAt);

        _repoMock.Verify(x => x.Update(cascadedFile), Times.Once);
        _repoMock.Verify(x => x.Update(folder), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
