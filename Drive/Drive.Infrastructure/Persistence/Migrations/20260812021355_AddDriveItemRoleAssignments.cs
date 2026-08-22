using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Drive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDriveItemRoleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "drive_item_role_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    drive_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drive_item_role_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_drive_item_role_assignments_AspNetRoles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_drive_item_role_assignments_AspNetUsers_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_drive_item_role_assignments_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_drive_item_role_assignments_drive_items_drive_item_id",
                        column: x => x.drive_item_id,
                        principalTable: "drive_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_drive_item_role_assignments_created_by",
                table: "drive_item_role_assignments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_drive_item_role_assignments_drive_item_id",
                table: "drive_item_role_assignments",
                column: "drive_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_drive_item_role_assignments_role_id",
                table: "drive_item_role_assignments",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_drive_item_role_assignments_user_id",
                table: "drive_item_role_assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UX_drive_item_role_assignments_item_user",
                table: "drive_item_role_assignments",
                columns: new[] { "drive_item_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "drive_item_role_assignments");
        }
    }
}
