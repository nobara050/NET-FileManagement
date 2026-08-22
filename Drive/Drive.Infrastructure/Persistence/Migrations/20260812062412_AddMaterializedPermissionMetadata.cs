using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Drive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterializedPermissionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_explicit",
                table: "drive_item_role_assignments",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_item_id",
                table: "drive_item_role_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_drive_item_role_assignments_source_item_id",
                table: "drive_item_role_assignments",
                column: "source_item_id");

            migrationBuilder.AddForeignKey(
                name: "FK_drive_item_role_assignments_drive_items_source_item_id",
                table: "drive_item_role_assignments",
                column: "source_item_id",
                principalTable: "drive_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_drive_item_role_assignments_drive_items_source_item_id",
                table: "drive_item_role_assignments");

            migrationBuilder.DropIndex(
                name: "IX_drive_item_role_assignments_source_item_id",
                table: "drive_item_role_assignments");

            migrationBuilder.DropColumn(
                name: "is_explicit",
                table: "drive_item_role_assignments");

            migrationBuilder.DropColumn(
                name: "source_item_id",
                table: "drive_item_role_assignments");
        }
    }
}
