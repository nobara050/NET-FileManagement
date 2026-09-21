using AutoMapper;
using Drive.Application;
using Drive.Application.Common.Exceptions;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Commands.CreateFolder;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Drive.Tests;

public class CreateFolderPermissionPropagationTests
{
    private readonly Mock<IDriveItemRepository> _repoMock = new();
    private readonly Mock<IDriveItemRoleAssignmentRepository> _assignmentRepoMock = new();
    private readonly Mock<ICurrentUserService> _userServiceMock = new();
    private readonly Mock<IPermissionService> _permissionServiceMock = new();
    private readonly IMapper _mapper;
    private readonly CreateFolderCommandHandler _handler;

    public CreateFolderPermissionPropagationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        var sp = services.BuildServiceProvider();
        _mapper = sp.GetRequiredService<IMapper>();

        _handler = new CreateFolderCommandHandler(
            _repoMock.Object,
            _assignmentRepoMock.Object,
            _userServiceMock.Object,
            _permissionServiceMock.Object,
            _mapper);
    }

    [Fact]
    public async Task Handle_WhenNonOwnerAttemptsToCreateInSharedParentWithoutPermission_ThrowsForbiddenException()
    {
        var parentOwnerId = Guid.NewGuid();
        var creatorUserId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(creatorUserId);

        var parentFolder = new DriveItem
        {
            Id = parentId,
            OwnerId = parentOwnerId,
            ItemType = DriveItemType.Folder,
            IsDeleted = false
        };

        _repoMock.Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentFolder);
        _permissionServiceMock.Setup(x => x.HasPermissionAsync(creatorUserId, parentId, Drive.Application.Common.Authorization.Permissions.DriveCreate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new CreateFolderCommand(parentId, "NewSubfolder"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNonOwnerHasDriveCreatePermissionInSharedParent_Success()
    {
        var parentOwnerId = Guid.NewGuid();
        var creatorUserId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(creatorUserId);

        var parentFolder = new DriveItem
        {
            Id = parentId,
            OwnerId = parentOwnerId,
            ItemType = DriveItemType.Folder,
            IsDeleted = false
        };

        _repoMock.Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentFolder);
        _repoMock.Setup(x => x.IsDuplicateNameAsync(creatorUserId, parentId, "NewSubfolder", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _permissionServiceMock.Setup(x => x.HasPermissionAsync(creatorUserId, parentId, Drive.Application.Common.Authorization.Permissions.DriveCreate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new CreateFolderCommand(parentId, "NewSubfolder"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("NewSubfolder", result.Name);

        _repoMock.Verify(x => x.AddAsync(It.IsAny<DriveItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOwnerCreatesInParent_Success()
    {
        var ownerId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(ownerId);

        var parentFolder = new DriveItem
        {
            Id = parentId,
            OwnerId = ownerId,
            ItemType = DriveItemType.Folder,
            IsDeleted = false
        };

        _repoMock.Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentFolder);

        _repoMock.Setup(x => x.IsDuplicateNameAsync(ownerId, parentId, "NewSubfolder", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new CreateFolderCommand(parentId, "NewSubfolder"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("NewSubfolder", result.Name);

        _repoMock.Verify(x => x.AddAsync(It.IsAny<DriveItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
