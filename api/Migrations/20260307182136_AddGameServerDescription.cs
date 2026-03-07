using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiratesQuest.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddGameServerDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "GameServers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "GameServers");
        }
    }
}
