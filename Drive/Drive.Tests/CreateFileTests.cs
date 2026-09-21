using AutoMapper;
using Drive.Application;
using Drive.Application.Common.Exceptions;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Commands.CreateFile;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Drive.Tests;

public class CreateFileTests
{
    private readonly Mock<IDriveItemRepository> _itemRepoMock = new();
    private readonly Mock<IDriveItemRoleAssignmentRepository> _assignmentRepoMock = new();
    private readonly Mock<IRepository<FileVersion>> _fileVersionRepoMock = new();
    private readonly Mock<ICurrentUserService> _userServiceMock = new();
    private readonly Mock<IPermissionService> _permissionServiceMock = new();
    private readonly Mock<IFileStorage> _fileStorageMock = new();
    private readonly IMapper _mapper;
    private readonly CreateFileCommandHandler _handler;

    public CreateFileTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        var sp = services.BuildServiceProvider();
        _mapper = sp.GetRequiredService<IMapper>();

        _handler = new CreateFileCommandHandler(
            _itemRepoMock.Object,
            _assignmentRepoMock.Object,
            _fileVersionRepoMock.Object,
            _userServiceMock.Object,
            _permissionServiceMock.Object,
            _fileStorageMock.Object,
            _mapper);
    }

    private static FileUpload CreateTestUpload(string fileName = "test.txt")
    {
        var stream = new MemoryStream("test content"u8.ToArray());
        return new FileUpload
        {
            FileName = fileName,
            ContentType = "text/plain",
            Length = stream.Length,
            Content = stream
        };
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _userServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var command = new CreateFileCommand(null, CreateTestUpload());

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenParentNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(userId);
        _itemRepoMock.Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DriveItem?)null);

        var command = new CreateFileCommand(parentId, CreateTestUpload());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenCallerNotOwnerOfParentAndNoPermission_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        var parentOwnerId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(userId);
        _itemRepoMock.Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem
            {
                Id = parentId,
                OwnerId = parentOwnerId,
                ItemType = DriveItemType.Folder
            });
        _permissionServiceMock.Setup(x => x.HasPermissionAsync(userId, parentId, Drive.Application.Common.Authorization.Permissions.DriveCreate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateFileCommand(parentId, CreateTestUpload());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenCallerNotOwnerOfParentHasDriveCreatePermission_UploadsSuccessfully()
    {
        var userId = Guid.NewGuid();
        var parentOwnerId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var upload = CreateTestUpload("shared-file.txt");

        _userServiceMock.Setup(x => x.UserId).Returns(userId);
        _itemRepoMock.Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem
            {
                Id = parentId,
                OwnerId = parentOwnerId,
                ItemType = DriveItemType.Folder
            });
        _itemRepoMock.Setup(x => x.IsDuplicateNameAsync(userId, parentId, "shared-file.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _permissionServiceMock.Setup(x => x.HasPermissionAsync(userId, parentId, Drive.Application.Common.Authorization.Permissions.DriveCreate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileStorageMock.Setup(x => x.BucketName).Returns("test-bucket");

        var command = new CreateFileCommand(parentId, upload);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        _itemRepoMock.Verify(x => x.AddAsync(It.IsAny<DriveItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _itemRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenParentIsNotFolder_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.UserId).Returns(userId);
        _itemRepoMock.Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DriveItem
            {
                Id = parentId,
                OwnerId = userId,
                ItemType = DriveItemType.File
            });

        var command = new CreateFileCommand(parentId, CreateTestUpload());

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenDuplicateNameExists_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var upload = CreateTestUpload("duplicate.txt");

        _userServiceMock.Setup(x => x.UserId).Returns(userId);
        _itemRepoMock.Setup(x => x.IsDuplicateNameAsync(userId, null, "duplicate.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateFileCommand(null, upload);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenValid_UploadsToStorageAndSavesItem()
    {
        var userId = Guid.NewGuid();
        var upload = CreateTestUpload("document.txt");

        _userServiceMock.Setup(x => x.UserId).Returns(userId);
        _itemRepoMock.Setup(x => x.IsDuplicateNameAsync(userId, null, "document.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _fileStorageMock.Setup(x => x.BucketName).Returns("test-bucket");

        var command = new CreateFileCommand(null, upload);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("document.txt", result.Name);
        Assert.Equal(DriveItemType.File, result.ItemType);

        _fileStorageMock.Verify(x => x.UploadAsync(
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            "text/plain",
            It.IsAny<CancellationToken>()), Times.Once);

        _itemRepoMock.Verify(x => x.AddAsync(
            It.Is<DriveItem>(i => i.Name == "document.txt" && i.OwnerId == userId),
            It.IsAny<CancellationToken>()), Times.Once);

        _fileVersionRepoMock.Verify(x => x.AddAsync(
            It.Is<FileVersion>(v => v.MimeType == "text/plain" && v.IsCurrent),
            It.IsAny<CancellationToken>()), Times.Once);

        _itemRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
