using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nudges.Data.Products.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePriceTierStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "price_tier",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'ACTIVE'::character varying",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldDefaultValueSql: "'INACTIVE'::character varying");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "price_tier",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValueSql: "'INACTIVE'::character varying",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValueSql: "'ACTIVE'::character varying");
        }
    }
}
