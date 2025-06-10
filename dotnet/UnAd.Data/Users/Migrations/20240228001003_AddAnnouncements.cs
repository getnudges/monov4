using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnAd.Data.Users.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "client",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying", nullable: false),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    customer_id = table.Column<string>(type: "character varying", nullable: true),
                    subscription_id = table.Column<string>(type: "character varying", nullable: true),
                    locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValueSql: "'en-US'::character varying")
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscriber",
                columns: table => new
                {
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    joined_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValueSql: "'en-US'::character varying")
                },
                constraints: table =>
                {
                    table.PrimaryKey("subscriber_pkey", x => x.phone_number);
                });

            migrationBuilder.CreateTable(
                name: "announcement",
                columns: table => new
                {
                    message_sid = table.Column<string>(type: "character(34)", fixedLength: true, maxLength: 34, nullable: false),
                    sent_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("announcement_pkey", x => x.message_sid);
                    table.ForeignKey(
                        name: "announcement_client_id_fkey",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_subscriber",
                columns: table => new
                {
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscriber_phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_subscriber_pkey", x => new { x.client_id, x.subscriber_phone_number });
                    table.ForeignKey(
                        name: "client_subscriber_client_id_fkey",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "client_subscriber_subscriber_phone_number_fkey",
                        column: x => x.subscriber_phone_number,
                        principalTable: "subscriber",
                        principalColumn: "phone_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_announcement_client_id",
                table: "announcement",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "client_phone_number_key",
                table: "client",
                column: "phone_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_client_phone_number",
                table: "client",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "IX_client_subscriber_subscriber_phone_number",
                table: "client_subscriber",
                column: "subscriber_phone_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "announcement");

            migrationBuilder.DropTable(
                name: "client_subscriber");

            migrationBuilder.DropTable(
                name: "client");

            migrationBuilder.DropTable(
                name: "subscriber");
        }
    }
}
