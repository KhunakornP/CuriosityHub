using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentitiyService.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGoogleAuthAndProfileUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleAuths");

            migrationBuilder.DropColumn(
                name: "ProfileUrl",
                table: "Profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileUrl",
                table: "Profiles",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GoogleAuths",
                columns: table => new
                {
                    GoogleId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAuths", x => x.GoogleId);
                    table.ForeignKey(
                        name: "FK_GoogleAuths_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAuths_UserId",
                table: "GoogleAuths",
                column: "UserId",
                unique: true);
        }
    }
}
