using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RIIS.Academic.Infrastructure.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddAffectationsMaquettes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaquettesPedagogiques_ParcoursAcademiques_ParcoursAcademiqueId",
                table: "MaquettesPedagogiques");

            migrationBuilder.DropIndex(
                name: "IX_MaquettesPedagogiques_ParcoursAcademiqueId_Code_Version",
                table: "MaquettesPedagogiques");

            migrationBuilder.AlterColumn<long>(
                name: "ParcoursAcademiqueId",
                table: "MaquettesPedagogiques",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "AffectationsMaquettes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParcoursAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    MaquettePedagogiqueId = table.Column<long>(type: "bigint", nullable: false),
                    EstPrincipale = table.Column<bool>(type: "bit", nullable: false),
                    EstActive = table.Column<bool>(type: "bit", nullable: false),
                    DateDebut = table.Column<DateOnly>(type: "date", nullable: true),
                    DateFin = table.Column<DateOnly>(type: "date", nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffectationsMaquettes", x => x.Id);
                    table.CheckConstraint("CK_AffectationsMaquettes_Dates", "[DateFin] IS NULL OR [DateDebut] IS NULL OR [DateFin] >= [DateDebut]");
                    table.ForeignKey(
                        name: "FK_AffectationsMaquettes_MaquettesPedagogiques_MaquettePedagogiqueId",
                        column: x => x.MaquettePedagogiqueId,
                        principalTable: "MaquettesPedagogiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AffectationsMaquettes_ParcoursAcademiques_ParcoursAcademiqueId",
                        column: x => x.ParcoursAcademiqueId,
                        principalTable: "ParcoursAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaquettesPedagogiques_Code_Version",
                table: "MaquettesPedagogiques",
                columns: new[] { "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffectationsMaquettes_MaquettePedagogiqueId",
                table: "AffectationsMaquettes",
                column: "MaquettePedagogiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_AffectationsMaquettes_ParcoursAcademiqueId_EstActive_EstPrincipale",
                table: "AffectationsMaquettes",
                columns: new[] { "ParcoursAcademiqueId", "EstActive", "EstPrincipale" });

            migrationBuilder.CreateIndex(
                name: "IX_AffectationsMaquettes_ParcoursAcademiqueId_MaquettePedagogiqueId",
                table: "AffectationsMaquettes",
                columns: new[] { "ParcoursAcademiqueId", "MaquettePedagogiqueId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AffectationsMaquettes");

            migrationBuilder.DropIndex(
                name: "IX_MaquettesPedagogiques_Code_Version",
                table: "MaquettesPedagogiques");

            migrationBuilder.AlterColumn<long>(
                name: "ParcoursAcademiqueId",
                table: "MaquettesPedagogiques",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaquettesPedagogiques_ParcoursAcademiqueId_Code_Version",
                table: "MaquettesPedagogiques",
                columns: new[] { "ParcoursAcademiqueId", "Code", "Version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MaquettesPedagogiques_ParcoursAcademiques_ParcoursAcademiqueId",
                table: "MaquettesPedagogiques",
                column: "ParcoursAcademiqueId",
                principalTable: "ParcoursAcademiques",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
