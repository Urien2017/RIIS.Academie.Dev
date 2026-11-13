# Graph Report - .  (2026-08-20)

## Corpus Check
- 359 files · ~206,395 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2802 nodes · 5075 edges · 138 communities (133 shown, 5 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 10 edges (avg confidence: 0.68)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Pedagogique Programmes Programme
- Dashboard Academique Radzen
- Persistence Seeder Seeders
- Notes Persistence Seeder
- Scolarite Dossiers Radzen
- Scolarite Seeder Notification
- Evaluations Radzen Changed
- Scolarite Finances Radzen
- Evaluations Evaluation Ievaluations
- Configuration Persistence Configurations
- Dashboard Academique Notes
- Scolarite Dossiers Dossier
- Proces Verbaux Resultats
- Maquettes Programme Radzen
- Releves Resultats Radzen
- Enseignement Unites Programme
- Parcours Academiques Referentiels
- Scolarite Elements Types
- Etudiants Dossier Radzen
- Persistence Configurations Dossiers
- Scolarite Tarifs Radzen
- Notes Saisie Evaluations
- Elements Constitutifs Programme
- Specialites Referentiels Radzen
- Imports Microsoft Asp
- Microsoft Core Framework
- Semestres Programme Radzen
- Official Programme Seeder
- Proces Verbal Persistence
- Referentiels Save Delete
- Pedagogiques Classes Academics
- Annees Academiques Referentiels
- Cycles Formation Referentiels
- Niveaux Etude Referentiels
- Scolarite Paiement Modes
- Launch Settings Json
- Filieres Referentiels Radzen
- Dashboard Academique Evaluation
- Proces Verbal Word
- Proces Verbal Details
- Template Proces Verbal
- Notes Saisie Lookup
- Referentiels Ireferentiels Etude
- Scolarite Tarifs Tarif
- Dossier Etudiant Radzen
- Proces Verbal Excel
- Proces Verbal Export
- Pedagogiques Classes Classe
- Elements Types Type
- Releve Details Resultats
- Programme Persistence Unite
- Parcours Academique Referentiels
- Releve Template Note
- Configuration Proces Resultat
- Evaluation Seeder Persistence
- Persistence Irepository Abstractions
- Etudiants Generator Matricule
- Proces Verbaux Verbal
- Etudiants Admission Info
- Export Word Note
- Scolarite Paiement Mode
- Note Releve Word
- Scolarite Persistence Paiement
- Referentiels Academique Parcours
- Scolarite Finances Paiement
- Administratif Dossier Dossiers
- Financier Dossier Dossiers
- Releves Notes Calculer
- Resultat Notes Configuration
- Academique Dossier Dossiers
- Scolarite Dossier Finance
- Scolarite Type Element
- Pedagogiques Classes Evaluations
- Notes Calcul Icalcul
- Note Word Export
- Element Constitutif Configuration
- Persistence Migrations Add
- Dashboard Radzen Route
- Persistence Context Connection
- Proces Verbal Ligne
- Releve Note Resume
- Dossiers Dossier Academique
- Etudiant Dossier Dossiers
- Etudiants Etudiant Cancellation
- Scolarite Element Etudiant
- Scolarite Echeance Configuration
- Scolarite Paiement Configuration
- Layout Dashboard Radzen
- Verbaux Proces Iproces
- Affectation Paiement Scolarite
- Module Placeholder Radzen
- Attribute Embedded Validatable
- Initial Schema Migrations
- Dossier Administratif Dossiers
- Scolarite Document Element
- Initial Create Persistence
- Snapshot Context Migrations
- Side Bar Nav
- Releve Releves Note
- Paiement Mode Configuration
- Reconnect Modal Layout
- Dossier Financier Dossiers
- Fix Socle Migrations
- Add Scolarite Module
- Paiement Add Mode
- Add Code Tarifs
- Require Specialite For
- Replace Cycle Formation
- App Radzen Theme
- Absolute Txt Runtime
- Date Check Staticwebassets
- Database Initializer Persistence
- Database Resetter Persistence
- Routes Router Found
- Releve Note Ligne
- Error System Diagnostics
- Cycle Formation Referentiels
- Filiere Referentiels Create
- Specialite Referentiels Create
- Font Lato Ofl
- Absolute Txt Document
- Web Absolute Txt
- Layout Not Found

## God Nodes (most connected - your core abstractions)
1. `RIIS.Academic.Domain` - 152 edges
2. `LookupDto` - 49 edges
3. `ProgrammePedagogiqueService` - 47 edges
4. `RiisAcademicDbContext` - 46 edges
5. `ParcoursAcademique` - 38 edges
6. `RIIS.Academic.Infrastructure.Persistence.Configurations` - 38 edges
7. `ReferentielsService` - 37 edges
8. `IProgrammePedagogiqueService` - 33 edges
9. `DossierAcademique` - 33 edges
10. `DashboardAcademiqueService` - 31 edges

## Surprising Connections (you probably didn't know these)
- `DossierAcademiqueDto` --references--> `StatutDossierAcademique`  [EXTRACTED]
  RIIS.Academic.Application/Dossiers/Dtos/DossierAcademiqueDto.cs → RIIS.Academic.Domain/Enums/StatutDossierAcademique.cs
- `DossierAdministratifDto` --references--> `StatutDossierAdministratif`  [EXTRACTED]
  RIIS.Academic.Application/Dossiers/Dtos/DossierAdministratifDto.cs → RIIS.Academic.Domain/Enums/StatutDossierAdministratif.cs
- `DossierFinancierDto` --references--> `StatutDossierScolarite`  [EXTRACTED]
  RIIS.Academic.Application/Dossiers/Dtos/DossierFinancierDto.cs → RIIS.Academic.Domain/Enums/StatutDossierScolarite.cs
- `EtudiantDto` --references--> `AptitudeMedicale`  [EXTRACTED]
  RIIS.Academic.Application/Etudiants/Dtos/EtudiantDto.cs → RIIS.Academic.Domain/Enums/AptitudeMedicale.cs
- `EtudiantDto` --references--> `Sexe`  [EXTRACTED]
  RIIS.Academic.Application/Etudiants/Dtos/EtudiantDto.cs → RIIS.Academic.Domain/Enums/Sexe.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Clean Architecture Build Layers** — riis_academic_domain_obj_debug_net10_0_riis_academic_domain_csproj_filelistabsolute_build_outputs, riis_academic_application_obj_debug_net10_0_riis_academic_application_csproj_filelistabsolute_build_outputs, riis_academic_infrastructure_obj_debug_net10_0_riis_academic_infrastructure_csproj_filelistabsolute_build_outputs, riis_academic_obj_debug_net10_0_riis_academic_web_csproj_filelistabsolute_build_outputs [INFERRED 0.85]
- **Static Web Asset Pipeline** — riis_academic_obj_debug_net10_0_staticwebassets_uptodatecheck_file_list, riis_academic_obj_debug_net10_0_staticwebassets_uptodatecheck_bootstrap_assets, riis_academic_obj_debug_net10_0_staticwebassets_uptodatecheck_blazor_framework_scripts, riis_academic_obj_debug_net10_0_staticwebassets_uptodatecheck_app_css_assets, riis_academic_obj_debug_net10_0_staticwebassets_uptodatecheck_favicon_static_asset [INFERRED 0.95]

## Communities (138 total, 5 thin omitted)

### Community 0 - "Pedagogique Programmes Programme"
Cohesion: 0.05
Nodes (31): RIIS.Academic.Application.Programmes.Services, RIIS.Academic.Application.Programmes.Dtos, LookupDto, ElementConstitutifDto, ElementConstitutifHierarchyDto, DateOnly, MaquettePedagogiqueDto, DateOnly (+23 more)

### Community 1 - "Dashboard Academique Radzen"
Cohesion: 0.03
Nodes (63): ClasseSyntheseDashboard, DashboardFilterOption, DashboardParcoursFilterOption, EcRisqueDashboard, EvaluationProgressPoint, HeaderTemplate, RadzenAreaSeries, RadzenCategoryAxis (+55 more)

### Community 2 - "Persistence Seeder Seeders"
Cohesion: 0.09
Nodes (30): FiliereDefinition, Nom, Prenoms, ICollection, AnneeAcademique, EntityTypeBuilder, AnneeAcademiqueConfiguration, CancellationToken (+22 more)

### Community 3 - "Notes Persistence Seeder"
Cohesion: 0.07
Nodes (33): NotesSectionSeedData, NotesSeedData, ParsedNote, ProgrammeSeedContext, ElementConstitutif, EntityTypeBuilder, ElementConstitutifConfiguration, CancellationToken (+25 more)

### Community 4 - "Scolarite Dossiers Radzen"
Cohesion: 0.03
Nodes (57): CodeLibelleOption, DisplayAutorisationParcours, DisplayExistence, DisplayParcours, DisplayStatut, FiliereBelongsToCycle, FilterMatches, FormatCodeLibelle (+49 more)

### Community 5 - "Scolarite Seeder Notification"
Cohesion: 0.05
Nodes (20): RIIS.Academic.Domain, RIIS.Academic.Infrastructure.Persistence.Seeders, SaisieNoteLigneDto, DateTime, AuditableEntity, Entity, CanalNotificationScolarite, StatutDossierAcademique (+12 more)

### Community 6 - "Evaluations Radzen Changed"
Cohesion: 0.04
Nodes (55): Cancel, Create, Delete, Edit, EnsureSelectedValueIsAvailable, GetDefaultPonderation, GetShortTypeLibelle, GetTypeCode (+47 more)

### Community 7 - "Scolarite Finances Radzen"
Cohesion: 0.04
Nodes (54): CodeLibelleOption, DisplayParcours, DisplayStatut, EnregistrerPaiementLibre, FiliereBelongsToCycle, FilterMatches, FormatCodeLibelle, FormatMoney (+46 more)

### Community 8 - "Evaluations Evaluation Ievaluations"
Cohesion: 0.11
Nodes (17): DateTime, EvaluationAcademiqueDto, CancellationToken, IReadOnlyCollection, List, Task, EvaluationsService, CancellationToken (+9 more)

### Community 9 - "Configuration Persistence Configurations"
Cohesion: 0.05
Nodes (25): RIIS.Academic.Infrastructure.Persistence.Configurations, IEntityTypeConfiguration, Etablissement, EntityTypeBuilder, DossierAcademiqueConfiguration, EntityTypeBuilder, EtablissementConfiguration, EntityTypeBuilder (+17 more)

### Community 10 - "Dashboard Academique Notes"
Cohesion: 0.15
Nodes (22): DashboardData, DashboardGroupeKey, GroupeDashboard, DashboardAcademiqueFilterDto, CancellationToken, IEnumerable, IReadOnlyCollection, List (+14 more)

### Community 11 - "Scolarite Dossiers Dossier"
Cohesion: 0.12
Nodes (18): DossierScolariteContext, DateTime, List, AdministrationDossierScolariteDto, DocumentAdministratifDossierDto, DateOnly, DateTime, DossierScolariteDto (+10 more)

### Community 12 - "Proces Verbaux Resultats"
Cohesion: 0.05
Nodes (42): ApplySearch, Contains, EnsureSelectedValueIsAvailable, ExportExcel, ExportTemplateWord, ExportWord, FormatCredit, FormatNote (+34 more)

### Community 13 - "Maquettes Programme Radzen"
Cohesion: 0.05
Nodes (41): ApplyFilter, BuildVersionOptions, Cancel, Create, Delete, Edit, Load, LoadLookups (+33 more)

### Community 14 - "Releves Resultats Radzen"
Cohesion: 0.05
Nodes (39): RadzenPanel, ApplySearch, Contains, ExportTemplateWord, ExportWord, FormatCredit, FormatNote, Load (+31 more)

### Community 15 - "Enseignement Unites Programme"
Cohesion: 0.05
Nodes (39): ApplyFilters, CancelUe, CreateUe, DeleteUe, EditUe, EnsureSelectedValueIsAvailable, Load, NotifyError (+31 more)

### Community 16 - "Parcours Academiques Referentiels"
Cohesion: 0.05
Nodes (38): Cancel, Create, Delete, Edit, LoadParcours, LoadReferentiels, NotifyError, NotifySuccess (+30 more)

### Community 17 - "Scolarite Elements Types"
Cohesion: 0.05
Nodes (37): CategorieOption, Cancel, CategorieOption, Create, Delete, DisplayCategorie, Edit, Load (+29 more)

### Community 18 - "Etudiants Dossier Radzen"
Cohesion: 0.05
Nodes (37): Cancel, Create, Delete, Edit, Load, NotifyError, NotifySuccess, OnDateNaissanceChanged (+29 more)

### Community 19 - "Persistence Configurations Dossiers"
Cohesion: 0.06
Nodes (22): DbContext, DbSet, AdmissionInfo, DateTime, DocumentPersonnel, AptitudeMedicale, Sexe, ContactUrgence (+14 more)

### Community 20 - "Scolarite Tarifs Radzen"
Cohesion: 0.05
Nodes (36): Cancel, Create, Delete, Edit, LoadTarifs, NotifyError, NotifySuccess, OnInitializedAsync (+28 more)

### Community 21 - "Notes Saisie Evaluations"
Cohesion: 0.06
Nodes (35): EnsureSelectedValueIsAvailable, LoadGrille, NotifyError, NotifySuccess, OnAnneeAcademiqueChanged, OnEvaluationFilterChanged, OnInitializedAsync, OnPresenceChanged (+27 more)

### Community 22 - "Elements Constitutifs Programme"
Cohesion: 0.06
Nodes (35): Cancel, Create, Delete, Edit, Load, LoadLookups, NotifyError, NotifySuccess (+27 more)

### Community 23 - "Specialites Referentiels Radzen"
Cohesion: 0.06
Nodes (35): Cancel, Create, Delete, Edit, GetFiliereIdsForCycle, Load, NotifyError, NotifySuccess (+27 more)

### Community 24 - "Imports Microsoft Asp"
Cohesion: 0.06
Nodes (33): RIIS.Academic.Extensions, Microsoft.AspNetCore.Components, Microsoft.AspNetCore.Components.Routing, Microsoft.AspNetCore.Components.Web, Radzen, Radzen.Blazor, RIIS.Academic.Application.ClassesPedagogiques.Dtos, RIIS.Academic.Application.ClassesPedagogiques.Services (+25 more)

### Community 25 - "Microsoft Core Framework"
Cohesion: 0.07
Nodes (31): ClosedXML (0.105.0), DocumentFormat.OpenXml (3.5.1), FluentValidation (12.1.1), FluentValidation.DependencyInjectionExtensions (12.1.1), Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.11), Microsoft.Extensions.Configuration.EnvironmentVariables (10.0.11), Microsoft.Extensions.Configuration.Json (10.0.11), NodaTime (3.3.2) (+23 more)

### Community 26 - "Semestres Programme Radzen"
Cohesion: 0.06
Nodes (32): Cancel, Create, Delete, Edit, Load, LoadLookups, NotifyError, NotifySuccess (+24 more)

### Community 27 - "Official Programme Seeder"
Cohesion: 0.16
Nodes (18): OfficialElementConstitutifData, OfficialProgrammeData, OfficialProgrammesSeedData, OfficialSemestreData, OfficialUniteEnseignementData, CancellationToken, DateOnly, IReadOnlyCollection (+10 more)

### Community 28 - "Proces Verbal Persistence"
Cohesion: 0.10
Nodes (18): byte, ElementConstitutifContext, JsonSerializerOptions, DecisionAcademique, ProcesVerbalLigne, EntityTypeBuilder, ProcesVerbalLigneConfiguration, CancellationToken (+10 more)

### Community 29 - "Referentiels Save Delete"
Cohesion: 0.20
Nodes (4): CancellationToken, List, Task, ReferentielsService

### Community 30 - "Pedagogiques Classes Academics"
Cohesion: 0.06
Nodes (30): Cancel, Create, Delete, Edit, Load, LoadLookups, NotifyError, NotifySuccess (+22 more)

### Community 31 - "Annees Academiques Referentiels"
Cohesion: 0.06
Nodes (30): Cancel, Create, Delete, Edit, Load, NotifyError, NotifySuccess, OnInitializedAsync (+22 more)

### Community 32 - "Cycles Formation Referentiels"
Cohesion: 0.06
Nodes (30): Cancel, Create, Delete, Edit, Load, NotifyError, NotifySuccess, OnInitializedAsync (+22 more)

### Community 33 - "Niveaux Etude Referentiels"
Cohesion: 0.06
Nodes (30): Cancel, Create, Delete, Edit, Load, NotifyError, NotifySuccess, OnInitializedAsync (+22 more)

### Community 34 - "Scolarite Paiement Modes"
Cohesion: 0.06
Nodes (30): Cancel, Create, Delete, Edit, Load, NotifyError, NotifySuccess, OnInitializedAsync (+22 more)

### Community 35 - "Launch Settings Json"
Cohesion: 0.07
Nodes (30): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+22 more)

### Community 36 - "Filieres Referentiels Radzen"
Cohesion: 0.07
Nodes (29): Cancel, Create, Delete, Edit, Load, NotifyError, NotifySuccess, OnInitializedAsync (+21 more)

### Community 37 - "Dashboard Academique Evaluation"
Cohesion: 0.09
Nodes (16): RIIS.Academic.Application.Dashboard.Dtos, RIIS.Academic.Application.Dashboard.Services, DateTime, List, DashboardAcademiqueDto, DashboardClasseSyntheseDto, DashboardEcRisqueDto, DashboardEvaluationCompletionDto (+8 more)

### Community 38 - "Proces Verbal Word"
Cohesion: 0.15
Nodes (8): CancellationToken, IReadOnlyCollection, Label, string, Task, Value, ZipArchive, ProcesVerbalWordExportService

### Community 39 - "Proces Verbal Details"
Cohesion: 0.07
Nodes (27): BackToList, BuildEcHeaders, ExportExcel, ExportTemplateWord, ExportWord, FormatCredit, FormatNote, GetNoteCssClass (+19 more)

### Community 40 - "Template Proces Verbal"
Cohesion: 0.15
Nodes (7): CancellationToken, IReadOnlyCollection, Task, XDocument, XElement, XNamespace, ProcesVerbalTemplateWordExportService

### Community 41 - "Notes Saisie Lookup"
Cohesion: 0.19
Nodes (10): List, SaisieNotesGrilleDto, CancellationToken, List, Task, ISaisieNotesService, CancellationToken, List (+2 more)

### Community 42 - "Referentiels Ireferentiels Etude"
Cohesion: 0.22
Nodes (5): NiveauEtudeDto, CancellationToken, List, Task, IReferentielsService

### Community 43 - "Scolarite Tarifs Tarif"
Cohesion: 0.19
Nodes (10): TarifScolariteDto, CancellationToken, List, Task, ITarifsScolariteService, CancellationToken, IReadOnlyCollection, List (+2 more)

### Community 44 - "Dossier Etudiant Radzen"
Cohesion: 0.08
Nodes (25): Blank, FormatDate, LoadDossier, OnInitializedAsync, OnSelectionChanged, EtudiantDto, IDossierEtudiantService, IEtudiantsService (+17 more)

### Community 45 - "Proces Verbal Excel"
Cohesion: 0.18
Nodes (8): CancellationToken, IReadOnlyCollection, List, string, Task, ZipArchive, ProcesVerbalExcelExportService, StringBuilder

### Community 46 - "Proces Verbal Export"
Cohesion: 0.10
Nodes (14): RIIS.Academic.Application.ProcesVerbaux.Dtos, IEndpointRouteBuilder, ProcesVerbalExcelExportDto, ProcesVerbalWordExportDto, CancellationToken, Task, IProcesVerbalExcelExportService, CancellationToken (+6 more)

### Community 47 - "Pedagogiques Classes Classe"
Cohesion: 0.20
Nodes (10): ClassePedagogiqueDto, CancellationToken, IReadOnlyCollection, List, Task, ClassesPedagogiquesService, CancellationToken, List (+2 more)

### Community 48 - "Elements Types Type"
Cohesion: 0.16
Nodes (9): TypeElementScolariteDto, CancellationToken, List, Task, ITypesElementsScolariteService, CancellationToken, List, Task (+1 more)

### Community 49 - "Releve Details Resultats"
Cohesion: 0.08
Nodes (24): BackToList, ExportTemplateWord, ExportWord, FormatCredit, FormatNote, GetDecisionBadgeStyle, OnParametersSetAsync, Columns (+16 more)

### Community 50 - "Programme Persistence Unite"
Cohesion: 0.18
Nodes (14): BtsProgrammeLine, IGrouping, ICollection, UniteEnseignement, EntityTypeBuilder, UniteEnseignementConfiguration, CancellationToken, IEnumerable (+6 more)

### Community 51 - "Parcours Academique Referentiels"
Cohesion: 0.17
Nodes (17): ParcoursPlanItem, IReadOnlyCollection, ICollection, ParcoursAcademique, ICollection, CycleFormation, ICollection, Filiere (+9 more)

### Community 52 - "Releve Template Note"
Cohesion: 0.19
Nodes (6): IReadOnlyCollection, List, XDocument, XElement, XNamespace, ReleveNoteTemplateWordExportService

### Community 53 - "Configuration Proces Resultat"
Cohesion: 0.10
Nodes (13): DateTime, ResultatUniteEnseignement, DateTime, ICollection, ProcesVerbal, ICollection, SemestrePedagogique, EntityTypeBuilder (+5 more)

### Community 54 - "Evaluation Seeder Persistence"
Cohesion: 0.14
Nodes (14): CancellationToken, EvaluationDefinition, IReadOnlyList, RiisAcademicDbContext, string, Task, EvaluationDefinition, EvaluationSeeder (+6 more)

### Community 55 - "Persistence Irepository Abstractions"
Cohesion: 0.18
Nodes (8): CancellationToken, List, Task, IRepository, CancellationToken, List, Task, EfRepository

### Community 56 - "Etudiants Generator Matricule"
Cohesion: 0.13
Nodes (10): RIIS.Academic.Application.Etudiants.Services, RIIS.Academic.Application.Etudiants.Dtos, int, CancellationToken, Task, IMatriculeGenerator, CancellationToken, string (+2 more)

### Community 57 - "Proces Verbaux Verbal"
Cohesion: 0.24
Nodes (7): JsonElement, CancellationToken, IReadOnlyCollection, List, Task, ProcesVerbauxService, TypeProcesVerbal

### Community 58 - "Etudiants Admission Info"
Cohesion: 0.16
Nodes (10): DateTime, AdmissionInfoDto, DocumentPersonnelDto, DateOnly, List, EtudiantDto, CancellationToken, List (+2 more)

### Community 59 - "Export Word Note"
Cohesion: 0.20
Nodes (6): RIIS.Academic.Application.ProcesVerbaux.Services, RIIS.Academic.Application.Releves.Services, RIIS.Academic.Application.Abstractions.Services, RIIS.Academic.Infrastructure.Documents, RIIS.Academic.Application.Releves.Dtos, System.Globalization

### Community 60 - "Scolarite Paiement Mode"
Cohesion: 0.19
Nodes (9): ModePaiementScolariteDto, CancellationToken, List, Task, IModesPaiementScolariteService, CancellationToken, List, Task (+1 more)

### Community 61 - "Note Releve Word"
Cohesion: 0.25
Nodes (5): IReadOnlyCollection, Label, string, Value, ReleveNoteWordExportService

### Community 62 - "Scolarite Persistence Paiement"
Cohesion: 0.21
Nodes (4): RIIS.Academic.Application.Scolarite.Dtos, RIIS.Academic.Application.Abstractions.Persistence, RIIS.Academic.Infrastructure.Persistence.Repositories, RIIS.Academic.Application.Scolarite.Services

### Community 63 - "Referentiels Academique Parcours"
Cohesion: 0.12
Nodes (6): RIIS.Academic.Application.Referentiels.Dtos, RIIS.Academic.Application.Referentiels.Services, AnneeAcademiqueDto, FiliereLookupDto, ParcoursAcademiqueDto, ParcoursFormationDto

### Community 64 - "Scolarite Finances Paiement"
Cohesion: 0.30
Nodes (5): CancellationToken, IReadOnlyCollection, List, Task, FinancesScolariteService

### Community 65 - "Administratif Dossier Dossiers"
Cohesion: 0.27
Nodes (8): DateTime, DossierAdministratifDto, CancellationToken, Task, DossierAdministratifService, CancellationToken, Task, IDossierAdministratifService

### Community 66 - "Financier Dossier Dossiers"
Cohesion: 0.27
Nodes (8): DateTime, DossierFinancierDto, CancellationToken, Task, DossierFinancierService, CancellationToken, Task, IDossierFinancierService

### Community 67 - "Releves Notes Calculer"
Cohesion: 0.26
Nodes (5): CancellationToken, IReadOnlyCollection, List, Task, RelevesNotesService

### Community 68 - "Resultat Notes Configuration"
Cohesion: 0.14
Nodes (9): StatutValidationAcademique, DateTime, ResultatAnnuel, DateTime, ResultatSemestre, EntityTypeBuilder, ResultatAnnuelConfiguration, EntityTypeBuilder (+1 more)

### Community 69 - "Academique Dossier Dossiers"
Cohesion: 0.29
Nodes (8): DateTime, DossierAcademiqueDto, CancellationToken, Task, DossierAcademiqueService, CancellationToken, Task, IDossierAcademiqueService

### Community 70 - "Scolarite Dossier Finance"
Cohesion: 0.24
Nodes (11): DateOnly, List, EcheanceFinanciereDossierDto, ElementFinancierDossierDto, FinanceDossierScolariteDto, PaiementLibreDossierDto, PaiementTypeElementTarifOptionDto, CancellationToken (+3 more)

### Community 71 - "Scolarite Type Element"
Cohesion: 0.15
Nodes (8): CategorieTypeElementScolarite, TarifScolarite, ICollection, TypeElementScolarite, EntityTypeBuilder, TarifScolariteConfiguration, EntityTypeBuilder, TypeElementScolariteConfiguration

### Community 72 - "Pedagogiques Classes Evaluations"
Cohesion: 0.19
Nodes (5): RIIS.Academic.Application.ClassesPedagogiques.Services, RIIS.Academic.Application.Evaluations.Dtos, RIIS.Academic.Application.ClassesPedagogiques.Dtos, RIIS.Academic.Application.Common.Dtos, RIIS.Academic.Application.Evaluations.Services

### Community 73 - "Notes Calcul Icalcul"
Cohesion: 0.15
Nodes (4): RIIS.Academic.Application.Notes.Services, RIIS.Academic.Application.Notes.Dtos, ICalculNotesService, CalculNotesService

### Community 74 - "Note Word Export"
Cohesion: 0.14
Nodes (9): ReleveNoteWordExportDto, CancellationToken, Task, IReleveNoteTemplateWordExportService, CancellationToken, Task, IReleveNoteWordExportService, CancellationToken (+1 more)

### Community 75 - "Element Constitutif Configuration"
Cohesion: 0.16
Nodes (8): DateTime, ResultatElementConstitutif, ICollection, MaquetteElementConstitutif, EntityTypeBuilder, ResultatElementConstitutifConfiguration, EntityTypeBuilder, MaquetteElementConstitutifConfiguration

### Community 76 - "Persistence Migrations Add"
Cohesion: 0.21
Nodes (5): RIIS.Academic.Infrastructure.Persistence.Migrations, RIIS.Academic.Infrastructure.Persistence, MigrationBuilder, ModelBuilder, AddRefonteQoder

### Community 77 - "Dashboard Radzen Route"
Cohesion: 0.15
Nodes (12): DashboardSection, DashboardShortcut, Open, NavigationManager, RadzenButton, RadzenCard, RadzenColumn, RadzenIcon (+4 more)

### Community 78 - "Persistence Context Connection"
Cohesion: 0.17
Nodes (7): IDesignTimeDbContextFactory, IServiceCollection, IConfiguration, ConnectionStringHelper, IConfiguration, DependencyInjection, RiisAcademicDbContextFactory

### Community 79 - "Proces Verbal Ligne"
Cohesion: 0.20
Nodes (8): DateTime, List, ProcesVerbalDto, ProcesVerbalElementConstitutifLigneDto, List, ProcesVerbalLigneDto, List, List

### Community 80 - "Releve Note Resume"
Cohesion: 0.18
Nodes (7): DateOnly, List, ReleveNoteAnnuelDto, ReleveNoteResumeDto, CancellationToken, Task, ZipArchive

### Community 82 - "Etudiant Dossier Dossiers"
Cohesion: 0.31
Nodes (7): DossierEtudiantAnnuelDto, CancellationToken, Task, DossierEtudiantService, CancellationToken, Task, IDossierEtudiantService

### Community 83 - "Etudiants Etudiant Cancellation"
Cohesion: 0.35
Nodes (4): CancellationToken, List, Task, EtudiantsService

### Community 84 - "Scolarite Element Etudiant"
Cohesion: 0.20
Nodes (6): StatutElementScolarite, DateTime, ICollection, ElementScolariteEtudiant, EntityTypeBuilder, ElementScolariteEtudiantConfiguration

### Community 85 - "Scolarite Echeance Configuration"
Cohesion: 0.20
Nodes (7): StatutEcheanceScolarite, DateOnly, DateTime, ICollection, EcheanceScolarite, EntityTypeBuilder, EcheanceScolariteConfiguration

### Community 86 - "Scolarite Paiement Configuration"
Cohesion: 0.20
Nodes (7): StatutPaiementScolarite, DateOnly, DateTime, ICollection, PaiementScolarite, EntityTypeBuilder, PaiementScolariteConfiguration

### Community 87 - "Layout Dashboard Radzen"
Cohesion: 0.20
Nodes (9): LayoutComponentBase, RadzenBody, RadzenHeader, RadzenLayout, RadzenSidebarToggle, RadzenStack, RadzenText, ToggleSidebar (+1 more)

### Community 88 - "Verbaux Proces Iproces"
Cohesion: 0.51
Nodes (4): CancellationToken, List, Task, IProcesVerbauxService

### Community 89 - "Affectation Paiement Scolarite"
Cohesion: 0.22
Nodes (6): DateTime, AffectationPaiementDossierDto, DateTime, AffectationPaiementEcheance, EntityTypeBuilder, AffectationPaiementEcheanceConfiguration

### Community 90 - "Module Placeholder Radzen"
Cohesion: 0.20
Nodes (9): FormatWord, HumanizeSegment, RadzenAlert, RadzenButton, RadzenCard, RadzenIcon, RadzenStack, RadzenText (+1 more)

### Community 91 - "Attribute Embedded Validatable"
Cohesion: 0.22
Nodes (5): Attribute, Microsoft.Extensions.Validation.Embedded, Microsoft.CodeAnalysis, EmbeddedAttribute, ValidatableTypeAttribute

### Community 92 - "Initial Schema Migrations"
Cohesion: 0.28
Nodes (4): RIIS.Academic.Infrastructure.Migrations, MigrationBuilder, ModelBuilder, InitialAcademicSchema

### Community 93 - "Dossier Administratif Dossiers"
Cohesion: 0.25
Nodes (5): DateTime, DossierAdministratif, StatutDossierAdministratif, EntityTypeBuilder, DossierAdministratifConfiguration

### Community 94 - "Scolarite Document Element"
Cohesion: 0.25
Nodes (5): StatutDocumentScolarite, DateTime, DocumentElementScolarite, EntityTypeBuilder, DocumentElementScolariteConfiguration

### Community 95 - "Initial Create Persistence"
Cohesion: 0.29
Nodes (4): Migration, MigrationBuilder, ModelBuilder, InitialCreate

### Community 96 - "Snapshot Context Migrations"
Cohesion: 0.25
Nodes (5): ModelSnapshot, ModelBuilder, RiisAcademicDbContextModelSnapshot, ModelBuilder, RiisAcademicDbContextModelSnapshot

### Community 97 - "Side Bar Nav"
Cohesion: 0.25
Nodes (7): RadzenPanelMenu, RadzenPanelMenuItem, RadzenSidebar, RadzenCard, RadzenIcon, RadzenStack, RadzenText

### Community 98 - "Releve Releves Note"
Cohesion: 0.32
Nodes (5): ReleveNoteEtudiantDisponibleDto, CancellationToken, List, Task, IRelevesNotesService

### Community 99 - "Paiement Mode Configuration"
Cohesion: 0.29
Nodes (4): ICollection, ModePaiementScolarite, EntityTypeBuilder, ModePaiementScolariteConfiguration

### Community 100 - "Reconnect Modal Layout"
Cohesion: 0.32
Nodes (6): handleReconnectStateChanged(), reconnectModal, resumeButton, retry(), retryButton, retryWhenDocumentBecomesVisible()

### Community 101 - "Dossier Financier Dossiers"
Cohesion: 0.29
Nodes (5): DateTime, ICollection, DossierFinancier, EntityTypeBuilder, DossierFinancierConfiguration

### Community 102 - "Fix Socle Migrations"
Cohesion: 0.29
Nodes (3): MigrationBuilder, ModelBuilder, FixDbSocle

### Community 103 - "Add Scolarite Module"
Cohesion: 0.29
Nodes (3): MigrationBuilder, ModelBuilder, AddScolariteModule

### Community 104 - "Paiement Add Mode"
Cohesion: 0.29
Nodes (3): MigrationBuilder, ModelBuilder, AddModePaiementAndPaiementReferences

### Community 105 - "Add Code Tarifs"
Cohesion: 0.29
Nodes (3): MigrationBuilder, ModelBuilder, AddCodeToTarifsScolarite

### Community 106 - "Require Specialite For"
Cohesion: 0.29
Nodes (3): MigrationBuilder, ModelBuilder, RequireSpecialiteForParcours

### Community 107 - "Replace Cycle Formation"
Cohesion: 0.29
Nodes (3): MigrationBuilder, ModelBuilder, ReplaceCycleFormationFiliereWithParcoursAcademique

### Community 108 - "App Radzen Theme"
Cohesion: 0.29
Nodes (6): HeadOutlet, ImportMap, RadzenTheme, ReconnectModal, ResourcePreloader, Routes

### Community 109 - "Absolute Txt Runtime"
Cohesion: 0.29
Nodes (7): RIIS Academic Application Assembly, RIIS Academic Application Build Outputs, Domain Runtime Dependency, RIIS Academic Application File List, RIIS Academic Domain Build Outputs, RIIS Academic Domain Assembly, RIIS Academic Domain File List

### Community 110 - "Date Check Staticwebassets"
Cohesion: 0.29
Nodes (7): Application CSS Assets, Blazor Framework Scripts, Bootstrap Static Assets, Favicon Static Asset, Static Web Assets Up To Date Check, Favicon Image, Purple At Symbol Brand Mark

### Community 111 - "Database Initializer Persistence"
Cohesion: 0.33
Nodes (4): IServiceProvider, CancellationToken, Task, DatabaseInitializer

### Community 112 - "Database Resetter Persistence"
Cohesion: 0.53
Nodes (3): CancellationToken, Task, RiisAcademicDatabaseResetter

### Community 113 - "Routes Router Found"
Cohesion: 0.40
Nodes (4): FocusOnNavigate, Found, Router, RouteView

### Community 114 - "Releve Note Ligne"
Cohesion: 0.40
Nodes (3): ReleveNoteLigneDto, List, ReleveNoteSemestreDto

### Community 115 - "Error System Diagnostics"
Cohesion: 0.50
Nodes (3): PageTitle, OnInitialized, System.Diagnostics

### Community 119 - "Font Lato Ofl"
Cohesion: 0.50
Nodes (4): Font Use Modify Redistribute Permissions, Lato Font Software, SIL Open Font License Document, SIL Open Font License

### Community 120 - "Absolute Txt Document"
Cohesion: 0.50
Nodes (4): RIIS Academic Infrastructure Build Outputs, Document Templates, RIIS Academic Infrastructure File List, RIIS Academic Infrastructure Assembly

### Community 121 - "Web Absolute Txt"
Cohesion: 0.50
Nodes (4): RIIS Academic Web Build Outputs, RIIS Academic Web File List, Web Runtime Dependencies, Web Static Web Assets

## Knowledge Gaps
- **1098 isolated node(s):** `GroupeDashboard`, `DashboardGroupeKey`, `DashboardData`, `net10.0`, `Microsoft.NET.Sdk` (+1093 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **5 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `RIIS.Academic.Domain` connect `Scolarite Seeder Notification` to `Pedagogique Programmes Programme`, `Persistence Seeder Seeders`, `Notes Persistence Seeder`, `Evaluations Evaluation Ievaluations`, `Configuration Persistence Configurations`, `Scolarite Dossiers Dossier`, `Persistence Configurations Dossiers`, `Proces Verbal Persistence`, `Dashboard Academique Evaluation`, `Proces Verbal Export`, `Elements Types Type`, `Programme Persistence Unite`, `Configuration Proces Resultat`, `Etudiants Generator Matricule`, `Proces Verbaux Verbal`, `Etudiants Admission Info`, `Export Word Note`, `Scolarite Persistence Paiement`, `Referentiels Academique Parcours`, `Administratif Dossier Dossiers`, `Financier Dossier Dossiers`, `Resultat Notes Configuration`, `Scolarite Dossier Finance`, `Scolarite Type Element`, `Pedagogiques Classes Evaluations`, `Notes Calcul Icalcul`, `Element Constitutif Configuration`, `Persistence Migrations Add`, `Proces Verbal Ligne`, `Dossiers Dossier Academique`, `Scolarite Element Etudiant`, `Scolarite Echeance Configuration`, `Scolarite Paiement Configuration`, `Affectation Paiement Scolarite`, `Dossier Administratif Dossiers`, `Scolarite Document Element`, `Paiement Mode Configuration`, `Dossier Financier Dossiers`?**
  _High betweenness centrality (0.130) - this node is a cross-community bridge._
- **Why does `RiisAcademicDbContext` connect `Persistence Configurations Dossiers` to `Pedagogique Programmes Programme`, `Persistence Seeder Seeders`, `Notes Persistence Seeder`, `Scolarite Seeder Notification`, `Configuration Persistence Configurations`, `Dashboard Academique Notes`, `Proces Verbal Persistence`, `Programme Persistence Unite`, `Parcours Academique Referentiels`, `Configuration Proces Resultat`, `Resultat Notes Configuration`, `Scolarite Type Element`, `Element Constitutif Configuration`, `Persistence Migrations Add`, `Persistence Context Connection`, `Scolarite Element Etudiant`, `Scolarite Echeance Configuration`, `Scolarite Paiement Configuration`, `Affectation Paiement Scolarite`, `Dossier Administratif Dossiers`, `Scolarite Document Element`, `Paiement Mode Configuration`, `Dossier Financier Dossiers`, `Database Resetter Persistence`?**
  _High betweenness centrality (0.066) - this node is a cross-community bridge._
- **Why does `RIIS.Academic.Infrastructure.Persistence` connect `Persistence Migrations Add` to `Snapshot Context Migrations`, `Fix Socle Migrations`, `Add Scolarite Module`, `Paiement Add Mode`, `Add Code Tarifs`, `Require Specialite For`, `Replace Cycle Formation`, `Persistence Context Connection`, `Database Initializer Persistence`, `Database Resetter Persistence`, `Imports Microsoft Asp`, `Export Word Note`, `Initial Schema Migrations`?**
  _High betweenness centrality (0.049) - this node is a cross-community bridge._
- **What connects `GroupeDashboard`, `DashboardGroupeKey`, `DashboardData` to the rest of the system?**
  _1098 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Pedagogique Programmes Programme` be split into smaller, more focused modules?**
  _Cohesion score 0.052941176470588235 - nodes in this community are weakly interconnected._
- **Should `Dashboard Academique Radzen` be split into smaller, more focused modules?**
  _Cohesion score 0.03125 - nodes in this community are weakly interconnected._
- **Should `Persistence Seeder Seeders` be split into smaller, more focused modules?**
  _Cohesion score 0.09152542372881356 - nodes in this community are weakly interconnected._