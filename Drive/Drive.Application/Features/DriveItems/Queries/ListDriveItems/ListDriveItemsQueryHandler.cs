using AutoMapper;
using Drive.Application.Common.Interfaces;
using Drive.Application.Common.Models;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using MediatR;
using System.Linq.Expressions;

namespace Drive.Application.Features.DriveItems.Queries.ListDriveItems;

public sealed class ListDriveItemsQueryHandler
    : IRequestHandler<ListDriveItemsQuery, PagedResult<DriveItemResult>>
{
    private const string ViewPermission = "View";
    private const string EditPermission = "Edit";

    private readonly IRepository<DriveItem> _repository;
    private readonly IDriveItemAccessQuery _driveItemAccessQuery;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public ListDriveItemsQueryHandler(
        IRepository<DriveItem> repository,
        IDriveItemAccessQuery driveItemAccessQuery,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _repository = repository;
        _driveItemAccessQuery = driveItemAccessQuery;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<PagedResult<DriveItemResult>> Handle(
        ListDriveItemsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            return new PagedResult<DriveItemResult>
            {
                Items = Array.Empty<DriveItemResult>(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        var searchTerm = request.SearchTerm?.Trim();

        if (request.Scope == ListDriveItemsScope.Accessible)
        {
            var viewItems = await _driveItemAccessQuery.ListAccessibleAsync(
                userId.Value,
                request.ParentId,
                searchTerm,
                request.ItemType,
                ViewPermission,
                cancellationToken);

            var editItems = await _driveItemAccessQuery.ListAccessibleAsync(
                userId.Value,
                request.ParentId,
                searchTerm,
                request.ItemType,
                EditPermission,
                cancellationToken);

            var accessibleItems = viewItems
                .Concat(editItems)
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .OrderByDescending(x => x.ItemType == DriveItemType.Folder)
                .ThenBy(x => x.Name)
                .ToList();

            var totalCount = accessibleItems.Count;

            var pagedAccessibleItems = accessibleItems
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var results = _mapper.Map<IReadOnlyList<DriveItemResult>>(
                pagedAccessibleItems);

            return new PagedResult<DriveItemResult>
            {
                Items = results,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(
                    totalCount / (double)request.PageSize)
            };
        }

        Expression<Func<DriveItem, bool>> predicate = x =>
            !x.IsDeleted &&
            x.OwnerId == userId.Value &&
            x.ParentId == request.ParentId &&
            (request.ItemType == null ||
             x.ItemType == request.ItemType.Value) &&
            (string.IsNullOrEmpty(searchTerm) ||
             x.Name.ToLower().Contains(searchTerm.ToLower()));

        var totalOwnedCount = await _repository.CountAsync(
            predicate,
            cancellationToken);

        var ownedItems = await _repository.ListAsync(
            predicate,
            cancellationToken);

        var orderedItems = ownedItems
            .OrderByDescending(x => x.ItemType == DriveItemType.Folder)
            .ThenBy(x => x.Name);

        var pagedItems = orderedItems
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var ownedResults = _mapper.Map<IReadOnlyList<DriveItemResult>>(
            pagedItems);

        return new PagedResult<DriveItemResult>
        {
            Items = ownedResults,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalOwnedCount,
            TotalPages = (int)Math.Ceiling(
                totalOwnedCount / (double)request.PageSize)
        };
    }
}