using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiratesQuest.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddGameServerLastSeenUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenUtc",
                table: "GameServers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeenUtc",
                table: "GameServers");
        }
    }
}
