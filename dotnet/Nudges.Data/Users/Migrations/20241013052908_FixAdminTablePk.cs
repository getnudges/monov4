using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nudges.Data.Users.Migrations
{
    /// <inheritdoc />
    public partial class FixAdminTablePk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "admin",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "admin_pkey",
                table: "admin",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "admin_pkey",
                table: "admin");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "admin",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
