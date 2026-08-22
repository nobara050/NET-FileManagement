using AutoMapper;
using Drive.Application.Common.Models;
using Drive.Application.Features.DriveItems;
using Drive.Application.Features.DriveItems.Queries.ListDriveItems;
using Drive.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.DriveItems.List;

[Authorize]
[ApiController]
[Route("api/drive-items")]
public sealed class ListDriveItemsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public ListDriveItemsController(
        ISender sender,
        IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<PagedListDriveItemsResponse>> List(
        [FromQuery] Guid? parentId,
        [FromQuery] string? searchTerm,
        [FromQuery] DriveItemType? itemType,
        [FromQuery] ListDriveItemsScope scope = ListDriveItemsScope.Owned,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListDriveItemsQuery(
                parentId,
                searchTerm,
                itemType,
                scope),
            cancellationToken);

        return Ok(
            _mapper.Map<PagedListDriveItemsResponse>(result));
    }
}