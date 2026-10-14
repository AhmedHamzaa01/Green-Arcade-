using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowCycle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BadgeIcons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The column now holds an icon name from a fixed set, not a URL (DECISIONS 2026-10-13).
            // Renamed rather than dropped; any existing badge gets the default icon.
            migrationBuilder.RenameColumn(
                name: "icon_url",
                table: "badges",
                newName: "icon");

            migrationBuilder.Sql("UPDATE badges SET icon = 'emoji_events'");

            migrationBuilder.AlterColumn<string>(
                name: "icon",
                table: "badges",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_badges_name",
                table: "badges",
                column: "name",
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_badges_name",
                table: "badges");

            migrationBuilder.AlterColumn<string>(
                name: "icon",
                table: "badges",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.RenameColumn(
                name: "icon",
                table: "badges",
                newName: "icon_url");
        }
    }
}
