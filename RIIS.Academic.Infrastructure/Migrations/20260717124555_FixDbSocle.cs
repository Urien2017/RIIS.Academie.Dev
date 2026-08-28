using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RIIS.Academic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDbSocle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inscriptions_EtudiantId_CycleFormationFiliereId_NiveauEtudeId",
                table: "Inscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ClassesPedagogiques_CycleFormationFiliereId_NiveauEtudeId_Code",
                table: "ClassesPedagogiques");

            migrationBuilder.AddColumn<long>(
                name: "AnneeAcademiqueId",
                table: "Inscriptions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "AnneeAcademiqueId",
                table: "ClassesPedagogiques",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_AnneeAcademiqueId_EtudiantId",
                table: "Inscriptions",
                columns: new[] { "AnneeAcademiqueId", "EtudiantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_EtudiantId",
                table: "Inscriptions",
                column: "EtudiantId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassesPedagogiques_AnneeAcademiqueId_CycleFormationFiliereId_NiveauEtudeId_Code",
                table: "ClassesPedagogiques",
                columns: new[] { "AnneeAcademiqueId", "CycleFormationFiliereId", "NiveauEtudeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassesPedagogiques_CycleFormationFiliereId",
                table: "ClassesPedagogiques",
                column: "CycleFormationFiliereId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassesPedagogiques_AnneesAcademiques_AnneeAcademiqueId",
                table: "ClassesPedagogiques",
                column: "AnneeAcademiqueId",
                principalTable: "AnneesAcademiques",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inscriptions_AnneesAcademiques_AnneeAcademiqueId",
                table: "Inscriptions",
                column: "AnneeAcademiqueId",
                principalTable: "AnneesAcademiques",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassesPedagogiques_AnneesAcademiques_AnneeAcademiqueId",
                table: "ClassesPedagogiques");

            migrationBuilder.DropForeignKey(
                name: "FK_Inscriptions_AnneesAcademiques_AnneeAcademiqueId",
                table: "Inscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Inscriptions_AnneeAcademiqueId_EtudiantId",
                table: "Inscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Inscriptions_EtudiantId",
                table: "Inscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ClassesPedagogiques_AnneeAcademiqueId_CycleFormationFiliereId_NiveauEtudeId_Code",
                table: "ClassesPedagogiques");

            migrationBuilder.DropIndex(
                name: "IX_ClassesPedagogiques_CycleFormationFiliereId",
                table: "ClassesPedagogiques");

            migrationBuilder.DropColumn(
                name: "AnneeAcademiqueId",
                table: "Inscriptions");

            migrationBuilder.DropColumn(
                name: "AnneeAcademiqueId",
                table: "ClassesPedagogiques");

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_EtudiantId_CycleFormationFiliereId_NiveauEtudeId",
                table: "Inscriptions",
                columns: new[] { "EtudiantId", "CycleFormationFiliereId", "NiveauEtudeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassesPedagogiques_CycleFormationFiliereId_NiveauEtudeId_Code",
                table: "ClassesPedagogiques",
                columns: new[] { "CycleFormationFiliereId", "NiveauEtudeId", "Code" },
                unique: true);
        }
    }
}
