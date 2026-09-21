using Drive.Domain.Entities;
using Drive.Domain.Enums;

namespace Drive.Application.Common.Interfaces;

public interface IDriveItemAccessQuery
{
    Task<IReadOnlyList<DriveItem>> ListAccessibleAsync(
        Guid userId,
        Guid? parentId,
        string? searchTerm,
        DriveItemType? itemType,
        string permission,
        int pageNumber = 0,
        int pageSize = 0,
        CancellationToken cancellationToken = default);

    Task<int> CountAccessibleAsync(
        Guid userId,
        Guid? parentId,
        string? searchTerm,
        DriveItemType? itemType,
        string permission,
        CancellationToken cancellationToken = default);
}