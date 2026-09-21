using AutoMapper;
using Drive.Application.Common.Interfaces;
using Drive.Application.Features.DriveItems.Queries.ListDeletedItems;
using Drive.Application.Mapping;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Moq;

using Microsoft.Extensions.DependencyInjection;
using Drive.Application;

namespace Drive.Tests;

public class ListDeletedItemsTests
{
    private readonly Mock<IDriveItemRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _userServiceMock = new();
    private readonly IMapper _mapper;
    private readonly ListDeletedItemsQueryHandler _handler;

    public ListDeletedItemsTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        var sp = services.BuildServiceProvider();
        _mapper = sp.GetRequiredService<IMapper>();
        _handler = new ListDeletedItemsQueryHandler(_repoMock.Object, _userServiceMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_WhenCallerNotAuthenticated_ReturnsEmptyPagedResult()
    {
        _userServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await _handler.Handle(new ListDeletedItemsQuery(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Handle_WhenDeletedItemsExist_ReturnsPagedResults()
    {
        var userId = Guid.NewGuid();
        var deletedFile = new DriveItem
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Name = "deleted_document.pdf",
            ItemType = DriveItemType.File,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId);
        _repoMock.Setup(x => x.CountDeletedItemsAsync(userId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repoMock.Setup(x => x.ListDeletedItemsAsync(userId, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriveItem> { deletedFile });

        var result = await _handler.Handle(new ListDeletedItemsQuery(), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(deletedFile.Name, result.Items[0].Name);
        Assert.Equal(1, result.TotalCount);
    }
}
