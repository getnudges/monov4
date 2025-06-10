using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nudges.Data.Products.Migrations
{
    /// <inheritdoc />
    public partial class PriceTierFeaturesRemoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "price_tier_features");

            migrationBuilder.CreateTable(
                name: "plan_features",
                columns: table => new
                {
                    plan_id = table.Column<int>(type: "integer", nullable: false),
                    max_messages = table.Column<int>(type: "integer", nullable: true),
                    support_tier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ai_support = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("plan_features_pkey", x => x.plan_id);
                    table.ForeignKey(
                        name: "plan_features_plan_id_fkey",
                        column: x => x.plan_id,
                        principalTable: "plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_features");

            migrationBuilder.CreateTable(
                name: "price_tier_features",
                columns: table => new
                {
                    price_tier_id = table.Column<int>(type: "integer", nullable: false),
                    ai_support = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    max_messages = table.Column<int>(type: "integer", nullable: true),
                    support_tier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("price_tier_features_pkey", x => x.price_tier_id);
                    table.ForeignKey(
                        name: "price_tier_features_price_tier_id_fkey",
                        column: x => x.price_tier_id,
                        principalTable: "price_tier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
