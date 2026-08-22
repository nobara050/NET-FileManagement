using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Drive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_file_versions_created_by",
                table: "file_versions",
                column: "created_by");

            migrationBuilder.AddForeignKey(
                name: "FK_drive_items_AspNetUsers_owner_id",
                table: "drive_items",
                column: "owner_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_file_versions_AspNetUsers_created_by",
                table: "file_versions",
                column: "created_by",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_drive_items_AspNetUsers_owner_id",
                table: "drive_items");

            migrationBuilder.DropForeignKey(
                name: "FK_file_versions_AspNetUsers_created_by",
                table: "file_versions");

            migrationBuilder.DropIndex(
                name: "IX_file_versions_created_by",
                table: "file_versions");
        }
    }
}
