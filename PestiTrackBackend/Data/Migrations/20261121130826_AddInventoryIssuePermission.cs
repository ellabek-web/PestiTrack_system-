using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PestiTrackBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryIssuePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Name" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000023"), "inventory.issue", "Issue pesticide products from farm inventory.", "Issue inventory" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000023"));
        }
    }
}
