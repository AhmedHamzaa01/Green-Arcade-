using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowCycle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LowStockSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "key", "value" },
                values: new object[] { "low_stock_threshold", "5" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "key",
                keyValue: "low_stock_threshold");
        }
    }
}
