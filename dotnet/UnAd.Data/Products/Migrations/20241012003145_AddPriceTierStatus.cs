using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnAd.Data.Products.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceTierStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "price_tier",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValueSql: "'INACTIVE'::character varying");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                table: "price_tier");
        }
    }
}
