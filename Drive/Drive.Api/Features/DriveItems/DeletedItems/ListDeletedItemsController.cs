using AutoMapper;
using Drive.Api.Features.DriveItems.List;
using Drive.Application.Features.DriveItems.Queries.ListDeletedItems;
using Drive.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.DriveItems.DeletedItems;

[Authorize]
[ApiController]
[Route("api/drive-items")]
public class ListDeletedItemsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public ListDeletedItemsController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [HttpGet("deleted")]
    public async Task<ActionResult<PagedListDriveItemsResponse>> ListDeleted(
        [FromQuery] string? searchTerm,
        [FromQuery] DriveItemType? itemType,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListDeletedItemsQuery(
                searchTerm,
                itemType,
                pageNumber,
                pageSize),
            cancellationToken);

        return Ok(_mapper.Map<PagedListDriveItemsResponse>(result));
    }
}
