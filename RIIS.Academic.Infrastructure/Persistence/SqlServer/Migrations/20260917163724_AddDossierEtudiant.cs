using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RIIS.Academic.Infrastructure.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddDossierEtudiant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Etudiants_Matricule",
                table: "Etudiants");

            migrationBuilder.DropColumn(
                name: "Matricule",
                table: "Etudiants");

            migrationBuilder.AddColumn<long>(
                name: "DossierEtudiantId",
                table: "DossiersFinanciers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DossierEtudiantId",
                table: "DossiersAdministratifs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DossierEtudiantId",
                table: "DossiersAcademiques",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DossiersEtudiants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EtudiantId = table.Column<long>(type: "bigint", nullable: false),
                    ParcoursAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    Matricule = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DossiersEtudiants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DossiersEtudiants_Etudiants_EtudiantId",
                        column: x => x.EtudiantId,
                        principalTable: "Etudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DossiersEtudiants_ParcoursAcademiques_ParcoursAcademiqueId",
                        column: x => x.ParcoursAcademiqueId,
                        principalTable: "ParcoursAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DossiersFinanciers_DossierEtudiantId",
                table: "DossiersFinanciers",
                column: "DossierEtudiantId",
                unique: true,
                filter: "[DossierEtudiantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DossiersAdministratifs_DossierEtudiantId",
                table: "DossiersAdministratifs",
                column: "DossierEtudiantId",
                unique: true,
                filter: "[DossierEtudiantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DossiersAcademiques_DossierEtudiantId",
                table: "DossiersAcademiques",
                column: "DossierEtudiantId",
                unique: true,
                filter: "[DossierEtudiantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DossiersEtudiants_EtudiantId_ParcoursAcademiqueId",
                table: "DossiersEtudiants",
                columns: new[] { "EtudiantId", "ParcoursAcademiqueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DossiersEtudiants_Matricule",
                table: "DossiersEtudiants",
                column: "Matricule",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DossiersEtudiants_ParcoursAcademiqueId",
                table: "DossiersEtudiants",
                column: "ParcoursAcademiqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_DossiersAcademiques_DossiersEtudiants_DossierEtudiantId",
                table: "DossiersAcademiques",
                column: "DossierEtudiantId",
                principalTable: "DossiersEtudiants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DossiersAdministratifs_DossiersEtudiants_DossierEtudiantId",
                table: "DossiersAdministratifs",
                column: "DossierEtudiantId",
                principalTable: "DossiersEtudiants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DossiersFinanciers_DossiersEtudiants_DossierEtudiantId",
                table: "DossiersFinanciers",
                column: "DossierEtudiantId",
                principalTable: "DossiersEtudiants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DossiersAcademiques_DossiersEtudiants_DossierEtudiantId",
                table: "DossiersAcademiques");

            migrationBuilder.DropForeignKey(
                name: "FK_DossiersAdministratifs_DossiersEtudiants_DossierEtudiantId",
                table: "DossiersAdministratifs");

            migrationBuilder.DropForeignKey(
                name: "FK_DossiersFinanciers_DossiersEtudiants_DossierEtudiantId",
                table: "DossiersFinanciers");

            migrationBuilder.DropTable(
                name: "DossiersEtudiants");

            migrationBuilder.DropIndex(
                name: "IX_DossiersFinanciers_DossierEtudiantId",
                table: "DossiersFinanciers");

            migrationBuilder.DropIndex(
                name: "IX_DossiersAdministratifs_DossierEtudiantId",
                table: "DossiersAdministratifs");

            migrationBuilder.DropIndex(
                name: "IX_DossiersAcademiques_DossierEtudiantId",
                table: "DossiersAcademiques");

            migrationBuilder.DropColumn(
                name: "DossierEtudiantId",
                table: "DossiersFinanciers");

            migrationBuilder.DropColumn(
                name: "DossierEtudiantId",
                table: "DossiersAdministratifs");

            migrationBuilder.DropColumn(
                name: "DossierEtudiantId",
                table: "DossiersAcademiques");

            migrationBuilder.AddColumn<string>(
                name: "Matricule",
                table: "Etudiants",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Etudiants_Matricule",
                table: "Etudiants",
                column: "Matricule",
                unique: true,
                filter: "[Matricule] IS NOT NULL");
        }
    }
}
