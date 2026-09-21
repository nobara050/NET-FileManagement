using AutoMapper;
using Drive.Application.Common.Authorization;
using Drive.Application.Common.Exceptions;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems;
using Drive.Application.Features.DriveItems.Models;
using Drive.Application.Features.DriveItems.Queries.ListDriveItems;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Moq;
using Xunit;

namespace Drive.Tests;

public class ListDriveItemsAccessibleTests
{
    private readonly Mock<IDriveItemRepository> _driveItemRepoMock = new();
    private readonly Mock<ISharedDriveItemQuery> _sharedQueryMock = new();
    private readonly Mock<IDriveItemAccessQuery> _accessQueryMock = new();
    private readonly Mock<IPermissionService> _permissionServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly ListDriveItemsQueryHandler _handler;

    public ListDriveItemsAccessibleTests()
    {
        _handler = new ListDriveItemsQueryHandler(
            _driveItemRepoMock.Object,
            _sharedQueryMock.Object,
            _accessQueryMock.Object,
            _permissionServiceMock.Object,
            _currentUserServiceMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_WhenBrowsingSharedFolderAsNonOwner_UsesDriveItemAccessQuery()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var parentFolderId = Guid.NewGuid();
        var accessibleChildId = Guid.NewGuid();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(viewerId);

        _driveItemRepoMock.Setup(x => x.GetByIdAsync(parentFolderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem
            {
                Id = parentFolderId,
                OwnerId = ownerId,
                ItemType = DriveItemType.Folder,
                IsDeleted = false
            });

        _permissionServiceMock.Setup(x => x.HasPermissionAsync(viewerId, parentFolderId, Permissions.DriveRead, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var accessibleChild = new DriveItem
        {
            Id = accessibleChildId,
            ParentId = parentFolderId,
            OwnerId = ownerId,
            Name = "accessible_folder",
            ItemType = DriveItemType.Folder
        };

        _accessQueryMock.Setup(x => x.ListAccessibleAsync(
                viewerId,
                parentFolderId,
                null,
                null,
                Permissions.DriveRead,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveItem> { accessibleChild });

        _accessQueryMock.Setup(x => x.CountAccessibleAsync(
                viewerId,
                parentFolderId,
                null,
                null,
                Permissions.DriveRead,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(x => x.Map<IReadOnlyList<DriveItemResult>>(It.IsAny<IReadOnlyList<DriveItem>>()))
            .Returns(new List<DriveItemResult>
            {
                new() { Id = accessibleChildId, Name = "accessible_folder" }
            });

        var query = new ListDriveItemsQuery(
            parentFolderId,
            null,
            null,
            ListDriveItemsScope.Shared,
            1,
            20);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(accessibleChildId, result.Items[0].Id);
        Assert.Equal(1, result.TotalCount);

        // Verify that DriveItemAccessQuery was used, NOT DriveItemRepository.ListFolderItemsAsync
        _accessQueryMock.Verify(x => x.ListAccessibleAsync(viewerId, parentFolderId, null, null, Permissions.DriveRead, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
        _driveItemRepoMock.Verify(x => x.ListFolderItemsAsync(It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<DriveItemType?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBrowsingOwnedFolderAsOwner_UsesDriveItemRepository()
    {
        var ownerId = Guid.NewGuid();
        var parentFolderId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(ownerId);

        _driveItemRepoMock.Setup(x => x.GetByIdAsync(parentFolderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem
            {
                Id = parentFolderId,
                OwnerId = ownerId,
                ItemType = DriveItemType.Folder,
                IsDeleted = false
            });

        var child = new DriveItem
        {
            Id = childId,
            ParentId = parentFolderId,
            OwnerId = ownerId,
            Name = "my_file.txt",
            ItemType = DriveItemType.File
        };

        _driveItemRepoMock.Setup(x => x.ListFolderItemsAsync(
                parentFolderId,
                ownerId,
                null,
                null,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveItem> { child });

        _driveItemRepoMock.Setup(x => x.CountFolderItemsAsync(
                parentFolderId,
                ownerId,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(x => x.Map<IReadOnlyList<DriveItemResult>>(It.IsAny<IReadOnlyList<DriveItem>>()))
            .Returns(new List<DriveItemResult>
            {
                new() { Id = childId, Name = "my_file.txt" }
            });

        var query = new ListDriveItemsQuery(
            parentFolderId,
            null,
            null,
            ListDriveItemsScope.Owned,
            1,
            20);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(childId, result.Items[0].Id);

        // Verify that DriveItemRepository was used, NOT DriveItemAccessQuery
        _driveItemRepoMock.Verify(x => x.ListFolderItemsAsync(parentFolderId, ownerId, null, null, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
        _accessQueryMock.Verify(x => x.ListAccessibleAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DriveItemType?>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNonOwnerHasNoPermissionOnParent_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var parentFolderId = Guid.NewGuid();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(viewerId);

        _driveItemRepoMock.Setup(x => x.GetByIdAsync(parentFolderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem
            {
                Id = parentFolderId,
                OwnerId = ownerId,
                ItemType = DriveItemType.Folder,
                IsDeleted = false
            });

        _permissionServiceMock.Setup(x => x.HasPermissionAsync(viewerId, parentFolderId, Permissions.DriveRead, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var query = new ListDriveItemsQuery(
            parentFolderId,
            null,
            null,
            ListDriveItemsScope.Owned,
            1,
            20);

        await Assert.ThrowsAsync<ForbiddenException>(() => _handler.Handle(query, CancellationToken.None));
    }
}
