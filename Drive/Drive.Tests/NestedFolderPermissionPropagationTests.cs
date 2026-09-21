using AutoMapper;
using Drive.Application;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Commands.CreateFolder;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Drive.Tests;

public class NestedFolderPermissionPropagationTests
{
    private readonly IMapper _mapper;

    public NestedFolderPermissionPropagationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        var sp = services.BuildServiceProvider();
        _mapper = sp.GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task CreateFolder_Level3InheritsRootSourceItemId_FromLevel2Parent()
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
        var level1Id = Guid.NewGuid();
        var level2Id = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        userServiceMock.Setup(x => x.UserId).Returns(ownerId);

        var level2Folder = new DriveItem
        {
            Id = level2Id,
            ParentId = level1Id,
            OwnerId = ownerId,
            ItemType = DriveItemType.Folder,
            IsDeleted = false
        };

        repoMock.Setup(x => x.GetByIdAsync(level2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(level2Folder);

        repoMock.Setup(x => x.IsDuplicateNameAsync(ownerId, level2Id, "Level3_Folder", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Level 2 has an inherited assignment whose SourceItemId is Level 1
        assignmentRepoMock.Setup(x => x.ListByDriveItemIdAsync(level2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveItemRoleAssignment>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    DriveItemId = level2Id,
                    UserId = collaboratorId,
                    RoleId = roleId,
                    IsDirect = false,
                    SourceItemId = level1Id
                }
            });

        var result = await handler.Handle(new CreateFolderCommand(level2Id, "Level3_Folder"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Level3_Folder", result.Name);

        // Verify that Level 3's created assignment retains SourceItemId pointing to Level 1
        assignmentRepoMock.Verify(x => x.AddAsync(
            It.Is<DriveItemRoleAssignment>(a =>
                a.DriveItemId == result.Id &&
                a.UserId == collaboratorId &&
                a.RoleId == roleId &&
                !a.IsDirect &&
                a.SourceItemId == level1Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
