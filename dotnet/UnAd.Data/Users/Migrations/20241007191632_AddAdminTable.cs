using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnAd.Data.Users.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "client",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValueSql: "''::character varying",
                oldClrType: typeof(string),
                oldType: "character varying(12)",
                oldMaxLength: 12);

            migrationBuilder.CreateTable(
                name: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "admin_id_fkey",
                        column: x => x.id,
                        principalTable: "client",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_id",
                table: "admin",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin");

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "client",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(12)",
                oldMaxLength: 12,
                oldDefaultValueSql: "''::character varying");
        }
    }
}
