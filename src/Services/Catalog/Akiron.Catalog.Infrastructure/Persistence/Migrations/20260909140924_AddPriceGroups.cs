using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akiron.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "price_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "tr-TR-x-icu"),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    discount_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "price_list_entries",
                columns: table => new
                {
                    price_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_list_entries", x => new { x.price_group_id, x.product_id });
                    table.ForeignKey(
                        name: "fk_price_list_entries_price_groups_price_group_id",
                        column: x => x.price_group_id,
                        principalTable: "price_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_price_list_entries_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_price_groups_code",
                table: "price_groups",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_price_list_entries_product_id",
                table: "price_list_entries",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "price_list_entries");

            migrationBuilder.DropTable(
                name: "price_groups");
        }
    }
}
