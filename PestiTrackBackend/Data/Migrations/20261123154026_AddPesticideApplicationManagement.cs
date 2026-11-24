using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PestiTrackBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPesticideApplicationManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PesticideApplicationProductId",
                table: "InventoryTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PesticideApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FarmId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldCropId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetPest = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApplicationMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Equipment = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AreaTreatedHectares = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    WeatherRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PesticideApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PesticideApplications_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PesticideApplications_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PesticideApplications_FieldCrops_FieldCropId",
                        column: x => x.FieldCropId,
                        principalTable: "FieldCrops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PesticideApplications_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PesticideApplications_WeatherRecords_WeatherRecordId",
                        column: x => x.WeatherRecordId,
                        principalTable: "WeatherRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PesticideApplications_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PesticideApplicationProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PesticideApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QuantityUsed = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ApplicationRate = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RateUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PesticideApplicationProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PesticideApplicationProducts_PesticideApplications_PesticideApplicationId",
                        column: x => x.PesticideApplicationId,
                        principalTable: "PesticideApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PesticideApplicationProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ApplicationProductId",
                table: "InventoryTransactions",
                column: "PesticideApplicationProductId",
                unique: true,
                filter: "[PesticideApplicationProductId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplicationProducts_PesticideApplicationId",
                table: "PesticideApplicationProducts",
                column: "PesticideApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplicationProducts_ProductId",
                table: "PesticideApplicationProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_CreatedByUserId",
                table: "PesticideApplications",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_FarmId_ApplicationDateUtc",
                table: "PesticideApplications",
                columns: new[] { "FarmId", "ApplicationDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_FarmId_FieldId",
                table: "PesticideApplications",
                columns: new[] { "FarmId", "FieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_FieldCropId",
                table: "PesticideApplications",
                column: "FieldCropId");

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_FieldId",
                table: "PesticideApplications",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_WeatherRecordId",
                table: "PesticideApplications",
                column: "WeatherRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_WorkerId",
                table: "PesticideApplications",
                column: "WorkerId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_PesticideApplicationProducts_PesticideApplicationProductId",
                table: "InventoryTransactions",
                column: "PesticideApplicationProductId",
                principalTable: "PesticideApplicationProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_PesticideApplicationProducts_PesticideApplicationProductId",
                table: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "PesticideApplicationProducts");

            migrationBuilder.DropTable(
                name: "PesticideApplications");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ApplicationProductId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "PesticideApplicationProductId",
                table: "InventoryTransactions");
        }
    }
}
