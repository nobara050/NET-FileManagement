using Drive.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drive.Infrastructure.Persistence.Configurations;

public class FileVersionConfiguration
    : IEntityTypeConfiguration<FileVersion>
{
    public void Configure(
        EntityTypeBuilder<FileVersion> builder)
    {
        builder.ToTable("file_versions", table =>
        {
            table.HasCheckConstraint(
                "CK_file_versions_version_number_positive",
                "version_number > 0");

            table.HasCheckConstraint(
                "CK_file_versions_size_non_negative",
                "size >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.DriveItemId)
            .HasColumnName("drive_item_id")
            .IsRequired();

        builder.Property(x => x.VersionNumber)
            .HasColumnName("version_number")
            .IsRequired();

        builder.Property(x => x.S3Bucket)
            .HasColumnName("s3_bucket")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.S3ObjectKey)
            .HasColumnName("s3_object_key")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(x => x.Size)
            .HasColumnName("size")
            .IsRequired();

        builder.Property(x => x.Checksum)
            .HasColumnName("checksum")
            .HasMaxLength(128);

        builder.Property(x => x.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.IsCurrent)
            .HasColumnName("is_current")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasOne(x => x.DriveItem)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.DriveItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.DriveItemId,
            x.VersionNumber
        })
        .HasDatabaseName("UX_file_versions_drive_item_version")
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.S3Bucket,
            x.S3ObjectKey
        })
        .HasDatabaseName("UX_file_versions_s3_object")
        .IsUnique();

        builder.HasIndex(x => x.DriveItemId)
            .HasDatabaseName("UX_file_versions_current")
            .IsUnique()
            .HasFilter("is_current = true");
    }
}