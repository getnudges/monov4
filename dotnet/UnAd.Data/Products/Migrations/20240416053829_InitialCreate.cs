using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UnAd.Data.Products.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "plan",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    icon_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("plan_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "price_tier",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plan_id = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    icon_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("price_tier_pkey", x => x.id);
                    table.ForeignKey(
                        name: "price_tier_plan_id_fkey",
                        column: x => x.plan_id,
                        principalTable: "plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "discount_code",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    price_tier_id = table.Column<int>(type: "integer", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("discount_code_pkey", x => x.id);
                    table.ForeignKey(
                        name: "discount_code_price_tier_id_fkey",
                        column: x => x.price_tier_id,
                        principalTable: "price_tier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_subscription",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    price_tier_id = table.Column<int>(type: "integer", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValueSql: "'INACTIVE'::character varying"),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_confirmation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("plan_subscription_pkey", x => x.id);
                    table.ForeignKey(
                        name: "plan_subscription_price_tier_id_fkey",
                        column: x => x.price_tier_id,
                        principalTable: "price_tier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "price_tier_features",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    price_tier_id = table.Column<int>(type: "integer", nullable: true),
                    max_messages = table.Column<int>(type: "integer", nullable: true),
                    support_tier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ai_support = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("price_tier_features_pkey", x => x.id);
                    table.ForeignKey(
                        name: "price_tier_features_price_tier_id_fkey",
                        column: x => x.price_tier_id,
                        principalTable: "price_tier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trial_offer",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    price_tier_id = table.Column<int>(type: "integer", nullable: true),
                    duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("trial_offer_pkey", x => x.id);
                    table.ForeignKey(
                        name: "trial_offer_price_tier_id_fkey",
                        column: x => x.price_tier_id,
                        principalTable: "price_tier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "discount",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    discount_code_id = table.Column<int>(type: "integer", nullable: true),
                    plan_subscription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("discount_pkey", x => x.id);
                    table.ForeignKey(
                        name: "discount_discount_code_id_fkey",
                        column: x => x.discount_code_id,
                        principalTable: "discount_code",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "discount_plan_subscription_id_fkey",
                        column: x => x.plan_subscription_id,
                        principalTable: "plan_subscription",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trial",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    trail_offer_id = table.Column<int>(type: "integer", nullable: true),
                    plan_subscription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("trial_pkey", x => x.id);
                    table.ForeignKey(
                        name: "trial_plan_subscription_id_fkey",
                        column: x => x.plan_subscription_id,
                        principalTable: "plan_subscription",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "trial_trail_offer_id_fkey",
                        column: x => x.trail_offer_id,
                        principalTable: "trial_offer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_discount_discount_code_id",
                table: "discount",
                column: "discount_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_plan_subscription_id",
                table: "discount",
                column: "plan_subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_code_price_tier_id",
                table: "discount_code",
                column: "price_tier_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_subscription_price_tier_id",
                table: "plan_subscription",
                column: "price_tier_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_tier_plan_id",
                table: "price_tier",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_tier_features_price_tier_id",
                table: "price_tier_features",
                column: "price_tier_id");

            migrationBuilder.CreateIndex(
                name: "IX_trial_plan_subscription_id",
                table: "trial",
                column: "plan_subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_trial_trail_offer_id",
                table: "trial",
                column: "trail_offer_id");

            migrationBuilder.CreateIndex(
                name: "IX_trial_offer_price_tier_id",
                table: "trial_offer",
                column: "price_tier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discount");

            migrationBuilder.DropTable(
                name: "price_tier_features");

            migrationBuilder.DropTable(
                name: "trial");

            migrationBuilder.DropTable(
                name: "discount_code");

            migrationBuilder.DropTable(
                name: "plan_subscription");

            migrationBuilder.DropTable(
                name: "trial_offer");

            migrationBuilder.DropTable(
                name: "price_tier");

            migrationBuilder.DropTable(
                name: "plan");
        }
    }
}
