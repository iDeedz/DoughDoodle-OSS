using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoughDoodle.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedAIFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsShared",
                table: "FinancialGoals",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SharedWithUserIds",
                table: "FinancialGoals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InvestmentAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    InvestmentType = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostBasis = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AccountNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    InstitutionName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestmentAccounts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FromCategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    ToCategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    NextTransferDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastTransferDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoExecute = table.Column<bool>(type: "INTEGER", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MaxTransfers = table.Column<int>(type: "INTEGER", nullable: true),
                    TransfersCompleted = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledTransfers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduledTransfers_Categories_FromCategoryId",
                        column: x => x.FromCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ScheduledTransfers_Categories_ToCategoryId",
                        column: x => x.ToCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TaxOptimizationSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    PotentialSavings = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    IsApplied = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActionSteps = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxOptimizationSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxOptimizationSuggestions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioHoldings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvestmentAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    AverageCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastPriceUpdate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioHoldings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioHoldings_InvestmentAccounts_InvestmentAccountId",
                        column: x => x.InvestmentAccountId,
                        principalTable: "InvestmentAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentAccounts_UserId_IsActive",
                table: "InvestmentAccounts",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioHoldings_InvestmentAccountId_Symbol",
                table: "PortfolioHoldings",
                columns: new[] { "InvestmentAccountId", "Symbol" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTransfers_FromCategoryId",
                table: "ScheduledTransfers",
                column: "FromCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTransfers_ToCategoryId",
                table: "ScheduledTransfers",
                column: "ToCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTransfers_UserId_NextTransferDate_IsActive",
                table: "ScheduledTransfers",
                columns: new[] { "UserId", "NextTransferDate", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxOptimizationSuggestions_UserId_CreatedAt",
                table: "TaxOptimizationSuggestions",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioHoldings");

            migrationBuilder.DropTable(
                name: "ScheduledTransfers");

            migrationBuilder.DropTable(
                name: "TaxOptimizationSuggestions");

            migrationBuilder.DropTable(
                name: "InvestmentAccounts");

            migrationBuilder.DropColumn(
                name: "IsShared",
                table: "FinancialGoals");

            migrationBuilder.DropColumn(
                name: "SharedWithUserIds",
                table: "FinancialGoals");
        }
    }
}
