using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Auth.Commands.RefreshToken;
using Drive.Application.Features.Auth.Models;
using Moq;
using Xunit;

namespace Drive.Tests;

public class RefreshTokenTests
{
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenTests()
    {
        _handler = new RefreshTokenCommandHandler(
            _jwtTokenServiceMock.Object,
            _refreshTokenServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTokenInvalidOrExpired_ReturnsNull()
    {
        _refreshTokenServiceMock.Setup(x => x.RotateAsync("invalid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenRotationResult?)null);

        var result = await _handler.Handle(new RefreshTokenCommand("invalid-token"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenTokenValid_RevokesOldTokenAndIssuesNewTokens()
    {
        var rawToken = "valid-raw-token";
        var userId = Guid.NewGuid();
        var rotated = new RefreshTokenRotationResult
        {
            UserId = userId,
            RefreshToken = "new-refresh-token"
        };
        var newAccessToken = "new-jwt-access-token";

        _refreshTokenServiceMock.Setup(x => x.RotateAsync(rawToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rotated);

        _jwtTokenServiceMock.Setup(x => x.GenerateToken(userId))
            .Returns(newAccessToken);

        var result = await _handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(newAccessToken, result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);
    }
}
