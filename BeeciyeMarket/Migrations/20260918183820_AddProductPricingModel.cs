using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeeciyeMarket.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPricingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InitialQuantity",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE Products SET InitialQuantity = Quantity;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitialQuantity",
                table: "Products");
        }
    }
}
