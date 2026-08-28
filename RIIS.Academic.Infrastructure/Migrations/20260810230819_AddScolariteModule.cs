using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RIIS.Academic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScolariteModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DossiersScolarite",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    AnneeAcademiqueCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AnneeAcademiqueLibelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CycleCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CycleLibelle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FiliereCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FiliereLibelle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SpecialiteCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SpecialiteLibelle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NiveauNumero = table.Column<int>(type: "int", nullable: false),
                    NiveauLibelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StatutAdministratif = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatutFinancier = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatutGlobal = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateDernierRecalculUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DossiersScolarite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DossiersScolarite_Inscriptions_InscriptionId",
                        column: x => x.InscriptionId,
                        principalTable: "Inscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TypesElementsScolarite",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Categorie = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EstPayable = table.Column<bool>(type: "bit", nullable: false),
                    EstDocumentaire = table.Column<bool>(type: "bit", nullable: false),
                    EstSoumisValidation = table.Column<bool>(type: "bit", nullable: false),
                    EstObligatoire = table.Column<bool>(type: "bit", nullable: false),
                    OrdreAffichage = table.Column<int>(type: "int", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesElementsScolarite", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElementsScolariteEtudiants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DossierScolariteId = table.Column<long>(type: "bigint", nullable: false),
                    TypeElementScolariteId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MontantAttendu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantAffecte = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantRestant = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateDernierRecalculUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElementsScolariteEtudiants", x => x.Id);
                    table.CheckConstraint("CK_ElementsScolariteEtudiants_Montants", "[MontantAttendu] >= 0 AND [MontantAffecte] >= 0 AND [MontantRestant] >= 0");
                    table.ForeignKey(
                        name: "FK_ElementsScolariteEtudiants_DossiersScolarite_DossierScolariteId",
                        column: x => x.DossierScolariteId,
                        principalTable: "DossiersScolarite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ElementsScolariteEtudiants_TypesElementsScolarite_TypeElementScolariteId",
                        column: x => x.TypeElementScolariteId,
                        principalTable: "TypesElementsScolarite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TarifsScolarite",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeElementScolariteId = table.Column<long>(type: "bigint", nullable: false),
                    AnneeAcademiqueCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CycleCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NiveauNumero = table.Column<int>(type: "int", nullable: true),
                    FiliereCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SpecialiteCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Devise = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DateDebutValidite = table.Column<DateOnly>(type: "date", nullable: false),
                    DateFinValidite = table.Column<DateOnly>(type: "date", nullable: true),
                    Priorite = table.Column<int>(type: "int", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarifsScolarite", x => x.Id);
                    table.CheckConstraint("CK_TarifsScolarite_Montant", "[Montant] >= 0");
                    table.ForeignKey(
                        name: "FK_TarifsScolarite_TypesElementsScolarite_TypeElementScolariteId",
                        column: x => x.TypeElementScolariteId,
                        principalTable: "TypesElementsScolarite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentsElementsScolarite",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElementScolariteEtudiantId = table.Column<long>(type: "bigint", nullable: false),
                    NomDocument = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UrlFichier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DateDepotUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateVerificationUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiePar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentsElementsScolarite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentsElementsScolarite_ElementsScolariteEtudiants_ElementScolariteEtudiantId",
                        column: x => x.ElementScolariteEtudiantId,
                        principalTable: "ElementsScolariteEtudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EcheancesScolarite",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElementScolariteEtudiantId = table.Column<long>(type: "bigint", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DateExigibilite = table.Column<DateOnly>(type: "date", nullable: false),
                    MontantAttendu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantAffecte = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantRestant = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DateDernierRecalculUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcheancesScolarite", x => x.Id);
                    table.CheckConstraint("CK_EcheancesScolarite_Montants", "[MontantAttendu] >= 0 AND [MontantAffecte] >= 0 AND [MontantRestant] >= 0");
                    table.ForeignKey(
                        name: "FK_EcheancesScolarite_ElementsScolariteEtudiants_ElementScolariteEtudiantId",
                        column: x => x.ElementScolariteEtudiantId,
                        principalTable: "ElementsScolariteEtudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaiementsScolarite",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DossierScolariteId = table.Column<long>(type: "bigint", nullable: false),
                    ElementScolariteEtudiantId = table.Column<long>(type: "bigint", nullable: true),
                    DatePaiement = table.Column<DateOnly>(type: "date", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ModePaiement = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferencePaiement = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EncaissePar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MontantAffecte = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantNonAffecte = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaiementsScolarite", x => x.Id);
                    table.CheckConstraint("CK_PaiementsScolarite_Montants", "[Montant] >= 0 AND [MontantAffecte] >= 0 AND [MontantNonAffecte] >= 0");
                    table.ForeignKey(
                        name: "FK_PaiementsScolarite_DossiersScolarite_DossierScolariteId",
                        column: x => x.DossierScolariteId,
                        principalTable: "DossiersScolarite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaiementsScolarite_ElementsScolariteEtudiants_ElementScolariteEtudiantId",
                        column: x => x.ElementScolariteEtudiantId,
                        principalTable: "ElementsScolariteEtudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ValidationsElementsScolarite",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElementScolariteEtudiantId = table.Column<long>(type: "bigint", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DateValidationUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidePar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MotifRejet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationsElementsScolarite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationsElementsScolarite_ElementsScolariteEtudiants_ElementScolariteEtudiantId",
                        column: x => x.ElementScolariteEtudiantId,
                        principalTable: "ElementsScolariteEtudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationsScolarite",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DossierScolariteId = table.Column<long>(type: "bigint", nullable: false),
                    ElementScolariteEtudiantId = table.Column<long>(type: "bigint", nullable: true),
                    EcheanceScolariteId = table.Column<long>(type: "bigint", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Canal = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Titre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DateGenerationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateEnvoiUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationsScolarite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationsScolarite_DossiersScolarite_DossierScolariteId",
                        column: x => x.DossierScolariteId,
                        principalTable: "DossiersScolarite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationsScolarite_EcheancesScolarite_EcheanceScolariteId",
                        column: x => x.EcheanceScolariteId,
                        principalTable: "EcheancesScolarite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationsScolarite_ElementsScolariteEtudiants_ElementScolariteEtudiantId",
                        column: x => x.ElementScolariteEtudiantId,
                        principalTable: "ElementsScolariteEtudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AffectationsPaiementsEcheances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaiementScolariteId = table.Column<long>(type: "bigint", nullable: false),
                    EcheanceScolariteId = table.Column<long>(type: "bigint", nullable: false),
                    MontantAffecte = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DateAffectationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AffectePar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffectationsPaiementsEcheances", x => x.Id);
                    table.CheckConstraint("CK_AffectationsPaiementsEcheances_Montant", "[MontantAffecte] > 0");
                    table.ForeignKey(
                        name: "FK_AffectationsPaiementsEcheances_EcheancesScolarite_EcheanceScolariteId",
                        column: x => x.EcheanceScolariteId,
                        principalTable: "EcheancesScolarite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AffectationsPaiementsEcheances_PaiementsScolarite_PaiementScolariteId",
                        column: x => x.PaiementScolariteId,
                        principalTable: "PaiementsScolarite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AffectationsPaiementsEcheances_EcheanceScolariteId",
                table: "AffectationsPaiementsEcheances",
                column: "EcheanceScolariteId");

            migrationBuilder.CreateIndex(
                name: "IX_AffectationsPaiementsEcheances_PaiementScolariteId_EcheanceScolariteId",
                table: "AffectationsPaiementsEcheances",
                columns: new[] { "PaiementScolariteId", "EcheanceScolariteId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsElementsScolarite_ElementScolariteEtudiantId_NomDocument",
                table: "DocumentsElementsScolarite",
                columns: new[] { "ElementScolariteEtudiantId", "NomDocument" });

            migrationBuilder.CreateIndex(
                name: "IX_DossiersScolarite_AnneeAcademiqueCode_CycleCode_FiliereCode_SpecialiteCode_NiveauNumero",
                table: "DossiersScolarite",
                columns: new[] { "AnneeAcademiqueCode", "CycleCode", "FiliereCode", "SpecialiteCode", "NiveauNumero" });

            migrationBuilder.CreateIndex(
                name: "IX_DossiersScolarite_InscriptionId",
                table: "DossiersScolarite",
                column: "InscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EcheancesScolarite_DateExigibilite_Statut",
                table: "EcheancesScolarite",
                columns: new[] { "DateExigibilite", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_EcheancesScolarite_ElementScolariteEtudiantId_Numero",
                table: "EcheancesScolarite",
                columns: new[] { "ElementScolariteEtudiantId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElementsScolariteEtudiants_DossierScolariteId_Statut",
                table: "ElementsScolariteEtudiants",
                columns: new[] { "DossierScolariteId", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_ElementsScolariteEtudiants_DossierScolariteId_TypeElementScolariteId",
                table: "ElementsScolariteEtudiants",
                columns: new[] { "DossierScolariteId", "TypeElementScolariteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElementsScolariteEtudiants_TypeElementScolariteId",
                table: "ElementsScolariteEtudiants",
                column: "TypeElementScolariteId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationsScolarite_DossierScolariteId_Statut_DateGenerationUtc",
                table: "NotificationsScolarite",
                columns: new[] { "DossierScolariteId", "Statut", "DateGenerationUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationsScolarite_EcheanceScolariteId_Type",
                table: "NotificationsScolarite",
                columns: new[] { "EcheanceScolariteId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationsScolarite_ElementScolariteEtudiantId",
                table: "NotificationsScolarite",
                column: "ElementScolariteEtudiantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsScolarite_DossierScolariteId_DatePaiement",
                table: "PaiementsScolarite",
                columns: new[] { "DossierScolariteId", "DatePaiement" });

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsScolarite_ElementScolariteEtudiantId",
                table: "PaiementsScolarite",
                column: "ElementScolariteEtudiantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsScolarite_ReferencePaiement",
                table: "PaiementsScolarite",
                column: "ReferencePaiement");

            migrationBuilder.CreateIndex(
                name: "IX_TarifsScolarite_TypeElementScolariteId_AnneeAcademiqueCode_CycleCode_NiveauNumero_FiliereCode_SpecialiteCode",
                table: "TarifsScolarite",
                columns: new[] { "TypeElementScolariteId", "AnneeAcademiqueCode", "CycleCode", "NiveauNumero", "FiliereCode", "SpecialiteCode" });

            migrationBuilder.CreateIndex(
                name: "IX_TypesElementsScolarite_Code",
                table: "TypesElementsScolarite",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationsElementsScolarite_ElementScolariteEtudiantId_Statut",
                table: "ValidationsElementsScolarite",
                columns: new[] { "ElementScolariteEtudiantId", "Statut" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AffectationsPaiementsEcheances");

            migrationBuilder.DropTable(
                name: "DocumentsElementsScolarite");

            migrationBuilder.DropTable(
                name: "NotificationsScolarite");

            migrationBuilder.DropTable(
                name: "TarifsScolarite");

            migrationBuilder.DropTable(
                name: "ValidationsElementsScolarite");

            migrationBuilder.DropTable(
                name: "PaiementsScolarite");

            migrationBuilder.DropTable(
                name: "EcheancesScolarite");

            migrationBuilder.DropTable(
                name: "ElementsScolariteEtudiants");

            migrationBuilder.DropTable(
                name: "DossiersScolarite");

            migrationBuilder.DropTable(
                name: "TypesElementsScolarite");
        }
    }
}
