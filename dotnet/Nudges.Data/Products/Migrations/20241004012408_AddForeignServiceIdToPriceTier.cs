using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nudges.Data.Products.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignServiceIdToPriceTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "foreign_service_id",
                table: "price_tier",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "foreign_service_id",
                table: "price_tier");
        }
    }
}
