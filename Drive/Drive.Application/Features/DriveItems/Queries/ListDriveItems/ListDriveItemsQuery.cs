using Drive.Application.Common.Models;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Enums;
using MediatR;

namespace Drive.Application.Features.DriveItems.Queries.ListDriveItems;

public sealed record ListDriveItemsQuery(
    Guid? ParentId,
    string? SearchTerm,
    DriveItemType? ItemType,
    ListDriveItemsScope Scope = ListDriveItemsScope.Owned,
    int PageNumber = 1,
    int PageSize = 50)
    : IRequest<PagedResult<DriveItemResult>>;