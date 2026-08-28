using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RIIS.Academic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModePaiementAndPaiementReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ModePaiementScolariteId",
                table: "PaiementsScolarite",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TarifScolariteId",
                table: "PaiementsScolarite",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TypeElementScolariteId",
                table: "PaiementsScolarite",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModesPaiementScolarite",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    OrdreAffichage = table.Column<int>(type: "int", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModesPaiementScolarite", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsScolarite_DossierScolariteId_TypeElementScolariteId",
                table: "PaiementsScolarite",
                columns: new[] { "DossierScolariteId", "TypeElementScolariteId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsScolarite_ModePaiementScolariteId",
                table: "PaiementsScolarite",
                column: "ModePaiementScolariteId");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsScolarite_TarifScolariteId",
                table: "PaiementsScolarite",
                column: "TarifScolariteId");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsScolarite_TypeElementScolariteId",
                table: "PaiementsScolarite",
                column: "TypeElementScolariteId");

            migrationBuilder.CreateIndex(
                name: "IX_ModesPaiementScolarite_Code",
                table: "ModesPaiementScolarite",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModesPaiementScolarite_EstActif_OrdreAffichage",
                table: "ModesPaiementScolarite",
                columns: new[] { "EstActif", "OrdreAffichage" });

            migrationBuilder.AddForeignKey(
                name: "FK_PaiementsScolarite_ModesPaiementScolarite_ModePaiementScolariteId",
                table: "PaiementsScolarite",
                column: "ModePaiementScolariteId",
                principalTable: "ModesPaiementScolarite",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaiementsScolarite_TarifsScolarite_TarifScolariteId",
                table: "PaiementsScolarite",
                column: "TarifScolariteId",
                principalTable: "TarifsScolarite",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaiementsScolarite_TypesElementsScolarite_TypeElementScolariteId",
                table: "PaiementsScolarite",
                column: "TypeElementScolariteId",
                principalTable: "TypesElementsScolarite",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaiementsScolarite_ModesPaiementScolarite_ModePaiementScolariteId",
                table: "PaiementsScolarite");

            migrationBuilder.DropForeignKey(
                name: "FK_PaiementsScolarite_TarifsScolarite_TarifScolariteId",
                table: "PaiementsScolarite");

            migrationBuilder.DropForeignKey(
                name: "FK_PaiementsScolarite_TypesElementsScolarite_TypeElementScolariteId",
                table: "PaiementsScolarite");

            migrationBuilder.DropTable(
                name: "ModesPaiementScolarite");

            migrationBuilder.DropIndex(
                name: "IX_PaiementsScolarite_DossierScolariteId_TypeElementScolariteId",
                table: "PaiementsScolarite");

            migrationBuilder.DropIndex(
                name: "IX_PaiementsScolarite_ModePaiementScolariteId",
                table: "PaiementsScolarite");

            migrationBuilder.DropIndex(
                name: "IX_PaiementsScolarite_TarifScolariteId",
                table: "PaiementsScolarite");

            migrationBuilder.DropIndex(
                name: "IX_PaiementsScolarite_TypeElementScolariteId",
                table: "PaiementsScolarite");

            migrationBuilder.DropColumn(
                name: "ModePaiementScolariteId",
                table: "PaiementsScolarite");

            migrationBuilder.DropColumn(
                name: "TarifScolariteId",
                table: "PaiementsScolarite");

            migrationBuilder.DropColumn(
                name: "TypeElementScolariteId",
                table: "PaiementsScolarite");
        }
    }
}
