using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RowCycle.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_submission_categories_name",
                table: "submission_categories");

            migrationBuilder.DropIndex(
                name: "ix_products_slug",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_product_variants_sku",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "ix_product_categories_slug",
                table: "product_categories");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "submission_categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "product_variants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "product_images",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "product_categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "badges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "asp_net_users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "addresses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: new Guid("01f0b21c-d090-42d2-a67b-7ed18b67599d"),
                column: "deleted_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: new Guid("1d68bd30-bd19-4df5-b750-fe1014a4e3c7"),
                column: "deleted_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: new Guid("52629265-d063-46a3-9594-2fbac9ac63b0"),
                column: "deleted_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "product_categories",
                keyColumn: "id",
                keyValue: new Guid("b3d85dc1-e1d2-4a6f-bf53-6fd374a3bce0"),
                column: "deleted_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "submission_categories",
                keyColumn: "id",
                keyValue: new Guid("17ce849a-9c7c-46d9-b5e4-aad889192218"),
                column: "deleted_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "submission_categories",
                keyColumn: "id",
                keyValue: new Guid("3a236a25-ac7c-47a1-b914-97eec2772c8e"),
                column: "deleted_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "submission_categories",
                keyColumn: "id",
                keyValue: new Guid("865079da-4bd6-4715-950b-dca027c9f711"),
                column: "deleted_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "submission_categories",
                keyColumn: "id",
                keyValue: new Guid("a7879a02-a50c-4bf4-bccc-ddd893c46334"),
                column: "deleted_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "submission_categories",
                keyColumn: "id",
                keyValue: new Guid("c0b678f5-740b-4a83-8c94-d0b9330871b7"),
                column: "deleted_at",
                value: null);

            migrationBuilder.UpdateData(
                table: "submission_categories",
                keyColumn: "id",
                keyValue: new Guid("dbb6cd09-5c4b-4830-8680-344995ce1079"),
                column: "deleted_at",
                value: null);

            migrationBuilder.CreateIndex(
                name: "ix_submission_categories_name",
                table: "submission_categories",
                column: "name",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_products_slug",
                table: "products",
                column: "slug",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_sku",
                table: "product_variants",
                column: "sku",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_slug",
                table: "product_categories",
                column: "slug",
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_submission_categories_name",
                table: "submission_categories");

            migrationBuilder.DropIndex(
                name: "ix_products_slug",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_product_variants_sku",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "ix_product_categories_slug",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "submission_categories");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "products");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "asp_net_users");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "addresses");

            migrationBuilder.CreateIndex(
                name: "ix_submission_categories_name",
                table: "submission_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_slug",
                table: "products",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_sku",
                table: "product_variants",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_slug",
                table: "product_categories",
                column: "slug",
                unique: true);
        }
    }
}
