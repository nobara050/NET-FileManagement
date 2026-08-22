namespace Drive.Api.Features.DriveItems.List;

public sealed class PagedListDriveItemsResponse
{
    public IReadOnlyList<DriveItemResponse> Items { get; init; }
        = Array.Empty<DriveItemResponse>();

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }
}