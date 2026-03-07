using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiratesQuest.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddGameServerRuntimeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlayerCount",
                table: "GameServers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlayerMax",
                table: "GameServers",
                type: "integer",
                nullable: false,
                defaultValue: 8);

            migrationBuilder.AddColumn<string>(
                name: "ServerVersion",
                table: "GameServers",
                type: "text",
                nullable: false,
                defaultValue: "unknown");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayerCount",
                table: "GameServers");

            migrationBuilder.DropColumn(
                name: "PlayerMax",
                table: "GameServers");

            migrationBuilder.DropColumn(
                name: "ServerVersion",
                table: "GameServers");
        }
    }
}
