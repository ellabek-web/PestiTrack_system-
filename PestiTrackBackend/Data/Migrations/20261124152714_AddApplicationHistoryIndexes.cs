using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PestiTrackBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationHistoryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PesticideApplications_FarmId_Status_ApplicationDateUtc",
                table: "PesticideApplications",
                columns: new[] { "FarmId", "Status", "ApplicationDateUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PesticideApplications_FarmId_Status_ApplicationDateUtc",
                table: "PesticideApplications");
        }
    }
}
