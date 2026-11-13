using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RIIS.Academic.Infrastructure.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUEAndMaquette : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MaquettePedagogiqueId",
                table: "UnitesEnseignement",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitesEnseignement_MaquettePedagogiqueId",
                table: "UnitesEnseignement",
                column: "MaquettePedagogiqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_UnitesEnseignement_MaquettesPedagogiques_MaquettePedagogiqueId",
                table: "UnitesEnseignement",
                column: "MaquettePedagogiqueId",
                principalTable: "MaquettesPedagogiques",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnitesEnseignement_MaquettesPedagogiques_MaquettePedagogiqueId",
                table: "UnitesEnseignement");

            migrationBuilder.DropIndex(
                name: "IX_UnitesEnseignement_MaquettePedagogiqueId",
                table: "UnitesEnseignement");

            migrationBuilder.DropColumn(
                name: "MaquettePedagogiqueId",
                table: "UnitesEnseignement");
        }
    }
}
