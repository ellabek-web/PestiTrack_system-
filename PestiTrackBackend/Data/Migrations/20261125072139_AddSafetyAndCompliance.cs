using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PestiTrackBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSafetyAndCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LabelNotes",
                table: "Products",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxApplicationRate",
                table: "Products",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaxApplicationRateUnit",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxWindSpeedMetersPerSecond",
                table: "Products",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreHarvestIntervalDays",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReEntryIntervalHours",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RegistrationExpiryDate",
                table: "Products",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignalWord",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComplianceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FarmId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PesticideApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecordType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CheckedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InspectorName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Findings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CorrectiveAction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceRecords_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceRecords_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceRecords_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceRecords_PesticideApplications_PesticideApplicationId",
                        column: x => x.PesticideApplicationId,
                        principalTable: "PesticideApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceRecords_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000024"), "compliance.view", "View certification monitoring, safety intervals, and compliance records.", "View safety and compliance" },
                    { new Guid("10000000-0000-0000-0000-000000000025"), "compliance.manage", "Create and update farm compliance records and corrective actions.", "Manage safety and compliance" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_CreatedByUserId",
                table: "ComplianceRecords",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_FarmId_RecordType",
                table: "ComplianceRecords",
                columns: new[] { "FarmId", "RecordType" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_FarmId_Status_CheckedAtUtc",
                table: "ComplianceRecords",
                columns: new[] { "FarmId", "Status", "CheckedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_FieldId",
                table: "ComplianceRecords",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_PesticideApplicationId",
                table: "ComplianceRecords",
                column: "PesticideApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRecords_WorkerId",
                table: "ComplianceRecords",
                column: "WorkerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplianceRecords");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000025"));

            migrationBuilder.DropColumn(
                name: "LabelNotes",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaxApplicationRate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaxApplicationRateUnit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaxWindSpeedMetersPerSecond",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PreHarvestIntervalDays",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ReEntryIntervalHours",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RegistrationExpiryDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SignalWord",
                table: "Products");
        }
    }
}
