using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nudges.Data.Users.Migrations
{
    /// <inheritdoc />
    public partial class Slugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "joined_date",
                table: "client",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "client",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "client_slug_key",
                table: "client",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_client_slug",
                table: "client",
                column: "slug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "client_slug_key",
                table: "client");

            migrationBuilder.DropIndex(
                name: "idx_client_slug",
                table: "client");

            migrationBuilder.DropColumn(
                name: "joined_date",
                table: "client");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "client");
        }
    }
}
