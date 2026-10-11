using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowCycle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrdersAndAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "order_number_seq",
                startValue: 10001L);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "order_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "variant_name",
                table: "order_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "full_name",
                table: "addresses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "addresses",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "variant_name",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "full_name",
                table: "addresses");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "addresses");

            migrationBuilder.DropSequence(
                name: "order_number_seq");
        }
    }
}
