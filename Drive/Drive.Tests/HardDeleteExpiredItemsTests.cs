using Drive.Application.Common.Interfaces;
using Drive.Application.Common.Models;
using Drive.Application.Features.DriveItems.Commands.HardDeleteExpiredItems;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Drive.Tests;

public class HardDeleteExpiredItemsTests
{
    private readonly Mock<IDriveItemRepository> _repoMock = new();
    private readonly Mock<IFileStorage> _fileStorageMock = new();
    private readonly Mock<ILogger<HardDeleteExpiredItemsCommandHandler>> _loggerMock = new();
    private readonly IOptions<TrashSettings> _trashSettingsOptions;
    private readonly HardDeleteExpiredItemsCommandHandler _handler;

    public HardDeleteExpiredItemsTests()
    {
        _trashSettingsOptions = Options.Create(new TrashSettings { RetentionDays = 30, CleanupIntervalHours = 24 });
        _handler = new HardDeleteExpiredItemsCommandHandler(
            _repoMock.Object,
            _fileStorageMock.Object,
            _trashSettingsOptions,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNoExpiredItems_ReturnsZero()
    {
        _repoMock.Setup(x => x.GetExpiredDeletedItemsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveItem>());

        var result = await _handler.Handle(new HardDeleteExpiredItemsCommand(), CancellationToken.None);

        Assert.Equal(0, result);
        _fileStorageMock.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(x => x.HardDeleteItemsAsync(It.IsAny<IEnumerable<DriveItem>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExpiredItemsExist_DeletesS3ObjectsAndPurgesDatabase()
    {
        var fileId = Guid.NewGuid();
        var s3Key = "files/" + fileId + "/v1";

        var expiredFile = new DriveItem
        {
            Id = fileId,
            OwnerId = Guid.NewGuid(),
            Name = "old_file.txt",
            ItemType = DriveItemType.File,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow.AddDays(-35),
            Versions = new List<FileVersion>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    DriveItemId = fileId,
                    S3Bucket = "test-bucket",
                    S3ObjectKey = s3Key,
                    VersionNumber = 1,
                    IsCurrent = true
                }
            }
        };

        var expiredFolder = new DriveItem
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "old_folder",
            ItemType = DriveItemType.Folder,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow.AddDays(-40)
        };

        var expiredItems = new List<DriveItem> { expiredFolder, expiredFile };

        _repoMock.Setup(x => x.GetExpiredDeletedItemsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredItems);

        var result = await _handler.Handle(new HardDeleteExpiredItemsCommand(), CancellationToken.None);

        Assert.Equal(2, result);
        _fileStorageMock.Verify(x => x.DeleteAsync(s3Key, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(x => x.HardDeleteItemsAsync(expiredItems, It.IsAny<CancellationToken>()), Times.Once);
    }
}
