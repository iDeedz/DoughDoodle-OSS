using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoughDoodle.Migrations
{
    /// <inheritdoc />
    public partial class AddNetWorthTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NetWorthItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsAsset = table.Column<bool>(type: "INTEGER", nullable: false),
                    AssetType = table.Column<int>(type: "INTEGER", nullable: true),
                    LiabilityType = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetWorthItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetWorthItems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NetWorthHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NetWorthItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetWorthHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetWorthHistories_NetWorthItems_NetWorthItemId",
                        column: x => x.NetWorthItemId,
                        principalTable: "NetWorthItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NetWorthHistories_NetWorthItemId_RecordedAt",
                table: "NetWorthHistories",
                columns: new[] { "NetWorthItemId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NetWorthItems_UserId_IsAsset",
                table: "NetWorthItems",
                columns: new[] { "UserId", "IsAsset" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NetWorthHistories");

            migrationBuilder.DropTable(
                name: "NetWorthItems");
        }
    }
}
