using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnAd.Data.Users.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "subject",
                table: "client",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "subject",
                table: "client");
        }
    }
}
