using AutoMapper;
using Drive.Application.Common.Interfaces;
using Drive.Application.Common.Models;
using Drive.Application.Features.DriveItems.Models;
using MediatR;

namespace Drive.Application.Features.DriveItems.Queries.ListDeletedItems;

public sealed class ListDeletedItemsQueryHandler : IRequestHandler<ListDeletedItemsQuery, PagedResult<DriveItemResult>>
{
    private readonly IDriveItemRepository _driveItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public ListDeletedItemsQueryHandler(
        IDriveItemRepository driveItemRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _driveItemRepository = driveItemRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<PagedResult<DriveItemResult>> Handle(
        ListDeletedItemsQuery request,
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

        var totalCount = await _driveItemRepository.CountDeletedItemsAsync(
            userId.Value,
            request.SearchTerm,
            request.ItemType,
            cancellationToken);

        var items = await _driveItemRepository.ListDeletedItemsAsync(
            userId.Value,
            request.SearchTerm,
            request.ItemType,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var mappedResults = _mapper.Map<IReadOnlyList<DriveItemResult>>(items);

        return new PagedResult<DriveItemResult>
        {
            Items = mappedResults,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}
