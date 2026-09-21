using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Auth.Commands.UploadAvatar;
using Drive.Application.Features.DriveItems.Models;
using Drive.Application.Features.Users.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Drive.Tests;

public class UploadAvatarTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IUserManagementService> _userManagementServiceMock = new();
    private readonly Mock<IFileStorage> _fileStorageMock = new();
    private readonly Mock<ILogger<UploadAvatarCommandHandler>> _loggerMock = new();
    private readonly UploadAvatarCommandHandler _handler;

    public UploadAvatarTests()
    {
        _handler = new UploadAvatarCommandHandler(
            _currentUserServiceMock.Object,
            _userManagementServiceMock.Object,
            _fileStorageMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidImage_DeletesOldAvatarAndUploadsNew()
    {
        var userId = Guid.NewGuid();
        var oldAvatarUrl = "http://localhost:4566/drive-files/avatars/user/old-avatar.png";
        var newAvatarUrl = "http://localhost:4566/drive-files/avatars/user/new-avatar.png";

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _userManagementServiceMock.Setup(x => x.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResult { UserId = userId, Email = "user@test.com", DisplayName = "User", AvatarUrl = oldAvatarUrl });

        _fileStorageMock.Setup(x => x.GetFileUrl(It.IsAny<string>())).Returns(newAvatarUrl);
        _userManagementServiceMock.Setup(x => x.UpdateAvatarAsync(userId, newAvatarUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        using var memoryStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var fileUpload = new FileUpload
        {
            FileName = "avatar.png",
            ContentType = "image/png",
            Length = 3,
            Content = memoryStream
        };

        var result = await _handler.Handle(new UploadAvatarCommand(fileUpload), CancellationToken.None);

        Assert.Equal(newAvatarUrl, result);

        // Verified old avatar deletion
        _fileStorageMock.Verify(x => x.DeleteAsync("avatars/user/old-avatar.png", It.IsAny<CancellationToken>()), Times.Once);

        // Verified new avatar upload
        _fileStorageMock.Verify(x => x.UploadAsync(
            It.Is<string>(k => k.StartsWith($"avatars/{userId}/")),
            memoryStream,
            "image/png",
            It.IsAny<CancellationToken>()), Times.Once);

        // Verified user profile update
        _userManagementServiceMock.Verify(x => x.UpdateAvatarAsync(userId, newAvatarUrl, It.IsAny<CancellationToken>()), Times.Once);
    }
}
