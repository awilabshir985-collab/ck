using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeeciyeMarket.Migrations
{
    /// <inheritdoc />
    public partial class AllowGuestProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SellerId",
                table: "Products",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "GuestName",
                table: "Products",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestPhone",
                table: "Products",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SellerId",
                table: "OrderItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestName",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "GuestPhone",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "SellerId",
                table: "Products",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SellerId",
                table: "OrderItems",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
