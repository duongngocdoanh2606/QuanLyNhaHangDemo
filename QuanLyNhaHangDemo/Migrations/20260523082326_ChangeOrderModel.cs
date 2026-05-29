using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class ChangeOrderModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrandTotal",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ServiceAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VATAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderCode",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "OrderDetails");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "Orders",
                newName: "GuestName");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "OrderDetails",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "OrderDetailModifiers",
                newName: "ModifierPrice");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GuestName",
                table: "Orders",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "OrderDetails",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "ModifierPrice",
                table: "OrderDetailModifiers",
                newName: "Price");

            migrationBuilder.AddColumn<decimal>(
                name: "GrandTotal",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VATAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "OrderCode",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
