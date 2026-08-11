using Drive.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drive.Infrastructure.Persistence.Configurations;

public class DriveItemConfiguration
    : IEntityTypeConfiguration<DriveItem>
{
    public void Configure(
        EntityTypeBuilder<DriveItem> builder)
    {
        builder.ToTable("drive_items", table =>
        {
            table.HasCheckConstraint(
                "CK_drive_items_name_not_empty",
                "length(trim(name)) > 0");

            table.HasCheckConstraint(
                "CK_drive_items_file_metadata",
                """
                (
                    item_type = 'File'
                    AND mime_type IS NOT NULL
                    AND size >= 0
                )
                OR
                (
                    item_type = 'Folder'
                    AND mime_type IS NULL
                    AND size IS NULL
                )
                """);
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(x => x.ParentId)
            .HasColumnName("parent_id");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        // Shadow property used for case-insensitive unique indexes.
        // PostgreSQL will store LOWER(name) in this generated column.
        builder.Property<string>("NormalizedName")
            .HasColumnName("normalized_name")
            .HasComputedColumnSql(
                "LOWER(name)",
                stored: true)
            .HasMaxLength(255);

        builder.Property(x => x.ItemType)
            .HasColumnName("item_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(255);

        builder.Property(x => x.Size)
            .HasColumnName("size");

        builder.Property(x => x.Checksum)
            .HasColumnName("checksum")
            .HasMaxLength(128);

        builder.Property(x => x.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamptz");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Normal lookup indexes
        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_drive_items_name");

        builder.HasIndex(x => new
        {
            x.ParentId,
            x.Name
        })
        .HasDatabaseName("IX_drive_items_parent_id_name");

        // Active item indexes
        builder.HasIndex(x => x.ParentId)
            .HasDatabaseName("IX_drive_items_active_parent_id")
            .HasFilter("is_deleted = false");

        builder.HasIndex(x => x.OwnerId)
            .HasDatabaseName("IX_drive_items_active_owner_id")
            .HasFilter("is_deleted = false");

        // Root-level active name uniqueness:
        // OwnerId + LOWER(Name)
        // Only applies when ParentId IS NULL.
        builder.HasIndex(
            "OwnerId",
            "NormalizedName")
            .HasDatabaseName("UX_drive_items_root_owner_name_active")
            .IsUnique()
            .HasFilter(
                "parent_id IS NULL AND is_deleted = false");

        // Child-level active name uniqueness:
        // ParentId + LOWER(Name)
        // Only applies when ParentId IS NOT NULL.
        builder.HasIndex(
            "ParentId",
            "NormalizedName")
            .HasDatabaseName("UX_drive_items_parent_name_active")
            .IsUnique()
            .HasFilter(
                "parent_id IS NOT NULL AND is_deleted = false");

        // Soft-delete global query filter.
        // Normal queries never return deleted items.
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}