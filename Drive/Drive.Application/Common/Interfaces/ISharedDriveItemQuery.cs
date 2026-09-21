using Drive.Domain.Entities;
using Drive.Domain.Enums;

namespace Drive.Application.Common.Interfaces;

public interface ISharedDriveItemQuery
{
    Task<IReadOnlyList<DriveItem>> ListSharedAsync(
        Guid userId,
        DriveItemType? itemType,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountSharedAsync(
        Guid userId,
        DriveItemType? itemType,
        string? searchTerm,
        CancellationToken cancellationToken = default);
}
