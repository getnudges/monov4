using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UnAd.Data.Products.Migrations
{
    /// <inheritdoc />
    public partial class PriceTierFeatureOneToOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "price_tier_features_pkey",
                table: "price_tier_features");

            migrationBuilder.DropIndex(
                name: "IX_price_tier_features_price_tier_id",
                table: "price_tier_features");

            migrationBuilder.DropColumn(
                name: "id",
                table: "price_tier_features");

            migrationBuilder.AlterColumn<int>(
                name: "price_tier_id",
                table: "price_tier_features",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "price_tier_features_pkey",
                table: "price_tier_features",
                column: "price_tier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "price_tier_features_pkey",
                table: "price_tier_features");

            migrationBuilder.AlterColumn<int>(
                name: "price_tier_id",
                table: "price_tier_features",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "price_tier_features",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "price_tier_features_pkey",
                table: "price_tier_features",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_price_tier_features_price_tier_id",
                table: "price_tier_features",
                column: "price_tier_id");
        }
    }
}
