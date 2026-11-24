using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PestiTrackBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryReferenceCheckIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ReferenceCheck",
                table: "InventoryTransactions",
                columns: new[] { "FarmId", "ProductId", "BatchNumber", "TransactionType", "Reference" },
                filter: "[Reference] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ReferenceCheck",
                table: "InventoryTransactions");
        }
    }
}
