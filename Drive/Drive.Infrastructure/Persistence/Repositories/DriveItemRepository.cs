using Drive.Application.Common.Interfaces;
using Drive.Domain.Entities;
using Drive.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Persistence.Repositories;

public class DriveItemRepository : Repository<DriveItem>, IDriveItemRepository
{
    public DriveItemRepository(DriveDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<DriveItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(GetByIdAsync));
        return await DbContext.DriveItems
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<DriveItem?> GetWithVersionsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(GetWithVersionsAsync));
        return await DbContext.DriveItems
            .Include(x => x.Versions)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<bool> IsDuplicateNameAsync(
        Guid ownerId,
        Guid? parentId,
        string name,
        CancellationToken cancellationToken = default)
    {
        return await IsDuplicateNameAsync(ownerId, parentId, name, null, cancellationToken);
    }

    public async Task<bool> IsDuplicateNameAsync(
        Guid ownerId,
        Guid? parentId,
        string name,
        Guid? excludeItemId,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(IsDuplicateNameAsync));
        var normalizedName = name.ToLower();

        if (parentId.HasValue)
        {
            return await DbContext.DriveItems
                .AnyAsync(
                    x => !x.IsDeleted &&
                         x.ParentId == parentId.Value &&
                         (!excludeItemId.HasValue || x.Id != excludeItemId.Value) &&
                         x.Name.ToLower() == normalizedName,
                    cancellationToken);
        }

        return await DbContext.DriveItems
            .AnyAsync(
                x => !x.IsDeleted &&
                     x.ParentId == null &&
                     x.OwnerId == ownerId &&
                     (!excludeItemId.HasValue || x.Id != excludeItemId.Value) &&
                     x.Name.ToLower() == normalizedName,
                cancellationToken);
    }

    public async Task<IReadOnlyList<DriveItem>> ListFolderItemsAsync(
        Guid? parentId,
        Guid ownerId,
        DriveItemType? itemType,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(ListFolderItemsAsync));
        var query = DbContext.DriveItems.Where(x => !x.IsDeleted);

        if (parentId.HasValue)
        {
            query = query.Where(x => x.ParentId == parentId.Value);
        }
        else
        {
            query = query.Where(x => x.ParentId == null && x.OwnerId == ownerId);
        }

        if (itemType.HasValue)
        {
            query = query.Where(x => x.ItemType == itemType.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term));
        }

        query = query
            .OrderByDescending(x => x.ItemType == DriveItemType.Folder)
            .ThenBy(x => x.Name);

        if (pageNumber > 0 && pageSize > 0)
        {
            query = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> CountFolderItemsAsync(
        Guid? parentId,
        Guid ownerId,
        DriveItemType? itemType,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(CountFolderItemsAsync));
        var query = DbContext.DriveItems.Where(x => !x.IsDeleted);

        if (parentId.HasValue)
        {
            query = query.Where(x => x.ParentId == parentId.Value);
        }
        else
        {
            query = query.Where(x => x.ParentId == null && x.OwnerId == ownerId);
        }

        if (itemType.HasValue)
        {
            query = query.Where(x => x.ItemType == itemType.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<DriveItem?> GetDeletedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(GetDeletedByIdAsync));
        return await DbContext.DriveItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<DriveItem>> GetActiveDescendantsAsync(
        Guid parentId,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(GetActiveDescendantsAsync));
        var result = new List<DriveItem>();
        var queue = new Queue<Guid>();
        queue.Enqueue(parentId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            var children = await DbContext.DriveItems
                .Where(x => x.ParentId == currentId && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var child in children)
            {
                result.Add(child);
                if (child.ItemType == DriveItemType.Folder)
                {
                    queue.Enqueue(child.Id);
                }
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<DriveItem>> GetCascadedDeletedDescendantsAsync(
        Guid parentId,
        DateTimeOffset deletedAt,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(GetCascadedDeletedDescendantsAsync));
        var result = new List<DriveItem>();
        var queue = new Queue<Guid>();
        queue.Enqueue(parentId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            var children = await DbContext.DriveItems
                .IgnoreQueryFilters()
                .Where(x => x.ParentId == currentId && x.IsDeleted && x.DeletedAt == deletedAt)
                .ToListAsync(cancellationToken);

            foreach (var child in children)
            {
                result.Add(child);
                if (child.ItemType == DriveItemType.Folder)
                {
                    queue.Enqueue(child.Id);
                }
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<DriveItem>> ListDeletedItemsAsync(
        Guid ownerId,
        string? searchTerm,
        DriveItemType? itemType,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(ListDeletedItemsAsync));
        var query = DbContext.DriveItems
            .IgnoreQueryFilters()
            .Where(x => x.OwnerId == ownerId && x.IsDeleted);

        if (itemType.HasValue)
        {
            query = query.Where(x => x.ItemType == itemType.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term));
        }

        query = query.OrderByDescending(x => x.DeletedAt);

        if (pageNumber > 0 && pageSize > 0)
        {
            query = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> CountDeletedItemsAsync(
        Guid ownerId,
        string? searchTerm,
        DriveItemType? itemType,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(CountDeletedItemsAsync));
        var query = DbContext.DriveItems
            .IgnoreQueryFilters()
            .Where(x => x.OwnerId == ownerId && x.IsDeleted);

        if (itemType.HasValue)
        {
            query = query.Where(x => x.ItemType == itemType.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DriveItem>> GetDeletedDescendantsAsync(
        Guid parentId,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(GetDeletedDescendantsAsync));
        var result = new List<DriveItem>();
        var queue = new Queue<Guid>();
        queue.Enqueue(parentId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            var children = await DbContext.DriveItems
                .IgnoreQueryFilters()
                .Include(x => x.Versions)
                .Where(x => x.ParentId == currentId && x.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var child in children)
            {
                result.Add(child);
                if (child.ItemType == DriveItemType.Folder)
                {
                    queue.Enqueue(child.Id);
                }
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<DriveItem>> GetExpiredDeletedItemsAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(GetExpiredDeletedItemsAsync));
        return await DbContext.DriveItems
            .IgnoreQueryFilters()
            .Include(x => x.Versions)
            .Where(x => x.IsDeleted && x.DeletedAt != null && x.DeletedAt <= cutoff)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DriveItem>> GetUserDeletedItemsAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(GetUserDeletedItemsAsync));
        return await DbContext.DriveItems
            .IgnoreQueryFilters()
            .Include(x => x.Versions)
            .Where(x => x.OwnerId == ownerId && x.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task HardDeleteItemsAsync(
        IEnumerable<DriveItem> items,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(HardDeleteItemsAsync));
        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            return;
        }

        // Topologically sort items leaf-to-root so that children are removed before parents.
        // This satisfies PostgreSQL's DeleteBehavior.Restrict on ParentId.
        var remaining = new HashSet<DriveItem>(itemList);
        var orderedForDeletion = new List<DriveItem>();

        while (remaining.Count > 0)
        {
            var remainingIds = new HashSet<Guid>(remaining.Select(x => x.Id));

            // A leaf item is one whose Id is NOT a ParentId of any other remaining item.
            var leaves = remaining
                .Where(item => !remaining.Any(other => other.ParentId == item.Id))
                .ToList();

            if (leaves.Count == 0)
            {
                // Fallback in case of unexpected circular reference (should never happen in tree)
                leaves.AddRange(remaining);
            }

            foreach (var leaf in leaves)
            {
                orderedForDeletion.Add(leaf);
                remaining.Remove(leaf);
            }
        }

        foreach (var item in orderedForDeletion)
        {
            DbContext.DriveItems.Remove(item);
        }

        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
