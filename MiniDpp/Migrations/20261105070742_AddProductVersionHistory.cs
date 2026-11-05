using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniDpp.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVersionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "ProductBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ProductBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "ProductBatchVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductBatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    Gtin = table.Column<string>(type: "TEXT", nullable: false),
                    ModelName = table.Column<string>(type: "TEXT", nullable: false),
                    BatchNumber = table.Column<string>(type: "TEXT", nullable: false),
                    CottonPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    ElastanePercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProductionLocation = table.Column<string>(type: "TEXT", nullable: false),
                    CarbonKgCo2e = table.Column<decimal>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBatchVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBatchVersions_ProductBatches_ProductBatchId",
                        column: x => x.ProductBatchId,
                        principalTable: "ProductBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatchVersions_ProductBatchId_Version",
                table: "ProductBatchVersions",
                columns: new[] { "ProductBatchId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductBatchVersions");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ProductBatches");
        }
    }
}
