using RIIS.Academic.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace RIIS.Academic.Infrastructure.Persistence;

public abstract class RiisAcademicModelDbContext(DbContextOptions options)
    : DbContext(options)
{
    public DbSet<Etablissement> Etablissements => Set<Etablissement>();
    public DbSet<Etudiant> Etudiants => Set<Etudiant>();
    public DbSet<ContactUrgence> ContactsUrgence => Set<ContactUrgence>();
    public DbSet<AdmissionInfo> AdmissionInfos => Set<AdmissionInfo>();
    public DbSet<DocumentPersonnel> DocumentsPersonnels => Set<DocumentPersonnel>();
    public DbSet<DossierAdministratif> DossiersAdministratifs => Set<DossierAdministratif>();
    public DbSet<DossierAcademique> DossiersAcademiques => Set<DossierAcademique>();
    public DbSet<DossierFinancier> DossiersFinanciers => Set<DossierFinancier>();
    public DbSet<DossierEtudiant> DossiersEtudiants => Set<DossierEtudiant>();
    public DbSet<AnneeAcademique> AnneesAcademiques => Set<AnneeAcademique>();
    public DbSet<CycleFormation> CyclesFormation => Set<CycleFormation>();
    public DbSet<ParcoursAcademique> ParcoursAcademiques => Set<ParcoursAcademique>();
    public DbSet<NiveauEtude> NiveauxEtude => Set<NiveauEtude>();
    public DbSet<Filiere> Filieres => Set<Filiere>();
    public DbSet<Specialite> Specialites => Set<Specialite>();
    public DbSet<MaquettePedagogique> MaquettesPedagogiques => Set<MaquettePedagogique>();
    public DbSet<AffectationMaquette> AffectationsMaquettes => Set<AffectationMaquette>();
    public DbSet<SemestrePedagogique> SemestresPedagogiques => Set<SemestrePedagogique>();
    public DbSet<UniteEnseignement> UnitesEnseignement => Set<UniteEnseignement>();
    public DbSet<ElementConstitutif> ElementsConstitutifs => Set<ElementConstitutif>();
    public DbSet<MaquetteElementConstitutif> MaquetteElementsConstitutifs => Set<MaquetteElementConstitutif>();
    public DbSet<CahierTexte> CahiersTextes => Set<CahierTexte>();
    public DbSet<CahierTextePage> CahiersTextesPages => Set<CahierTextePage>();
    public DbSet<SeanceCours> SeancesCours => Set<SeanceCours>();
    public DbSet<ClassePedagogique> ClassesPedagogiques => Set<ClassePedagogique>();
    public DbSet<EvaluationAcademique> EvaluationsAcademiques => Set<EvaluationAcademique>();
    public DbSet<NoteEvaluation> NotesEvaluations => Set<NoteEvaluation>();
    public DbSet<ResultatElementConstitutif> ResultatsElementsConstitutifs => Set<ResultatElementConstitutif>();
    public DbSet<ResultatUniteEnseignement> ResultatsUnitesEnseignement => Set<ResultatUniteEnseignement>();
    public DbSet<ResultatSemestre> ResultatsSemestres => Set<ResultatSemestre>();
    public DbSet<ResultatAnnuel> ResultatsAnnuels => Set<ResultatAnnuel>();
    public DbSet<ProcesVerbal> ProcesVerbaux => Set<ProcesVerbal>();
    public DbSet<ProcesVerbalLigne> ProcesVerbauxLignes => Set<ProcesVerbalLigne>();
    public DbSet<TypeElementScolarite> TypesElementsScolarite => Set<TypeElementScolarite>();
    public DbSet<ModePaiementScolarite> ModesPaiementScolarite => Set<ModePaiementScolarite>();
    public DbSet<TarifScolarite> TarifsScolarite => Set<TarifScolarite>();
    public DbSet<ElementScolariteEtudiant> ElementsScolariteEtudiants => Set<ElementScolariteEtudiant>();
    public DbSet<EcheanceScolarite> EcheancesScolarite => Set<EcheanceScolarite>();
    public DbSet<PaiementScolarite> PaiementsScolarite => Set<PaiementScolarite>();
    public DbSet<AffectationPaiementEcheance> AffectationsPaiementsEcheances => Set<AffectationPaiementEcheance>();
    public DbSet<DocumentElementScolarite> DocumentsElementsScolarite => Set<DocumentElementScolarite>();
    public DbSet<ValidationElementScolarite> ValidationsElementsScolarite => Set<ValidationElementScolarite>();
    public DbSet<NotificationScolarite> NotificationsScolarite => Set<NotificationScolarite>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RiisAcademicModelDbContext).Assembly);

        // SQL Server fournit rowversion; SQLite n'a pas d'equivalent natif.
        // La concurrence optimiste reste geree par l'application en mode SQLite.
        if (Database.IsSqlite())
        {
            modelBuilder.Entity<EvaluationAcademique>().ToTable(
                "EvaluationsAcademiques",
                table => table.HasCheckConstraint(
                    "CK_EvaluationsAcademiques_BaremePonderation",
                    "CAST([Bareme] AS REAL) > 0 AND CAST([PonderationPourcentage] AS REAL) >= 0 AND CAST([PonderationPourcentage] AS REAL) <= 100"));

            modelBuilder.Entity<NoteEvaluation>().ToTable(
                "NotesEvaluations",
                table => table.HasCheckConstraint(
                    "CK_NotesEvaluations_Valeur",
                    "[Valeur] IS NULL OR (CAST([Valeur] AS REAL) >= 0 AND CAST([Valeur] AS REAL) <= 20)"));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var version = entityType.FindProperty("Version");
                if (version?.ClrType == typeof(byte[]))
                {
                    version.ValueGenerated = ValueGenerated.Never;
                    version.IsConcurrencyToken = false;
                }
            }
        }
    }
}

public sealed class RiisAcademicDbContext(
    DbContextOptions<RiisAcademicDbContext> options)
    : RiisAcademicModelDbContext(options);
