using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RIIS.Academic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialAcademicSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnneesAcademiques",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Libelle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AnneeDebut = table.Column<short>(type: "smallint", nullable: false),
                    AnneeFin = table.Column<short>(type: "smallint", nullable: false),
                    EstActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnneesAcademiques", x => x.Id);
                    table.CheckConstraint("CK_AnneesAcademiques_Periode", "[AnneeFin] = [AnneeDebut] + 1");
                });

            migrationBuilder.CreateTable(
                name: "CyclesFormation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrdreAffichage = table.Column<short>(type: "smallint", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CyclesFormation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Etablissements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NomOfficiel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sigle = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NumeroAutorisation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NumeroRegistreCommerce = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Banque = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    NumeroCompteBancaire = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    TelephonePrincipal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TelephoneSecondaire = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BoitePostale = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Ville = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Pays = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Facebook = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Instagram = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Adresse = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etablissements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Etudiants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Matricule = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Prenoms = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DateNaissance = table.Column<DateOnly>(type: "date", nullable: false),
                    LieuNaissance = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Sexe = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AptitudeMedicale = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nationalite = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RegionOrigine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TelephonePrincipal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TelephoneSecondaire = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    NomPere = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NomMere = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LieuResidence = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etudiants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Filieres",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EstActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Filieres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NiveauxEtude",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<byte>(type: "tinyint", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NiveauxEtude", x => x.Id);
                    table.CheckConstraint("CK_NiveauxEtude_Numero", "[Numero] BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "ContactsUrgence",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EtudiantId = table.Column<long>(type: "bigint", nullable: false),
                    NomComplet = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LienParente = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TelephonePrincipal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TelephoneSecondaire = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EstPrincipal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactsUrgence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactsUrgence_Etudiants_EtudiantId",
                        column: x => x.EtudiantId,
                        principalTable: "Etudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Specialites",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FiliereId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EstActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Specialites_Filieres_FiliereId",
                        column: x => x.FiliereId,
                        principalTable: "Filieres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CyclesFormationsFilieres",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CycleFormationId = table.Column<long>(type: "bigint", nullable: false),
                    FiliereId = table.Column<long>(type: "bigint", nullable: false),
                    SpecialiteId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EstOuverteInscription = table.Column<bool>(type: "bit", nullable: false),
                    DateOuvertureInscription = table.Column<DateOnly>(type: "date", nullable: true),
                    DateFermetureInscription = table.Column<DateOnly>(type: "date", nullable: true),
                    Capacite = table.Column<int>(type: "int", nullable: true),
                    EstActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CyclesFormationsFilieres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CyclesFormationsFilieres_CyclesFormation_CycleFormationId",
                        column: x => x.CycleFormationId,
                        principalTable: "CyclesFormation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CyclesFormationsFilieres_Filieres_FiliereId",
                        column: x => x.FiliereId,
                        principalTable: "Filieres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CyclesFormationsFilieres_Specialites_SpecialiteId",
                        column: x => x.SpecialiteId,
                        principalTable: "Specialites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaquettesPedagogiques",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CycleFormationFiliereId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DateDebutValidite = table.Column<DateOnly>(type: "date", nullable: true),
                    DateFinValidite = table.Column<DateOnly>(type: "date", nullable: true),
                    SourceDocument = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaquettesPedagogiques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaquettesPedagogiques_CyclesFormationsFilieres_CycleFormationFiliereId",
                        column: x => x.CycleFormationFiliereId,
                        principalTable: "CyclesFormationsFilieres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassesPedagogiques",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CycleFormationFiliereId = table.Column<long>(type: "bigint", nullable: false),
                    NiveauEtudeId = table.Column<long>(type: "bigint", nullable: false),
                    MaquettePedagogiqueId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EstActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassesPedagogiques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassesPedagogiques_CyclesFormationsFilieres_CycleFormationFiliereId",
                        column: x => x.CycleFormationFiliereId,
                        principalTable: "CyclesFormationsFilieres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassesPedagogiques_MaquettesPedagogiques_MaquettePedagogiqueId",
                        column: x => x.MaquettePedagogiqueId,
                        principalTable: "MaquettesPedagogiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassesPedagogiques_NiveauxEtude_NiveauEtudeId",
                        column: x => x.NiveauEtudeId,
                        principalTable: "NiveauxEtude",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SemestresPedagogiques",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaquettePedagogiqueId = table.Column<long>(type: "bigint", nullable: false),
                    NiveauEtudeId = table.Column<long>(type: "bigint", nullable: true),
                    Numero = table.Column<byte>(type: "tinyint", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreditsAttendus = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    VolumeHoraireAttendu = table.Column<short>(type: "smallint", nullable: false),
                    OrdreAffichage = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemestresPedagogiques", x => x.Id);
                    table.CheckConstraint("CK_SemestresPedagogiques_Numero", "[Numero] BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_SemestresPedagogiques_MaquettesPedagogiques_MaquettePedagogiqueId",
                        column: x => x.MaquettePedagogiqueId,
                        principalTable: "MaquettesPedagogiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SemestresPedagogiques_NiveauxEtude_NiveauEtudeId",
                        column: x => x.NiveauEtudeId,
                        principalTable: "NiveauxEtude",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Inscriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EtudiantId = table.Column<long>(type: "bigint", nullable: false),
                    CycleFormationFiliereId = table.Column<long>(type: "bigint", nullable: false),
                    NiveauEtudeId = table.Column<long>(type: "bigint", nullable: false),
                    MaquettePedagogiqueId = table.Column<long>(type: "bigint", nullable: true),
                    ClassePedagogiqueId = table.Column<long>(type: "bigint", nullable: true),
                    DateInscription = table.Column<DateOnly>(type: "date", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MentionSpeciale = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TutelleAcademique = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CodeAdministration = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inscriptions_ClassesPedagogiques_ClassePedagogiqueId",
                        column: x => x.ClassePedagogiqueId,
                        principalTable: "ClassesPedagogiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscriptions_CyclesFormationsFilieres_CycleFormationFiliereId",
                        column: x => x.CycleFormationFiliereId,
                        principalTable: "CyclesFormationsFilieres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscriptions_Etudiants_EtudiantId",
                        column: x => x.EtudiantId,
                        principalTable: "Etudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscriptions_MaquettesPedagogiques_MaquettePedagogiqueId",
                        column: x => x.MaquettePedagogiqueId,
                        principalTable: "MaquettesPedagogiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscriptions_NiveauxEtude_NiveauEtudeId",
                        column: x => x.NiveauEtudeId,
                        principalTable: "NiveauxEtude",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcesVerbaux",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnneeAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    ClassePedagogiqueId = table.Column<long>(type: "bigint", nullable: false),
                    SemestrePedagogiqueId = table.Column<long>(type: "bigint", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CodeSession = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Titre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DateEditionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstDefinitif = table.Column<bool>(type: "bit", nullable: false),
                    CheminFichier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcesVerbaux", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcesVerbaux_AnneesAcademiques_AnneeAcademiqueId",
                        column: x => x.AnneeAcademiqueId,
                        principalTable: "AnneesAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcesVerbaux_ClassesPedagogiques_ClassePedagogiqueId",
                        column: x => x.ClassePedagogiqueId,
                        principalTable: "ClassesPedagogiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcesVerbaux_SemestresPedagogiques_SemestrePedagogiqueId",
                        column: x => x.SemestrePedagogiqueId,
                        principalTable: "SemestresPedagogiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitesEnseignement",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SemestrePedagogiqueId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Credits = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    VolumeHoraire = table.Column<short>(type: "smallint", nullable: false),
                    OrdreAffichage = table.Column<short>(type: "smallint", nullable: false),
                    EstObligatoire = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitesEnseignement", x => x.Id);
                    table.CheckConstraint("CK_UnitesEnseignement_CreditsVolume", "[Credits] >= 0 AND [VolumeHoraire] >= 0");
                    table.ForeignKey(
                        name: "FK_UnitesEnseignement_SemestresPedagogiques_SemestrePedagogiqueId",
                        column: x => x.SemestrePedagogiqueId,
                        principalTable: "SemestresPedagogiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DossiersAdmission",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    SerieBaccalaureat = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AnneeObtentionBaccalaureat = table.Column<short>(type: "smallint", nullable: true),
                    MentionBaccalaureat = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DiplomeEntree = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SpecialiteDiplomeEntree = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NumeroEquivalence = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DiplomeEquivalence = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DossiersAdmission", x => x.Id);
                    table.CheckConstraint("CK_DossiersAdmission_AnneeBac", "[AnneeObtentionBaccalaureat] IS NULL OR [AnneeObtentionBaccalaureat] BETWEEN 1950 AND 2100");
                    table.ForeignKey(
                        name: "FK_DossiersAdmission_Inscriptions_InscriptionId",
                        column: x => x.InscriptionId,
                        principalTable: "Inscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResultatsAnnuels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    AnneeAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    NiveauEtudeId = table.Column<long>(type: "bigint", nullable: false),
                    MoyenneAnnuelle = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CreditsAcquis = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreditsRequis = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 60m),
                    Rang = table.Column<int>(type: "int", nullable: true),
                    StatutValidation = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DecisionJury = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CalculeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultatsAnnuels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResultatsAnnuels_AnneesAcademiques_AnneeAcademiqueId",
                        column: x => x.AnneeAcademiqueId,
                        principalTable: "AnneesAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResultatsAnnuels_Inscriptions_InscriptionId",
                        column: x => x.InscriptionId,
                        principalTable: "Inscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResultatsAnnuels_NiveauxEtude_NiveauEtudeId",
                        column: x => x.NiveauEtudeId,
                        principalTable: "NiveauxEtude",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResultatsSemestres",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    SemestrePedagogiqueId = table.Column<long>(type: "bigint", nullable: false),
                    MoyenneControleContinu = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MoyenneControleConnaissance = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MoyenneSessionNormale = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MoyenneSessionRattrapage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MoyenneSemestrielle = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CreditsAcquis = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreditsRequis = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 30m),
                    Rang = table.Column<int>(type: "int", nullable: true),
                    StatutValidation = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DecisionJury = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CalculeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultatsSemestres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResultatsSemestres_Inscriptions_InscriptionId",
                        column: x => x.InscriptionId,
                        principalTable: "Inscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResultatsSemestres_SemestresPedagogiques_SemestrePedagogiqueId",
                        column: x => x.SemestrePedagogiqueId,
                        principalTable: "SemestresPedagogiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ValidationsInscriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    LieuSignature = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateSignatureEtudiant = table.Column<DateOnly>(type: "date", nullable: true),
                    NomSignataireEtudiant = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SignatureEtudiantUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NomSignataireAdministration = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SignatureAdministrationUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateValidationAdministration = table.Column<DateOnly>(type: "date", nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationsInscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationsInscriptions_Inscriptions_InscriptionId",
                        column: x => x.InscriptionId,
                        principalTable: "Inscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcesVerbauxLignes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcesVerbalId = table.Column<long>(type: "bigint", nullable: false),
                    InscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    MatriculeSnapshot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NomCompletSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    MoyenneGenerale = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CreditsAcquis = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Rang = table.Column<int>(type: "int", nullable: true),
                    DecisionJury = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DetailsNotesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcesVerbauxLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcesVerbauxLignes_Inscriptions_InscriptionId",
                        column: x => x.InscriptionId,
                        principalTable: "Inscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcesVerbauxLignes_ProcesVerbaux_ProcesVerbalId",
                        column: x => x.ProcesVerbalId,
                        principalTable: "ProcesVerbaux",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ElementsConstitutifs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniteEnseignementId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Credits = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Coefficient = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 1m),
                    VolumeHoraire = table.Column<short>(type: "smallint", nullable: false),
                    OrdreAffichage = table.Column<short>(type: "smallint", nullable: false),
                    EstObligatoire = table.Column<bool>(type: "bit", nullable: false),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElementsConstitutifs", x => x.Id);
                    table.CheckConstraint("CK_ElementsConstitutifs_CreditsCoefVolume", "[Credits] >= 0 AND [Coefficient] >= 0 AND [VolumeHoraire] >= 0");
                    table.ForeignKey(
                        name: "FK_ElementsConstitutifs_UnitesEnseignement_UniteEnseignementId",
                        column: x => x.UniteEnseignementId,
                        principalTable: "UnitesEnseignement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResultatsUnitesEnseignement",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    UniteEnseignementId = table.Column<long>(type: "bigint", nullable: false),
                    Moyenne = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CreditsAcquis = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreditsAttendus = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    StatutValidation = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CalculeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultatsUnitesEnseignement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResultatsUnitesEnseignement_Inscriptions_InscriptionId",
                        column: x => x.InscriptionId,
                        principalTable: "Inscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResultatsUnitesEnseignement_UnitesEnseignement_UniteEnseignementId",
                        column: x => x.UniteEnseignementId,
                        principalTable: "UnitesEnseignement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationsAcademiques",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnneeAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    ElementConstitutifId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Numero = table.Column<byte>(type: "tinyint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Bareme = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 20m),
                    PonderationPourcentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DateEvaluation = table.Column<DateOnly>(type: "date", nullable: true),
                    EvaluationRemplaceeId = table.Column<long>(type: "bigint", nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationsAcademiques", x => x.Id);
                    table.CheckConstraint("CK_EvaluationsAcademiques_BaremePonderation", "[Bareme] > 0 AND [PonderationPourcentage] >= 0 AND [PonderationPourcentage] <= 100");
                    table.ForeignKey(
                        name: "FK_EvaluationsAcademiques_AnneesAcademiques_AnneeAcademiqueId",
                        column: x => x.AnneeAcademiqueId,
                        principalTable: "AnneesAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationsAcademiques_ElementsConstitutifs_ElementConstitutifId",
                        column: x => x.ElementConstitutifId,
                        principalTable: "ElementsConstitutifs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationsAcademiques_EvaluationsAcademiques_EvaluationRemplaceeId",
                        column: x => x.EvaluationRemplaceeId,
                        principalTable: "EvaluationsAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResultatsElementsConstitutifs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    ElementConstitutifId = table.Column<long>(type: "bigint", nullable: false),
                    MoyenneControleContinu = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MoyenneControleConnaissance = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MoyenneSessionNormale = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MoyenneSessionRattrapage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MoyenneAvantRattrapage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MoyenneApresRattrapage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MoyenneRetenue = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CreditsAcquis = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    StatutValidation = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EstEligibleRattrapage = table.Column<bool>(type: "bit", nullable: false),
                    CalculeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultatsElementsConstitutifs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResultatsElementsConstitutifs_ElementsConstitutifs_ElementConstitutifId",
                        column: x => x.ElementConstitutifId,
                        principalTable: "ElementsConstitutifs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResultatsElementsConstitutifs_Inscriptions_InscriptionId",
                        column: x => x.InscriptionId,
                        principalTable: "Inscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotesEvaluations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    InscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    Valeur = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    StatutPresence = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SaisieLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SaisiePar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotesEvaluations", x => x.Id);
                    table.CheckConstraint("CK_NotesEvaluations_Valeur", "[Valeur] IS NULL OR ([Valeur] >= 0 AND [Valeur] <= 20)");
                    table.ForeignKey(
                        name: "FK_NotesEvaluations_EvaluationsAcademiques_EvaluationAcademiqueId",
                        column: x => x.EvaluationAcademiqueId,
                        principalTable: "EvaluationsAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotesEvaluations_Inscriptions_InscriptionId",
                        column: x => x.InscriptionId,
                        principalTable: "Inscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CyclesFormation",
                columns: new[] { "Id", "Code", "EstActif", "Libelle", "OrdreAffichage" },
                values: new object[,]
                {
                    { 1L, "PREPA", true, "Préparatoire", (short)1 },
                    { 2L, "BTS", true, "Brevet de technicien supérieur", (short)2 },
                    { 3L, "LICENCE", true, "Licence", (short)3 },
                    { 4L, "MASTER", true, "Master", (short)4 }
                });

            migrationBuilder.InsertData(
                table: "Etablissements",
                columns: new[] { "Id", "Adresse", "Banque", "BoitePostale", "Email", "EstActif", "Facebook", "Instagram", "LogoUrl", "NomOfficiel", "NumeroAutorisation", "NumeroCompteBancaire", "NumeroRegistreCommerce", "Pays", "Sigle", "TelephonePrincipal", "TelephoneSecondaire", "Ville" },
                values: new object[] { 1L, "Nouvelle route Bastos, entrée derrière la Banque Mondiale", "UBA", "BP 1463", "contact@riis.institut", true, "riis", "riis_officiel", null, "RAPUS INTERNATIONAL INSTITUTE SCHOOL", "23-02048/L/MINESUP/SG/DDES/ESUP/SDA/AOSB", "10033 05206 06011000466 49", "RC/YAO/2022/B/1986", "Cameroun", "RIIS", "+237 699 51 17 76", "+237 651 61 35 98", "Yaoundé" });

            migrationBuilder.InsertData(
                table: "NiveauxEtude",
                columns: new[] { "Id", "EstActif", "Libelle", "Numero" },
                values: new object[,]
                {
                    { 1L, true, "Niveau 1", (byte)1 },
                    { 2L, true, "Niveau 2", (byte)2 },
                    { 3L, true, "Niveau 3", (byte)3 },
                    { 4L, true, "Niveau 4", (byte)4 },
                    { 5L, true, "Niveau 5", (byte)5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnneesAcademiques_AnneeDebut_AnneeFin",
                table: "AnneesAcademiques",
                columns: new[] { "AnneeDebut", "AnneeFin" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnneesAcademiques_Libelle",
                table: "AnneesAcademiques",
                column: "Libelle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassesPedagogiques_CycleFormationFiliereId_NiveauEtudeId_Code",
                table: "ClassesPedagogiques",
                columns: new[] { "CycleFormationFiliereId", "NiveauEtudeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassesPedagogiques_MaquettePedagogiqueId",
                table: "ClassesPedagogiques",
                column: "MaquettePedagogiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassesPedagogiques_NiveauEtudeId",
                table: "ClassesPedagogiques",
                column: "NiveauEtudeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactsUrgence_EtudiantId",
                table: "ContactsUrgence",
                column: "EtudiantId");

            migrationBuilder.CreateIndex(
                name: "IX_CyclesFormation_Code",
                table: "CyclesFormation",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CyclesFormationsFilieres_Code",
                table: "CyclesFormationsFilieres",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CyclesFormationsFilieres_CycleFormationId_FiliereId_SpecialiteId",
                table: "CyclesFormationsFilieres",
                columns: new[] { "CycleFormationId", "FiliereId", "SpecialiteId" },
                unique: true,
                filter: "[SpecialiteId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CyclesFormationsFilieres_FiliereId",
                table: "CyclesFormationsFilieres",
                column: "FiliereId");

            migrationBuilder.CreateIndex(
                name: "IX_CyclesFormationsFilieres_SpecialiteId",
                table: "CyclesFormationsFilieres",
                column: "SpecialiteId");

            migrationBuilder.CreateIndex(
                name: "IX_DossiersAdmission_InscriptionId",
                table: "DossiersAdmission",
                column: "InscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElementsConstitutifs_UniteEnseignementId_Code",
                table: "ElementsConstitutifs",
                columns: new[] { "UniteEnseignementId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ElementsConstitutifs_UniteEnseignementId_OrdreAffichage",
                table: "ElementsConstitutifs",
                columns: new[] { "UniteEnseignementId", "OrdreAffichage" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Etablissements_Sigle",
                table: "Etablissements",
                column: "Sigle",
                unique: true,
                filter: "[Sigle] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Etudiants_Matricule",
                table: "Etudiants",
                column: "Matricule",
                unique: true,
                filter: "[Matricule] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Etudiants_Nom",
                table: "Etudiants",
                column: "Nom");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsAcademiques_AnneeAcademiqueId_ElementConstitutifId_Type_Numero",
                table: "EvaluationsAcademiques",
                columns: new[] { "AnneeAcademiqueId", "ElementConstitutifId", "Type", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsAcademiques_ElementConstitutifId",
                table: "EvaluationsAcademiques",
                column: "ElementConstitutifId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsAcademiques_EvaluationRemplaceeId",
                table: "EvaluationsAcademiques",
                column: "EvaluationRemplaceeId");

            migrationBuilder.CreateIndex(
                name: "IX_Filieres_Code",
                table: "Filieres",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_ClassePedagogiqueId",
                table: "Inscriptions",
                column: "ClassePedagogiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_CodeAdministration",
                table: "Inscriptions",
                column: "CodeAdministration",
                unique: true,
                filter: "[CodeAdministration] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_CycleFormationFiliereId",
                table: "Inscriptions",
                column: "CycleFormationFiliereId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_EtudiantId_CycleFormationFiliereId_NiveauEtudeId",
                table: "Inscriptions",
                columns: new[] { "EtudiantId", "CycleFormationFiliereId", "NiveauEtudeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_MaquettePedagogiqueId",
                table: "Inscriptions",
                column: "MaquettePedagogiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_NiveauEtudeId",
                table: "Inscriptions",
                column: "NiveauEtudeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaquettesPedagogiques_CycleFormationFiliereId_Code_Version",
                table: "MaquettesPedagogiques",
                columns: new[] { "CycleFormationFiliereId", "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NiveauxEtude_Numero",
                table: "NiveauxEtude",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotesEvaluations_EvaluationAcademiqueId_InscriptionId",
                table: "NotesEvaluations",
                columns: new[] { "EvaluationAcademiqueId", "InscriptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotesEvaluations_InscriptionId",
                table: "NotesEvaluations",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesVerbaux_AnneeAcademiqueId",
                table: "ProcesVerbaux",
                column: "AnneeAcademiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesVerbaux_ClassePedagogiqueId_SemestrePedagogiqueId_Type_CodeSession",
                table: "ProcesVerbaux",
                columns: new[] { "ClassePedagogiqueId", "SemestrePedagogiqueId", "Type", "CodeSession" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcesVerbaux_SemestrePedagogiqueId",
                table: "ProcesVerbaux",
                column: "SemestrePedagogiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesVerbauxLignes_InscriptionId",
                table: "ProcesVerbauxLignes",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesVerbauxLignes_ProcesVerbalId_InscriptionId",
                table: "ProcesVerbauxLignes",
                columns: new[] { "ProcesVerbalId", "InscriptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsAnnuels_AnneeAcademiqueId",
                table: "ResultatsAnnuels",
                column: "AnneeAcademiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsAnnuels_InscriptionId_AnneeAcademiqueId_NiveauEtudeId",
                table: "ResultatsAnnuels",
                columns: new[] { "InscriptionId", "AnneeAcademiqueId", "NiveauEtudeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsAnnuels_NiveauEtudeId",
                table: "ResultatsAnnuels",
                column: "NiveauEtudeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsElementsConstitutifs_ElementConstitutifId",
                table: "ResultatsElementsConstitutifs",
                column: "ElementConstitutifId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsElementsConstitutifs_InscriptionId_ElementConstitutifId",
                table: "ResultatsElementsConstitutifs",
                columns: new[] { "InscriptionId", "ElementConstitutifId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsSemestres_InscriptionId_SemestrePedagogiqueId",
                table: "ResultatsSemestres",
                columns: new[] { "InscriptionId", "SemestrePedagogiqueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsSemestres_SemestrePedagogiqueId",
                table: "ResultatsSemestres",
                column: "SemestrePedagogiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsUnitesEnseignement_InscriptionId_UniteEnseignementId",
                table: "ResultatsUnitesEnseignement",
                columns: new[] { "InscriptionId", "UniteEnseignementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsUnitesEnseignement_UniteEnseignementId",
                table: "ResultatsUnitesEnseignement",
                column: "UniteEnseignementId");

            migrationBuilder.CreateIndex(
                name: "IX_SemestresPedagogiques_MaquettePedagogiqueId_Numero",
                table: "SemestresPedagogiques",
                columns: new[] { "MaquettePedagogiqueId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemestresPedagogiques_NiveauEtudeId",
                table: "SemestresPedagogiques",
                column: "NiveauEtudeId");

            migrationBuilder.CreateIndex(
                name: "IX_Specialites_FiliereId_Code",
                table: "Specialites",
                columns: new[] { "FiliereId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitesEnseignement_SemestrePedagogiqueId_Code",
                table: "UnitesEnseignement",
                columns: new[] { "SemestrePedagogiqueId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationsInscriptions_InscriptionId",
                table: "ValidationsInscriptions",
                column: "InscriptionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactsUrgence");

            migrationBuilder.DropTable(
                name: "DossiersAdmission");

            migrationBuilder.DropTable(
                name: "Etablissements");

            migrationBuilder.DropTable(
                name: "NotesEvaluations");

            migrationBuilder.DropTable(
                name: "ProcesVerbauxLignes");

            migrationBuilder.DropTable(
                name: "ResultatsAnnuels");

            migrationBuilder.DropTable(
                name: "ResultatsElementsConstitutifs");

            migrationBuilder.DropTable(
                name: "ResultatsSemestres");

            migrationBuilder.DropTable(
                name: "ResultatsUnitesEnseignement");

            migrationBuilder.DropTable(
                name: "ValidationsInscriptions");

            migrationBuilder.DropTable(
                name: "EvaluationsAcademiques");

            migrationBuilder.DropTable(
                name: "ProcesVerbaux");

            migrationBuilder.DropTable(
                name: "Inscriptions");

            migrationBuilder.DropTable(
                name: "ElementsConstitutifs");

            migrationBuilder.DropTable(
                name: "AnneesAcademiques");

            migrationBuilder.DropTable(
                name: "ClassesPedagogiques");

            migrationBuilder.DropTable(
                name: "Etudiants");

            migrationBuilder.DropTable(
                name: "UnitesEnseignement");

            migrationBuilder.DropTable(
                name: "SemestresPedagogiques");

            migrationBuilder.DropTable(
                name: "MaquettesPedagogiques");

            migrationBuilder.DropTable(
                name: "NiveauxEtude");

            migrationBuilder.DropTable(
                name: "CyclesFormationsFilieres");

            migrationBuilder.DropTable(
                name: "CyclesFormation");

            migrationBuilder.DropTable(
                name: "Specialites");

            migrationBuilder.DropTable(
                name: "Filieres");
        }
    }
}
