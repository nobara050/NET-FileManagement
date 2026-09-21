using Drive.Domain.Entities;
using Drive.Domain.Enums;

namespace Drive.Application.Common.Interfaces;

public interface IDriveItemRepository : IRepository<DriveItem>
{
    Task<DriveItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> IsDuplicateNameAsync(
        Guid ownerId,
        Guid? parentId,
        string name,
        CancellationToken cancellationToken = default);

    Task<bool> IsDuplicateNameAsync(
        Guid ownerId,
        Guid? parentId,
        string name,
        Guid? excludeItemId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriveItem>> ListFolderItemsAsync(
        Guid? parentId,
        Guid ownerId,
        DriveItemType? itemType,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountFolderItemsAsync(
        Guid? parentId,
        Guid ownerId,
        DriveItemType? itemType,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<DriveItem?> GetDeletedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DriveItem?> GetWithVersionsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriveItem>> GetActiveDescendantsAsync(
        Guid parentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriveItem>> GetDeletedDescendantsAsync(
        Guid parentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriveItem>> GetCascadedDeletedDescendantsAsync(
        Guid parentId,
        DateTimeOffset deletedAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriveItem>> ListDeletedItemsAsync(
        Guid ownerId,
        string? searchTerm,
        DriveItemType? itemType,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountDeletedItemsAsync(
        Guid ownerId,
        string? searchTerm,
        DriveItemType? itemType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriveItem>> GetExpiredDeletedItemsAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriveItem>> GetUserDeletedItemsAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task HardDeleteItemsAsync(
        IEnumerable<DriveItem> items,
        CancellationToken cancellationToken = default);
}
