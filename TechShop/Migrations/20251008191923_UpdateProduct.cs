using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechShop.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Density",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PiecesPerPackage",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Volume",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Density",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PiecesPerPackage",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Volume",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
