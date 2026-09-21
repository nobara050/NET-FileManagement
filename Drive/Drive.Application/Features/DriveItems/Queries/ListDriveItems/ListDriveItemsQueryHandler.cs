using AutoMapper;
using Drive.Application.Common.Authorization;
using Drive.Application.Common.Exceptions;
using Drive.Application.Common.Interfaces;
using Drive.Application.Common.Models;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using MediatR;

namespace Drive.Application.Features.DriveItems.Queries.ListDriveItems;

public sealed class ListDriveItemsQueryHandler
    : IRequestHandler<ListDriveItemsQuery, PagedResult<DriveItemResult>>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly ISharedDriveItemQuery _sharedDriveItemQuery;
    private readonly IDriveItemAccessQuery _driveItemAccessQuery;
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public ListDriveItemsQueryHandler(
        IDriveItemRepository driveItemRepository,
        ISharedDriveItemQuery sharedDriveItemQuery,
        IDriveItemAccessQuery driveItemAccessQuery,
        IPermissionService permissionService,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _driveItemRepository = driveItemRepository;
        _sharedDriveItemQuery = sharedDriveItemQuery;
        _driveItemAccessQuery = driveItemAccessQuery;
        _permissionService = permissionService;
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

        // 1. Browsing inside a folder (ParentId is specified)
        if (request.ParentId.HasValue)
        {
            var parent = await _driveItemRepository.GetByIdAsync(
                request.ParentId.Value,
                cancellationToken);

            if (parent is null || parent.IsDeleted)
            {
                throw new NotFoundException("Folder not found.");
            }

            var canRead = parent.OwnerId == userId.Value ||
                await _permissionService.HasPermissionAsync(
                    userId.Value,
                    parent.Id,
                    Permissions.DriveRead,
                    cancellationToken);

            if (!canRead)
            {
                throw new ForbiddenException("You do not have permission to view this folder.");
            }

            IReadOnlyList<DriveItem> items;
            int totalCount;

            if (parent.OwnerId == userId.Value)
            {
                // Owner browsing owned folder: fetch all owned items in folder
                items = await _driveItemRepository.ListFolderItemsAsync(
                    request.ParentId.Value,
                    parent.OwnerId,
                    request.ItemType,
                    searchTerm,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                totalCount = await _driveItemRepository.CountFolderItemsAsync(
                    request.ParentId.Value,
                    parent.OwnerId,
                    request.ItemType,
                    searchTerm,
                    cancellationToken);
            }
            else
            {
                // Non-owner browsing shared folder: only return items where caller has drive.read permission
                items = await _driveItemAccessQuery.ListAccessibleAsync(
                    userId.Value,
                    request.ParentId.Value,
                    searchTerm,
                    request.ItemType,
                    Permissions.DriveRead,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                totalCount = await _driveItemAccessQuery.CountAccessibleAsync(
                    userId.Value,
                    request.ParentId.Value,
                    searchTerm,
                    request.ItemType,
                    Permissions.DriveRead,
                    cancellationToken);
            }

            return new PagedResult<DriveItemResult>
            {
                Items = _mapper.Map<IReadOnlyList<DriveItemResult>>(items),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }

        // 2. Browsing at root level with Scope == Shared
        if (request.Scope == ListDriveItemsScope.Shared)
        {
            var sharedItems = await _sharedDriveItemQuery.ListSharedAsync(
                userId.Value,
                request.ItemType,
                searchTerm,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var totalSharedCount = await _sharedDriveItemQuery.CountSharedAsync(
                userId.Value,
                request.ItemType,
                searchTerm,
                cancellationToken);

            return new PagedResult<DriveItemResult>
            {
                Items = _mapper.Map<IReadOnlyList<DriveItemResult>>(sharedItems),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalSharedCount,
                TotalPages = (int)Math.Ceiling(totalSharedCount / (double)request.PageSize)
            };
        }

        // 3. Browsing at root level with Scope == All
        if (request.Scope == ListDriveItemsScope.All)
        {
            var ownedItems = await _driveItemRepository.ListFolderItemsAsync(
                null,
                userId.Value,
                request.ItemType,
                searchTerm,
                0,
                0,
                cancellationToken);

            var sharedItems = await _sharedDriveItemQuery.ListSharedAsync(
                userId.Value,
                request.ItemType,
                searchTerm,
                0,
                0,
                cancellationToken);

            var combined = ownedItems
                .Concat(sharedItems)
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .OrderByDescending(x => x.ItemType == DriveItemType.Folder)
                .ThenBy(x => x.Name)
                .ToList();

            var totalCombined = combined.Count;
            var pagedCombined = combined
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<DriveItemResult>
            {
                Items = _mapper.Map<IReadOnlyList<DriveItemResult>>(pagedCombined),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCombined,
                TotalPages = (int)Math.Ceiling(totalCombined / (double)request.PageSize)
            };
        }

        // 4. Default: Browsing at root level with Scope == Owned
        var ownedRootItems = await _driveItemRepository.ListFolderItemsAsync(
            null,
            userId.Value,
            request.ItemType,
            searchTerm,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var totalOwnedRootCount = await _driveItemRepository.CountFolderItemsAsync(
            null,
            userId.Value,
            request.ItemType,
            searchTerm,
            cancellationToken);

        return new PagedResult<DriveItemResult>
        {
            Items = _mapper.Map<IReadOnlyList<DriveItemResult>>(ownedRootItems),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalOwnedRootCount,
            TotalPages = (int)Math.Ceiling(totalOwnedRootCount / (double)request.PageSize)
        };
    }
}