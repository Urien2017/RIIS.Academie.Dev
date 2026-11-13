using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RIIS.Academic.Infrastructure.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
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
                name: "ClassesPedagogiques",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParcoursAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Effectif = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassesPedagogiques", x => x.Id);
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
                    TelephonePrincipal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TelephoneSecondaire = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TelephoneParent1 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TelephoneParent2 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
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
                    table.CheckConstraint("CK_NiveauxEtude_Numero", "[Numero] >= 1");
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ParentPermissionId = table.Column<long>(type: "bigint", nullable: true),
                    OrdreAffichage = table.Column<int>(type: "int", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_Permissions_ParentPermissionId",
                        column: x => x.ParentPermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EstSysteme = table.Column<bool>(type: "bit", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
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
                name: "UnitesEnseignement",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitesEnseignement", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionInfos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EtudiantId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_AdmissionInfos", x => x.Id);
                    table.CheckConstraint("CK_AdmissionInfos_AnneeBac", "[AnneeObtentionBaccalaureat] IS NULL OR [AnneeObtentionBaccalaureat] BETWEEN 1950 AND 2100");
                    table.ForeignKey(
                        name: "FK_AdmissionInfos_Etudiants_EtudiantId",
                        column: x => x.EtudiantId,
                        principalTable: "Etudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "DocumentsPersonnels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EtudiantId = table.Column<long>(type: "bigint", nullable: false),
                    NomDocument = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UrlFichier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DateDepotUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateVerificationUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiePar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentsPersonnels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentsPersonnels_Etudiants_EtudiantId",
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
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    PermissionId = table.Column<long>(type: "bigint", nullable: false),
                    AttribueLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Utilisateurs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NomUtilisateur = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MotDePasseHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifieLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Utilisateurs_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    VolumeHoraire = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElementsConstitutifs", x => x.Id);
                    table.CheckConstraint("CK_ElementsConstitutifs_CreditsVolume", "[Credits] >= 0 AND [VolumeHoraire] >= 0");
                    table.ForeignKey(
                        name: "FK_ElementsConstitutifs_UnitesEnseignement_UniteEnseignementId",
                        column: x => x.UniteEnseignementId,
                        principalTable: "UnitesEnseignement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParcoursAcademiques",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnneeAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    CycleFormationId = table.Column<long>(type: "bigint", nullable: false),
                    NiveauEtudeId = table.Column<long>(type: "bigint", nullable: false),
                    FiliereId = table.Column<long>(type: "bigint", nullable: false),
                    SpecialiteId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EstActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParcoursAcademiques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParcoursAcademiques_AnneesAcademiques_AnneeAcademiqueId",
                        column: x => x.AnneeAcademiqueId,
                        principalTable: "AnneesAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParcoursAcademiques_CyclesFormation_CycleFormationId",
                        column: x => x.CycleFormationId,
                        principalTable: "CyclesFormation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParcoursAcademiques_Filieres_FiliereId",
                        column: x => x.FiliereId,
                        principalTable: "Filieres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParcoursAcademiques_NiveauxEtude_NiveauEtudeId",
                        column: x => x.NiveauEtudeId,
                        principalTable: "NiveauxEtude",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParcoursAcademiques_Specialites_SpecialiteId",
                        column: x => x.SpecialiteId,
                        principalTable: "Specialites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DossiersAcademiques",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EtudiantId = table.Column<long>(type: "bigint", nullable: false),
                    ParcoursAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    ClassePedagogiqueId = table.Column<long>(type: "bigint", nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DossiersAcademiques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DossiersAcademiques_ClassesPedagogiques_ClassePedagogiqueId",
                        column: x => x.ClassePedagogiqueId,
                        principalTable: "ClassesPedagogiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DossiersAcademiques_Etudiants_EtudiantId",
                        column: x => x.EtudiantId,
                        principalTable: "Etudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DossiersAcademiques_ParcoursAcademiques_ParcoursAcademiqueId",
                        column: x => x.ParcoursAcademiqueId,
                        principalTable: "ParcoursAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DossiersAdministratifs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EtudiantId = table.Column<long>(type: "bigint", nullable: false),
                    ParcoursAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DossiersAdministratifs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DossiersAdministratifs_Etudiants_EtudiantId",
                        column: x => x.EtudiantId,
                        principalTable: "Etudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DossiersAdministratifs_ParcoursAcademiques_ParcoursAcademiqueId",
                        column: x => x.ParcoursAcademiqueId,
                        principalTable: "ParcoursAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DossiersFinanciers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EtudiantId = table.Column<long>(type: "bigint", nullable: false),
                    ParcoursAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    StatutAdministratif = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatutFinancier = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatutGlobal = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateDernierRecalculUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DossiersFinanciers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DossiersFinanciers_Etudiants_EtudiantId",
                        column: x => x.EtudiantId,
                        principalTable: "Etudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DossiersFinanciers_ParcoursAcademiques_ParcoursAcademiqueId",
                        column: x => x.ParcoursAcademiqueId,
                        principalTable: "ParcoursAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaquettesPedagogiques",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParcoursAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
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
                        name: "FK_MaquettesPedagogiques_ParcoursAcademiques_ParcoursAcademiqueId",
                        column: x => x.ParcoursAcademiqueId,
                        principalTable: "ParcoursAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TarifsScolarite",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TypeElementScolariteId = table.Column<long>(type: "bigint", nullable: false),
                    ParcoursAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Devise = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DateNotification = table.Column<DateOnly>(type: "date", nullable: true),
                    Priorite = table.Column<int>(type: "int", nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarifsScolarite", x => x.Id);
                    table.CheckConstraint("CK_TarifsScolarite_Montant", "[Montant] >= 0");
                    table.ForeignKey(
                        name: "FK_TarifsScolarite_ParcoursAcademiques_ParcoursAcademiqueId",
                        column: x => x.ParcoursAcademiqueId,
                        principalTable: "ParcoursAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TarifsScolarite_TypesElementsScolarite_TypeElementScolariteId",
                        column: x => x.TypeElementScolariteId,
                        principalTable: "TypesElementsScolarite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResultatsAnnuels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DossierAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
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
                        name: "FK_ResultatsAnnuels_DossiersAcademiques_DossierAcademiqueId",
                        column: x => x.DossierAcademiqueId,
                        principalTable: "DossiersAcademiques",
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
                name: "ElementsScolariteEtudiants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DossierFinancierId = table.Column<long>(type: "bigint", nullable: false),
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
                        name: "FK_ElementsScolariteEtudiants_DossiersFinanciers_DossierFinancierId",
                        column: x => x.DossierFinancierId,
                        principalTable: "DossiersFinanciers",
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
                name: "SemestresPedagogiques",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaquettePedagogiqueId = table.Column<long>(type: "bigint", nullable: false),
                    NumeroSemestre = table.Column<byte>(type: "tinyint", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrdreAffichage = table.Column<short>(type: "smallint", nullable: false),
                    NiveauEtudeId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemestresPedagogiques", x => x.Id);
                    table.CheckConstraint("CK_SemestresPedagogiques_NumeroSemestre", "[NumeroSemestre] BETWEEN 1 AND 8");
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
                        principalColumn: "Id");
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
                    DossierFinancierId = table.Column<long>(type: "bigint", nullable: false),
                    ElementScolariteEtudiantId = table.Column<long>(type: "bigint", nullable: true),
                    TypeElementScolariteId = table.Column<long>(type: "bigint", nullable: true),
                    TarifScolariteId = table.Column<long>(type: "bigint", nullable: true),
                    ModePaiementScolariteId = table.Column<long>(type: "bigint", nullable: true),
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
                        name: "FK_PaiementsScolarite_DossiersFinanciers_DossierFinancierId",
                        column: x => x.DossierFinancierId,
                        principalTable: "DossiersFinanciers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaiementsScolarite_ElementsScolariteEtudiants_ElementScolariteEtudiantId",
                        column: x => x.ElementScolariteEtudiantId,
                        principalTable: "ElementsScolariteEtudiants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaiementsScolarite_ModesPaiementScolarite_ModePaiementScolariteId",
                        column: x => x.ModePaiementScolariteId,
                        principalTable: "ModesPaiementScolarite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaiementsScolarite_TarifsScolarite_TarifScolariteId",
                        column: x => x.TarifScolariteId,
                        principalTable: "TarifsScolarite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaiementsScolarite_TypesElementsScolarite_TypeElementScolariteId",
                        column: x => x.TypeElementScolariteId,
                        principalTable: "TypesElementsScolarite",
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
                name: "MaquetteElementsConstitutifs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SemestrePedagogiqueId = table.Column<long>(type: "bigint", nullable: false),
                    ElementConstitutifId = table.Column<long>(type: "bigint", nullable: false),
                    Credits = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Coefficient = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 1m),
                    VolumeHoraire = table.Column<short>(type: "smallint", nullable: false),
                    OrdreAffichage = table.Column<short>(type: "smallint", nullable: false),
                    EstObligatoire = table.Column<bool>(type: "bit", nullable: false),
                    Observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaquetteElementsConstitutifs", x => x.Id);
                    table.CheckConstraint("CK_MaquetteElementsConstitutifs_CreditsCoefVolume", "[Credits] >= 0 AND [Coefficient] >= 0 AND [VolumeHoraire] >= 0");
                    table.ForeignKey(
                        name: "FK_MaquetteElementsConstitutifs_ElementsConstitutifs_ElementConstitutifId",
                        column: x => x.ElementConstitutifId,
                        principalTable: "ElementsConstitutifs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaquetteElementsConstitutifs_SemestresPedagogiques_SemestrePedagogiqueId",
                        column: x => x.SemestrePedagogiqueId,
                        principalTable: "SemestresPedagogiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcesVerbaux",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnneeAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    ParcoursAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
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
                        name: "FK_ProcesVerbaux_ParcoursAcademiques_ParcoursAcademiqueId",
                        column: x => x.ParcoursAcademiqueId,
                        principalTable: "ParcoursAcademiques",
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
                name: "ResultatsSemestres",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DossierAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
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
                        name: "FK_ResultatsSemestres_DossiersAcademiques_DossierAcademiqueId",
                        column: x => x.DossierAcademiqueId,
                        principalTable: "DossiersAcademiques",
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
                name: "ResultatsUnitesEnseignement",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DossierAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    SemestrePedagogiqueId = table.Column<long>(type: "bigint", nullable: false),
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
                        name: "FK_ResultatsUnitesEnseignement_DossiersAcademiques_DossierAcademiqueId",
                        column: x => x.DossierAcademiqueId,
                        principalTable: "DossiersAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResultatsUnitesEnseignement_SemestresPedagogiques_SemestrePedagogiqueId",
                        column: x => x.SemestrePedagogiqueId,
                        principalTable: "SemestresPedagogiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationsScolarite",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DossierFinancierId = table.Column<long>(type: "bigint", nullable: false),
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
                        name: "FK_NotificationsScolarite_DossiersFinanciers_DossierFinancierId",
                        column: x => x.DossierFinancierId,
                        principalTable: "DossiersFinanciers",
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

            migrationBuilder.CreateTable(
                name: "EvaluationsAcademiques",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnneeAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    MaquetteElementConstitutifId = table.Column<long>(type: "bigint", nullable: false),
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
                        name: "FK_EvaluationsAcademiques_EvaluationsAcademiques_EvaluationRemplaceeId",
                        column: x => x.EvaluationRemplaceeId,
                        principalTable: "EvaluationsAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationsAcademiques_MaquetteElementsConstitutifs_MaquetteElementConstitutifId",
                        column: x => x.MaquetteElementConstitutifId,
                        principalTable: "MaquetteElementsConstitutifs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResultatsElementsConstitutifs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DossierAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    MaquetteElementConstitutifId = table.Column<long>(type: "bigint", nullable: false),
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
                        name: "FK_ResultatsElementsConstitutifs_DossiersAcademiques_DossierAcademiqueId",
                        column: x => x.DossierAcademiqueId,
                        principalTable: "DossiersAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResultatsElementsConstitutifs_MaquetteElementsConstitutifs_MaquetteElementConstitutifId",
                        column: x => x.MaquetteElementConstitutifId,
                        principalTable: "MaquetteElementsConstitutifs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcesVerbauxLignes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcesVerbalId = table.Column<long>(type: "bigint", nullable: false),
                    DossierAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
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
                        name: "FK_ProcesVerbauxLignes_DossiersAcademiques_DossierAcademiqueId",
                        column: x => x.DossierAcademiqueId,
                        principalTable: "DossiersAcademiques",
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
                name: "NotesEvaluations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
                    DossierAcademiqueId = table.Column<long>(type: "bigint", nullable: false),
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
                        name: "FK_NotesEvaluations_DossiersAcademiques_DossierAcademiqueId",
                        column: x => x.DossierAcademiqueId,
                        principalTable: "DossiersAcademiques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotesEvaluations_EvaluationsAcademiques_EvaluationAcademiqueId",
                        column: x => x.EvaluationAcademiqueId,
                        principalTable: "EvaluationsAcademiques",
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
                name: "IX_AdmissionInfos_EtudiantId",
                table: "AdmissionInfos",
                column: "EtudiantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffectationsPaiementsEcheances_EcheanceScolariteId",
                table: "AffectationsPaiementsEcheances",
                column: "EcheanceScolariteId");

            migrationBuilder.CreateIndex(
                name: "IX_AffectationsPaiementsEcheances_PaiementScolariteId_EcheanceScolariteId",
                table: "AffectationsPaiementsEcheances",
                columns: new[] { "PaiementScolariteId", "EcheanceScolariteId" });

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
                name: "IX_ClassesPedagogiques_ParcoursAcademiqueId_Libelle",
                table: "ClassesPedagogiques",
                columns: new[] { "ParcoursAcademiqueId", "Libelle" },
                unique: true);

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
                name: "IX_DocumentsElementsScolarite_ElementScolariteEtudiantId_NomDocument",
                table: "DocumentsElementsScolarite",
                columns: new[] { "ElementScolariteEtudiantId", "NomDocument" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsPersonnels_EtudiantId_NomDocument",
                table: "DocumentsPersonnels",
                columns: new[] { "EtudiantId", "NomDocument" });

            migrationBuilder.CreateIndex(
                name: "IX_DossiersAcademiques_ClassePedagogiqueId",
                table: "DossiersAcademiques",
                column: "ClassePedagogiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_DossiersAcademiques_EtudiantId_ParcoursAcademiqueId",
                table: "DossiersAcademiques",
                columns: new[] { "EtudiantId", "ParcoursAcademiqueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DossiersAcademiques_ParcoursAcademiqueId",
                table: "DossiersAcademiques",
                column: "ParcoursAcademiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_DossiersAdministratifs_EtudiantId_ParcoursAcademiqueId",
                table: "DossiersAdministratifs",
                columns: new[] { "EtudiantId", "ParcoursAcademiqueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DossiersAdministratifs_ParcoursAcademiqueId",
                table: "DossiersAdministratifs",
                column: "ParcoursAcademiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_DossiersFinanciers_EtudiantId_ParcoursAcademiqueId",
                table: "DossiersFinanciers",
                columns: new[] { "EtudiantId", "ParcoursAcademiqueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DossiersFinanciers_ParcoursAcademiqueId",
                table: "DossiersFinanciers",
                column: "ParcoursAcademiqueId");

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
                name: "IX_ElementsConstitutifs_UniteEnseignementId_Code",
                table: "ElementsConstitutifs",
                columns: new[] { "UniteEnseignementId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ElementsScolariteEtudiants_DossierFinancierId_Statut",
                table: "ElementsScolariteEtudiants",
                columns: new[] { "DossierFinancierId", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_ElementsScolariteEtudiants_DossierFinancierId_TypeElementScolariteId",
                table: "ElementsScolariteEtudiants",
                columns: new[] { "DossierFinancierId", "TypeElementScolariteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElementsScolariteEtudiants_TypeElementScolariteId",
                table: "ElementsScolariteEtudiants",
                column: "TypeElementScolariteId");

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
                name: "IX_EvaluationsAcademiques_AnneeAcademiqueId_MaquetteElementConstitutifId_Type_Numero",
                table: "EvaluationsAcademiques",
                columns: new[] { "AnneeAcademiqueId", "MaquetteElementConstitutifId", "Type", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsAcademiques_EvaluationRemplaceeId",
                table: "EvaluationsAcademiques",
                column: "EvaluationRemplaceeId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsAcademiques_MaquetteElementConstitutifId",
                table: "EvaluationsAcademiques",
                column: "MaquetteElementConstitutifId");

            migrationBuilder.CreateIndex(
                name: "IX_Filieres_Code",
                table: "Filieres",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaquetteElementsConstitutifs_ElementConstitutifId",
                table: "MaquetteElementsConstitutifs",
                column: "ElementConstitutifId");

            migrationBuilder.CreateIndex(
                name: "IX_MaquetteElementsConstitutifs_SemestrePedagogiqueId_ElementConstitutifId",
                table: "MaquetteElementsConstitutifs",
                columns: new[] { "SemestrePedagogiqueId", "ElementConstitutifId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaquettesPedagogiques_ParcoursAcademiqueId_Code_Version",
                table: "MaquettesPedagogiques",
                columns: new[] { "ParcoursAcademiqueId", "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModesPaiementScolarite_Code",
                table: "ModesPaiementScolarite",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModesPaiementScolarite_EstActif_OrdreAffichage",
                table: "ModesPaiementScolarite",
                columns: new[] { "EstActif", "OrdreAffichage" });

            migrationBuilder.CreateIndex(
                name: "IX_NiveauxEtude_Numero",
                table: "NiveauxEtude",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotesEvaluations_DossierAcademiqueId",
                table: "NotesEvaluations",
                column: "DossierAcademiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_NotesEvaluations_EvaluationAcademiqueId_DossierAcademiqueId",
                table: "NotesEvaluations",
                columns: new[] { "EvaluationAcademiqueId", "DossierAcademiqueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationsScolarite_DossierFinancierId_Statut_DateGenerationUtc",
                table: "NotificationsScolarite",
                columns: new[] { "DossierFinancierId", "Statut", "DateGenerationUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationsScolarite_EcheanceScolariteId_Type",
                table: "NotificationsScolarite",
                columns: new[] { "EcheanceScolariteId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationsScolarite_ElementScolariteEtudiantId",
                table: "NotificationsScolarite",
                column: "ElementScolariteEtudiantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsScolarite_DossierFinancierId_DatePaiement",
                table: "PaiementsScolarite",
                columns: new[] { "DossierFinancierId", "DatePaiement" });

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsScolarite_DossierFinancierId_TypeElementScolariteId",
                table: "PaiementsScolarite",
                columns: new[] { "DossierFinancierId", "TypeElementScolariteId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsScolarite_ElementScolariteEtudiantId",
                table: "PaiementsScolarite",
                column: "ElementScolariteEtudiantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsScolarite_ModePaiementScolariteId",
                table: "PaiementsScolarite",
                column: "ModePaiementScolariteId");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsScolarite_ReferencePaiement",
                table: "PaiementsScolarite",
                column: "ReferencePaiement");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsScolarite_TarifScolariteId",
                table: "PaiementsScolarite",
                column: "TarifScolariteId");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsScolarite_TypeElementScolariteId",
                table: "PaiementsScolarite",
                column: "TypeElementScolariteId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcoursAcademiques_AnneeAcademiqueId_CycleFormationId_NiveauEtudeId_FiliereId_SpecialiteId",
                table: "ParcoursAcademiques",
                columns: new[] { "AnneeAcademiqueId", "CycleFormationId", "NiveauEtudeId", "FiliereId", "SpecialiteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParcoursAcademiques_Code",
                table: "ParcoursAcademiques",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParcoursAcademiques_CycleFormationId",
                table: "ParcoursAcademiques",
                column: "CycleFormationId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcoursAcademiques_FiliereId",
                table: "ParcoursAcademiques",
                column: "FiliereId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcoursAcademiques_NiveauEtudeId",
                table: "ParcoursAcademiques",
                column: "NiveauEtudeId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcoursAcademiques_SpecialiteId",
                table: "ParcoursAcademiques",
                column: "SpecialiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_ParentPermissionId_OrdreAffichage",
                table: "Permissions",
                columns: new[] { "ParentPermissionId", "OrdreAffichage" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcesVerbaux_AnneeAcademiqueId",
                table: "ProcesVerbaux",
                column: "AnneeAcademiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesVerbaux_ClassePedagogiqueId",
                table: "ProcesVerbaux",
                column: "ClassePedagogiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesVerbaux_ParcoursAcademiqueId_ClassePedagogiqueId_SemestrePedagogiqueId_Type_CodeSession",
                table: "ProcesVerbaux",
                columns: new[] { "ParcoursAcademiqueId", "ClassePedagogiqueId", "SemestrePedagogiqueId", "Type", "CodeSession" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcesVerbaux_SemestrePedagogiqueId",
                table: "ProcesVerbaux",
                column: "SemestrePedagogiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesVerbauxLignes_DossierAcademiqueId",
                table: "ProcesVerbauxLignes",
                column: "DossierAcademiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesVerbauxLignes_ProcesVerbalId_DossierAcademiqueId",
                table: "ProcesVerbauxLignes",
                columns: new[] { "ProcesVerbalId", "DossierAcademiqueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsAnnuels_AnneeAcademiqueId",
                table: "ResultatsAnnuels",
                column: "AnneeAcademiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsAnnuels_DossierAcademiqueId_AnneeAcademiqueId_NiveauEtudeId",
                table: "ResultatsAnnuels",
                columns: new[] { "DossierAcademiqueId", "AnneeAcademiqueId", "NiveauEtudeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsAnnuels_NiveauEtudeId",
                table: "ResultatsAnnuels",
                column: "NiveauEtudeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsElementsConstitutifs_DossierAcademiqueId_MaquetteElementConstitutifId",
                table: "ResultatsElementsConstitutifs",
                columns: new[] { "DossierAcademiqueId", "MaquetteElementConstitutifId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsElementsConstitutifs_MaquetteElementConstitutifId",
                table: "ResultatsElementsConstitutifs",
                column: "MaquetteElementConstitutifId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsSemestres_DossierAcademiqueId_SemestrePedagogiqueId",
                table: "ResultatsSemestres",
                columns: new[] { "DossierAcademiqueId", "SemestrePedagogiqueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsSemestres_SemestrePedagogiqueId",
                table: "ResultatsSemestres",
                column: "SemestrePedagogiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsUnitesEnseignement_DossierAcademiqueId_SemestrePedagogiqueId",
                table: "ResultatsUnitesEnseignement",
                columns: new[] { "DossierAcademiqueId", "SemestrePedagogiqueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultatsUnitesEnseignement_SemestrePedagogiqueId",
                table: "ResultatsUnitesEnseignement",
                column: "SemestrePedagogiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Code",
                table: "Roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemestresPedagogiques_MaquettePedagogiqueId_NumeroSemestre",
                table: "SemestresPedagogiques",
                columns: new[] { "MaquettePedagogiqueId", "NumeroSemestre" },
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
                name: "IX_TarifsScolarite_Code",
                table: "TarifsScolarite",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_TarifsScolarite_DateNotification",
                table: "TarifsScolarite",
                column: "DateNotification");

            migrationBuilder.CreateIndex(
                name: "IX_TarifsScolarite_ParcoursAcademiqueId",
                table: "TarifsScolarite",
                column: "ParcoursAcademiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_TarifsScolarite_TypeElementScolariteId_ParcoursAcademiqueId",
                table: "TarifsScolarite",
                columns: new[] { "TypeElementScolariteId", "ParcoursAcademiqueId" });

            migrationBuilder.CreateIndex(
                name: "IX_TypesElementsScolarite_Code",
                table: "TypesElementsScolarite",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitesEnseignement_Code",
                table: "UnitesEnseignement",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_NomUtilisateur",
                table: "Utilisateurs",
                column: "NomUtilisateur",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_RoleId",
                table: "Utilisateurs",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationsElementsScolarite_ElementScolariteEtudiantId_Statut",
                table: "ValidationsElementsScolarite",
                columns: new[] { "ElementScolariteEtudiantId", "Statut" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionInfos");

            migrationBuilder.DropTable(
                name: "AffectationsPaiementsEcheances");

            migrationBuilder.DropTable(
                name: "ContactsUrgence");

            migrationBuilder.DropTable(
                name: "DocumentsElementsScolarite");

            migrationBuilder.DropTable(
                name: "DocumentsPersonnels");

            migrationBuilder.DropTable(
                name: "DossiersAdministratifs");

            migrationBuilder.DropTable(
                name: "Etablissements");

            migrationBuilder.DropTable(
                name: "NotesEvaluations");

            migrationBuilder.DropTable(
                name: "NotificationsScolarite");

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
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Utilisateurs");

            migrationBuilder.DropTable(
                name: "ValidationsElementsScolarite");

            migrationBuilder.DropTable(
                name: "PaiementsScolarite");

            migrationBuilder.DropTable(
                name: "EvaluationsAcademiques");

            migrationBuilder.DropTable(
                name: "EcheancesScolarite");

            migrationBuilder.DropTable(
                name: "ProcesVerbaux");

            migrationBuilder.DropTable(
                name: "DossiersAcademiques");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "ModesPaiementScolarite");

            migrationBuilder.DropTable(
                name: "TarifsScolarite");

            migrationBuilder.DropTable(
                name: "MaquetteElementsConstitutifs");

            migrationBuilder.DropTable(
                name: "ElementsScolariteEtudiants");

            migrationBuilder.DropTable(
                name: "ClassesPedagogiques");

            migrationBuilder.DropTable(
                name: "ElementsConstitutifs");

            migrationBuilder.DropTable(
                name: "SemestresPedagogiques");

            migrationBuilder.DropTable(
                name: "DossiersFinanciers");

            migrationBuilder.DropTable(
                name: "TypesElementsScolarite");

            migrationBuilder.DropTable(
                name: "UnitesEnseignement");

            migrationBuilder.DropTable(
                name: "MaquettesPedagogiques");

            migrationBuilder.DropTable(
                name: "Etudiants");

            migrationBuilder.DropTable(
                name: "ParcoursAcademiques");

            migrationBuilder.DropTable(
                name: "AnneesAcademiques");

            migrationBuilder.DropTable(
                name: "CyclesFormation");

            migrationBuilder.DropTable(
                name: "NiveauxEtude");

            migrationBuilder.DropTable(
                name: "Specialites");

            migrationBuilder.DropTable(
                name: "Filieres");
        }
    }
}
