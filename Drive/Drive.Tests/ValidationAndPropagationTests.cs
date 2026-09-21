using AutoMapper;
using Drive.Application;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Commands.CreateFile;
using Drive.Application.Features.DriveItems.Commands.CreateFolder;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Drive.Tests;

public class ValidationAndPropagationTests
{
    private readonly IMapper _mapper;

    public ValidationAndPropagationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        var sp = services.BuildServiceProvider();
        _mapper = sp.GetRequiredService<IMapper>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateFolderCommandValidator_WhenNameInvalid_FailsValidationWithoutNRE(string? name)
    {
        var validator = new CreateFolderCommandValidator();
        var command = new CreateFolderCommand(null, name!);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateFolderCommand.Name));
    }

    [Fact]
    public void CreateFileCommandValidator_WhenFileIsNull_FailsValidationWithoutNRE()
    {
        var validator = new CreateFileCommandValidator();
        var command = new CreateFileCommand(null, null!);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateFileCommand.File));
    }

    [Fact]
    public async Task CreateFolder_WhenParentHasRoleAssignments_PropagatesAssignmentsToFolder()
    {
        var repoMock = new Mock<IDriveItemRepository>();
        var assignmentRepoMock = new Mock<IDriveItemRoleAssignmentRepository>();
        var userServiceMock = new Mock<ICurrentUserService>();
        var permissionServiceMock = new Mock<IPermissionService>();

        var handler = new CreateFolderCommandHandler(
            repoMock.Object,
            assignmentRepoMock.Object,
            userServiceMock.Object,
            permissionServiceMock.Object,
            _mapper);

        var ownerId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        userServiceMock.Setup(x => x.UserId).Returns(ownerId);

        var parentFolder = new DriveItem
        {
            Id = parentId,
            OwnerId = ownerId,
            ItemType = DriveItemType.Folder,
            IsDeleted = false
        };

        repoMock.Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentFolder);

        repoMock.Setup(x => x.IsDuplicateNameAsync(ownerId, parentId, "SubFolder", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        assignmentRepoMock.Setup(x => x.ListByDriveItemIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveItemRoleAssignment>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    DriveItemId = parentId,
                    UserId = collaboratorId,
                    RoleId = roleId,
                    IsDirect = true
                }
            });

        var result = await handler.Handle(new CreateFolderCommand(parentId, "SubFolder"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("SubFolder", result.Name);

        assignmentRepoMock.Verify(x => x.AddAsync(
            It.Is<DriveItemRoleAssignment>(a =>
                a.DriveItemId == result.Id &&
                a.UserId == collaboratorId &&
                a.RoleId == roleId &&
                !a.IsDirect &&
                a.SourceItemId == parentId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFile_WhenParentHasRoleAssignments_PropagatesAssignmentsToFile()
    {
        var itemRepoMock = new Mock<IDriveItemRepository>();
        var assignmentRepoMock = new Mock<IDriveItemRoleAssignmentRepository>();
        var fileVersionRepoMock = new Mock<IRepository<FileVersion>>();
        var userServiceMock = new Mock<ICurrentUserService>();
        var permissionServiceMock = new Mock<IPermissionService>();
        var fileStorageMock = new Mock<IFileStorage>();

        var handler = new CreateFileCommandHandler(
            itemRepoMock.Object,
            assignmentRepoMock.Object,
            fileVersionRepoMock.Object,
            userServiceMock.Object,
            permissionServiceMock.Object,
            fileStorageMock.Object,
            _mapper);

        var ownerId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        userServiceMock.Setup(x => x.UserId).Returns(ownerId);

        var parentFolder = new DriveItem
        {
            Id = parentId,
            OwnerId = ownerId,
            ItemType = DriveItemType.Folder,
            IsDeleted = false
        };

        itemRepoMock.Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentFolder);

        itemRepoMock.Setup(x => x.IsDuplicateNameAsync(ownerId, parentId, "doc.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        assignmentRepoMock.Setup(x => x.ListByDriveItemIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveItemRoleAssignment>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    DriveItemId = parentId,
                    UserId = collaboratorId,
                    RoleId = roleId,
                    IsDirect = true
                }
            });

        fileStorageMock.Setup(x => x.BucketName).Returns("test-bucket");

        using var stream = new MemoryStream("data"u8.ToArray());
        var upload = new FileUpload
        {
            FileName = "doc.txt",
            ContentType = "text/plain",
            Length = stream.Length,
            Content = stream
        };

        var result = await handler.Handle(new CreateFileCommand(parentId, upload), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("doc.txt", result.Name);

        assignmentRepoMock.Verify(x => x.AddAsync(
            It.Is<DriveItemRoleAssignment>(a =>
                a.DriveItemId == result.Id &&
                a.UserId == collaboratorId &&
                a.RoleId == roleId &&
                !a.IsDirect &&
                a.SourceItemId == parentId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
