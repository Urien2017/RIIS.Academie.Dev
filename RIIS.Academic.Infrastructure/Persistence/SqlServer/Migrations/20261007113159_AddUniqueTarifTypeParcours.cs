using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RIIS.Academic.Infrastructure.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueTarifTypeParcours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TarifsScolarite_TypeElementScolariteId_ParcoursAcademiqueId",
                table: "TarifsScolarite");

            migrationBuilder.CreateIndex(
                name: "IX_TarifsScolarite_TypeElementScolariteId_ParcoursAcademiqueId",
                table: "TarifsScolarite",
                columns: new[] { "TypeElementScolariteId", "ParcoursAcademiqueId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TarifsScolarite_TypeElementScolariteId_ParcoursAcademiqueId",
                table: "TarifsScolarite");

            migrationBuilder.CreateIndex(
                name: "IX_TarifsScolarite_TypeElementScolariteId_ParcoursAcademiqueId",
                table: "TarifsScolarite",
                columns: new[] { "TypeElementScolariteId", "ParcoursAcademiqueId" });
        }
    }
}
