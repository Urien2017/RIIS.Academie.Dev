using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RIIS.Academic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeToTarifsScolarite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "TarifsScolarite",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TarifsScolarite_Code",
                table: "TarifsScolarite",
                column: "Code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TarifsScolarite_Code",
                table: "TarifsScolarite");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "TarifsScolarite");
        }
    }
}
