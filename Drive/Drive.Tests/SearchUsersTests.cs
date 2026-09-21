using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Users.Models;
using Drive.Application.Features.Users.Queries.SearchUsers;
using Moq;
using Xunit;

namespace Drive.Tests;

public class SearchUsersTests
{
    private readonly Mock<IUserManagementService> _userServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly SearchUsersQueryHandler _handler;

    public SearchUsersTests()
    {
        _handler = new SearchUsersQueryHandler(
            _userServiceMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_CallsSearchUsersAsyncWithCurrentUserId()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(currentUserId);

        var expectedUsers = new List<UserResult>
        {
            new() { UserId = Guid.NewGuid(), Email = "colleague@test.com", DisplayName = "Colleague" }
        };

        _userServiceMock.Setup(x => x.SearchUsersAsync("coll", 10, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUsers);

        var result = await _handler.Handle(new SearchUsersQuery("coll", 10), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("colleague@test.com", result.First().Email);
    }
}
