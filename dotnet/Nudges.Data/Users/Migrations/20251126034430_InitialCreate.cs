using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nudges.Data.Users.Migrations
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
                name: "user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValueSql: "'en-US'::character varying"),
                    subject = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    phone_number_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    phone_number_encrypted = table.Column<string>(type: "text", nullable: false),
                    joined_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("admin_pkey", x => x.id);
                    table.ForeignKey(
                        name: "admin_user_id_fkey",
                        column: x => x.id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying", nullable: false),
                    slug = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false, defaultValueSql: "''::character varying"),
                    customer_id = table.Column<string>(type: "character varying", nullable: true),
                    subscription_id = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_pkey", x => x.id);
                    table.ForeignKey(
                        name: "client_user_id_fkey",
                        column: x => x.id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscriber",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("subscriber_pkey", x => x.id);
                    table.ForeignKey(
                        name: "subscriber_user_id_fkey",
                        column: x => x.id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_subscriber",
                columns: table => new
                {
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscriber_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_subscriber_pkey", x => new { x.client_id, x.subscriber_id });
                    table.ForeignKey(
                        name: "client_subscriber_client_id_fkey",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "client_subscriber_subscriber_id_fkey",
                        column: x => x.subscriber_id,
                        principalTable: "subscriber",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_client_slug",
                table: "client",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_client_subscriber_subscriber_id",
                table: "client_subscriber",
                column: "subscriber_id");

            migrationBuilder.CreateIndex(
                name: "user_phone_number_hash_key",
                table: "user",
                column: "phone_number_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin");

            migrationBuilder.DropTable(
                name: "client_subscriber");

            migrationBuilder.DropTable(
                name: "client");

            migrationBuilder.DropTable(
                name: "subscriber");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
