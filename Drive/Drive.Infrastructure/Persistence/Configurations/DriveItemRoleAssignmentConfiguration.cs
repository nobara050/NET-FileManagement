using Drive.Domain.Entities;
using Drive.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drive.Infrastructure.Persistence.Configurations;

public class DriveItemRoleAssignmentConfiguration
    : IEntityTypeConfiguration<DriveItemRoleAssignment>
{
    public void Configure(
        EntityTypeBuilder<DriveItemRoleAssignment> builder)
    {
        builder.ToTable("drive_item_role_assignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.DriveItemId)
            .HasColumnName("drive_item_id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(x => x.SourceItemId)
            .HasColumnName("source_item_id");

        builder.Property(x => x.IsExplicit)
            .HasColumnName("is_explicit")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        // A user can have only one explicit role on the same item.
        builder.HasIndex(x => new
        {
            x.DriveItemId,
            x.UserId
        })
        .HasDatabaseName("UX_drive_item_role_assignments_item_user")
        .IsUnique();

        builder.HasIndex(x => x.DriveItemId)
            .HasDatabaseName("IX_drive_item_role_assignments_drive_item_id");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_drive_item_role_assignments_user_id");

        builder.HasIndex(x => x.RoleId)
            .HasDatabaseName("IX_drive_item_role_assignments_role_id");

        builder.HasIndex(x => x.SourceItemId)
            .HasDatabaseName("IX_drive_item_role_assignments_source_item_id");

        // Item receiving the assignment.
        builder.HasOne<DriveItem>()
            .WithMany()
            .HasForeignKey(x => x.DriveItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // User receiving the role.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // User who created the assignment.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Permission profile.
        builder.HasOne<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Item from which the assignment was inherited.
        builder.HasOne<DriveItem>()
            .WithMany()
            .HasForeignKey(x => x.SourceItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}