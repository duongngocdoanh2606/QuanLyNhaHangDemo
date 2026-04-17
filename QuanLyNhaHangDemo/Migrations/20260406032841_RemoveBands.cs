using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhaHangDemo.Migrations
{
    public partial class RemoveBands : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Brands_BrandId",
                table: "Suppliers");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.RenameColumn(
                name: "BrandId",
                table: "Suppliers",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Suppliers_BrandId",
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Categories_CategoryId",
                table: "Suppliers");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "Suppliers",
                newName: "BrandId");

            migrationBuilder.RenameIndex(
                name: "IX_Suppliers_CategoryId",
                table: "Suppliers",
                newName: "IX_Suppliers_BrandId");

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Brands_BrandId",
                table: "Suppliers",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
