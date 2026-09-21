using Drive.Application.Common.Models;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Enums;
using MediatR;

namespace Drive.Application.Features.DriveItems.Queries.ListDeletedItems;

public sealed record ListDeletedItemsQuery(
    string? SearchTerm = null,
    DriveItemType? ItemType = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<DriveItemResult>>;
