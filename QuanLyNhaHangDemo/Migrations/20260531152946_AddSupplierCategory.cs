using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class AddSupplierCategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Categories_CategoryId",
                table: "Suppliers");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "Suppliers",
                newName: "SupplierCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Suppliers_CategoryId",
                table: "Suppliers",
                newName: "IX_Suppliers_SupplierCategoryId");

            migrationBuilder.CreateTable(
                name: "SupplierCategories",
                columns: table => new
                {
                    SupplierCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierCategories", x => x.SupplierCategoryId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_SupplierCategories_SupplierCategoryId",
                table: "Suppliers",
                column: "SupplierCategoryId",
                principalTable: "SupplierCategories",
                principalColumn: "SupplierCategoryId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_SupplierCategories_SupplierCategoryId",
                table: "Suppliers");

            migrationBuilder.DropTable(
                name: "SupplierCategories");

            migrationBuilder.RenameColumn(
                name: "SupplierCategoryId",
                table: "Suppliers",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Suppliers_SupplierCategoryId",
                table: "Suppliers",
                newName: "IX_Suppliers_CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Categories_CategoryId",
                table: "Suppliers",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
