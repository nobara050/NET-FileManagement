using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Drive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnsureActivePartialUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"UX_drive_items_parent_name_active\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"UX_drive_items_root_owner_name_active\";");

            migrationBuilder.CreateIndex(
                name: "UX_drive_items_parent_name_active",
                table: "drive_items",
                columns: new[] { "parent_id", "normalized_name" },
                unique: true,
                filter: "parent_id IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "UX_drive_items_root_owner_name_active",
                table: "drive_items",
                columns: new[] { "owner_id", "normalized_name" },
                unique: true,
                filter: "parent_id IS NULL AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_drive_items_parent_name_active",
                table: "drive_items");

            migrationBuilder.DropIndex(
                name: "UX_drive_items_root_owner_name_active",
                table: "drive_items");
        }
    }
}
