using System.Security.Claims;
using Drive.Api.Authorization;
using Drive.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;

namespace Drive.Tests;

public class AdminAuthorizationHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly AdminAuthorizationHandler _handler;

    public AdminAuthorizationHandlerTests()
    {
        _handler = new AdminAuthorizationHandler(
            _currentUserServiceMock.Object,
            _identityServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasAdminRole_Succeeds()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _identityServiceMock.Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Admin" });

        var requirement = new AdminRequirement();
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("userId", userId.ToString()) }, "Bearer")),
            null);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_WhenUserLacksAdminRole_Fails()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _identityServiceMock.Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Viewer", "Editor" });

        var requirement = new AdminRequirement();
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("userId", userId.ToString()) }, "Bearer")),
            null);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_WhenUserUnauthenticated_Fails()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var requirement = new AdminRequirement();
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            new ClaimsPrincipal(),
            null);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}
