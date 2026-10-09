using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowCycle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PointsLedgerSourceUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_points_ledger_source_type_source_id",
                table: "points_ledger");

            migrationBuilder.CreateIndex(
                name: "ix_points_ledger_source_type_source_id_type",
                table: "points_ledger",
                columns: new[] { "source_type", "source_id", "type" },
                unique: true,
                filter: "source_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_points_ledger_source_type_source_id_type",
                table: "points_ledger");

            migrationBuilder.CreateIndex(
                name: "ix_points_ledger_source_type_source_id",
                table: "points_ledger",
                columns: new[] { "source_type", "source_id" });
        }
    }
}
