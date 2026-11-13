using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RIIS.Academic.Infrastructure.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class EnsureOneClasseParProgramme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClassesPedagogiques_ParcoursAcademiqueId_Libelle",
                table: "ClassesPedagogiques");

            migrationBuilder.CreateIndex(
                name: "IX_ClassesPedagogiques_ParcoursAcademiqueId",
                table: "ClassesPedagogiques",
                column: "ParcoursAcademiqueId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClassesPedagogiques_ParcoursAcademiqueId",
                table: "ClassesPedagogiques");

            migrationBuilder.CreateIndex(
                name: "IX_ClassesPedagogiques_ParcoursAcademiqueId_Libelle",
                table: "ClassesPedagogiques",
                columns: new[] { "ParcoursAcademiqueId", "Libelle" },
                unique: true);
        }
    }
}
