using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PiratesQuest.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaderboardTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaderboardEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    InventoryGold = table.Column<int>(type: "integer", nullable: false),
                    VaultGold = table.Column<int>(type: "integer", nullable: false),
                    TotalGold = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaderboardEntries_GameServers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "GameServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_ServerId_TotalGold",
                table: "LeaderboardEntries",
                columns: new[] { "ServerId", "TotalGold" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_ServerId_UserId",
                table: "LeaderboardEntries",
                columns: new[] { "ServerId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaderboardEntries");
        }
    }
}
