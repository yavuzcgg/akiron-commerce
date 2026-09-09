using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akiron.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TurkishNameCollation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                collation: "tr-TR-x-icu",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "categories",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                collation: "tr-TR-x-icu",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldCollation: "tr-TR-x-icu");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "categories",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldCollation: "tr-TR-x-icu");
        }
    }
}
