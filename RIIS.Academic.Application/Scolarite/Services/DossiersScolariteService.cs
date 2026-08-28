using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Scolarite.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Scolarite.Services;

public class DossiersScolariteService(
    IRepository<DossierFinancier> dossiersFinanciers,
    IRepository<DossierAdministratif> dossiersAdministratifs,
    IRepository<Etudiant> etudiants,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<AnneeAcademique> anneesAcademiques,
    IRepository<CycleFormation> cyclesFormation,
    IRepository<Filiere> filieres,
    IRepository<Specialite> specialites,
    IRepository<NiveauEtude> niveauxEtude,
    IRepository<TypeElementScolarite> typesElementsScolarite,
    IRepository<ElementScolariteEtudiant> elementsScolariteEtudiants,
    IRepository<DocumentElementScolarite> documentsElementsScolarite,
    IRepository<ValidationElementScolarite> validationsElementsScolarite,
    IRepository<PaiementScolarite> paiementsScolarite) : IDossiersScolariteService
{
    public async Task<List<DossierScolariteDto>> GetDossiersScolariteAsync(
        bool inclureInscriptionsSansDossier = true,
        string? recherche = null,
        string? anneeAcademiqueCode = null,
        string? cycleCode = null,
        int? niveauNumero = null,
        string? filiereCode = null,
        string? specialiteCode = null,
        long? parcoursAcademiqueId = null,
        string? matricule = null,
        CancellationToken cancellationToken = default)
    {
        var context = await LoadContextAsync(cancellationToken);
        var query = context.DossiersFinanciers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            query = query.Where(x =>
            {
                var etudiant = context.Etudiants.FirstOrDefault(e => e.Id == x.EtudiantId);
                var value = recherche.Trim();

                return Contains(etudiant?.Matricule, value)
                    || Contains(etudiant?.Nom, value)
                    || Contains(etudiant?.Prenoms, value);
            });
        }

        if (!string.IsNullOrWhiteSpace(matricule))
        {
            query = query.Where(x =>
            {
                var etudiant = context.Etudiants.FirstOrDefault(e => e.Id == x.EtudiantId);
                var value = matricule.Trim();

                return Contains(etudiant?.Matricule, value);
            });
        }

        query = query.Where(x =>
            (parcoursAcademiqueId is null || x.ParcoursAcademiqueId == parcoursAcademiqueId)
            && MatchParcours(context, x.ParcoursAcademiqueId, anneeAcademiqueCode, cycleCode, niveauNumero, filiereCode, specialiteCode));

        return query
            .Select(x => ToDto(x, context))
            .OrderBy(x => x.EtudiantNomComplet)
            .ThenByDescending(x => x.AnneeAcademiqueLibelle)
            .ToList();
    }

    public async Task<DossierScolariteDto?> GetDossierScolariteAsync(long id, CancellationToken cancellationToken = default)
    {
        var context = await LoadContextAsync(cancellationToken);
        var dossier = context.DossiersFinanciers.FirstOrDefault(x => x.Id == id);

        return dossier is null ? null : ToDto(dossier, context);
    }

    public async Task<DossierScolariteDto> GetOrCreateDossierDepuisInscriptionAsync(
        long inscriptionId,
        CancellationToken cancellationToken = default)
    {
        var dossier = await dossiersFinanciers.GetByIdAsync(inscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("Le dossier financier selectionne est introuvable.");

        var context = await LoadContextAsync(cancellationToken);
        return ToDto(dossier, context);
    }

    public async Task<AdministrationDossierScolariteDto?> GetAdministrationDossierScolariteAsync(
        long dossierScolariteId,
        CancellationToken cancellationToken = default)
    {
        var dossier = await dossiersFinanciers.GetByIdAsync(dossierScolariteId, cancellationToken);

        return dossier is null ? null : await BuildAdministrationDtoAsync(dossierScolariteId, cancellationToken);
    }

    public async Task<AdministrationDossierScolariteDto> SynchroniserDocumentsAdministratifsAsync(
        long dossierScolariteId,
        CancellationToken cancellationToken = default)
    {
        var dossier = await dossiersFinanciers.GetByIdAsync(dossierScolariteId, cancellationToken)
            ?? throw new InvalidOperationException("Le dossier financier selectionne est introuvable.");

        var types = await typesElementsScolarite.ListAsync(cancellationToken);
        var elements = await elementsScolariteEtudiants.ListAsync(cancellationToken);
        var existingTypeIds = elements
            .Where(x => x.DossierFinancierId == dossier.Id)
            .Select(x => x.TypeElementScolariteId)
            .ToHashSet();

        foreach (var type in types.Where(x => x.EstActif && (x.EstDocumentaire || x.EstPayable)).OrderBy(x => x.OrdreAffichage))
        {
            if (existingTypeIds.Contains(type.Id))
            {
                continue;
            }

            await elementsScolariteEtudiants.AddAsync(new ElementScolariteEtudiant
            {
                DossierFinancierId = dossier.Id,
                TypeElementScolariteId = type.Id,
                Code = type.Code,
                Libelle = type.Libelle,
                MontantAttendu = 0,
                MontantAffecte = 0,
                MontantRestant = 0,
                Statut = StatutElementScolarite.NonDemarre
            }, cancellationToken);
        }

        await elementsScolariteEtudiants.SaveChangesAsync(cancellationToken);
        return await RecalculerValidationAdministrativeAsync(dossier.Id, cancellationToken);
    }

    public async Task<AdministrationDossierScolariteDto> SaveDocumentAdministratifAsync(
        DocumentAdministratifDossierDto dto,
        CancellationToken cancellationToken = default)
    {
        var element = await elementsScolariteEtudiants.GetByIdAsync(dto.ElementScolariteEtudiantId, cancellationToken)
            ?? throw new InvalidOperationException("L'element administratif selectionne est introuvable.");

        var documents = await documentsElementsScolarite.ListAsync(cancellationToken);
        var document = dto.DocumentId is null
            ? documents.FirstOrDefault(x => x.ElementScolariteEtudiantId == element.Id)
            : documents.FirstOrDefault(x => x.Id == dto.DocumentId.Value);

        if (document is null)
        {
            await documentsElementsScolarite.AddAsync(new DocumentElementScolarite
            {
                ElementScolariteEtudiantId = element.Id,
                NomDocument = RequireText(dto.NomDocument, "Le nom du document est obligatoire."),
                UrlFichier = NormalizeText(dto.UrlFichier),
                Statut = dto.DocumentStatut,
                DateDepotUtc = dto.DateDepotUtc ?? DateTime.UtcNow,
                DateVerificationUtc = dto.DateVerificationUtc,
                VerifiePar = NormalizeText(dto.VerifiePar),
                Observation = NormalizeText(dto.Observation)
            }, cancellationToken);
        }
        else
        {
            document.NomDocument = RequireText(dto.NomDocument, "Le nom du document est obligatoire.");
            document.UrlFichier = NormalizeText(dto.UrlFichier);
            document.Statut = dto.DocumentStatut;
            document.DateDepotUtc = dto.DateDepotUtc ?? document.DateDepotUtc ?? DateTime.UtcNow;
            document.DateVerificationUtc = dto.DateVerificationUtc;
            document.VerifiePar = NormalizeText(dto.VerifiePar);
            document.Observation = NormalizeText(dto.Observation);
        }

        element.Statut = dto.EstComplet ? StatutElementScolarite.Valide : StatutElementScolarite.EnAttente;
        await elementsScolariteEtudiants.SaveChangesAsync(cancellationToken);

        return await RecalculerValidationAdministrativeAsync(element.DossierFinancierId, cancellationToken);
    }

    public async Task<AdministrationDossierScolariteDto> RecalculerValidationAdministrativeAsync(
        long dossierScolariteId,
        CancellationToken cancellationToken = default)
    {
        var dossier = await dossiersFinanciers.GetByIdAsync(dossierScolariteId, cancellationToken)
            ?? throw new InvalidOperationException("Le dossier financier selectionne est introuvable.");

        var administration = await BuildAdministrationDtoAsync(dossierScolariteId, cancellationToken);
        var paiementsActifs = (await paiementsScolarite.ListAsync(cancellationToken))
            .Where(x => x.DossierFinancierId == dossier.Id && x.Statut != StatutPaiementScolarite.Annule)
            .ToList();

        dossier.StatutAdministratif = administration.PiecesObligatoiresCompletes
            ? StatutDossierScolarite.Regulier
            : StatutDossierScolarite.Incomplet;
        dossier.StatutFinancier = paiementsActifs.Count > 0
            ? StatutDossierScolarite.EnCours
            : StatutDossierScolarite.EnPreparation;
        dossier.StatutGlobal = administration.SuiteParcoursAutorisee
            ? StatutDossierScolarite.EnCours
            : StatutDossierScolarite.Incomplet;
        dossier.DateDernierRecalculUtc = DateTime.UtcNow;

        await dossiersFinanciers.SaveChangesAsync(cancellationToken);
        return await BuildAdministrationDtoAsync(dossierScolariteId, cancellationToken);
    }

    private async Task<AdministrationDossierScolariteDto> BuildAdministrationDtoAsync(
        long dossierFinancierId,
        CancellationToken cancellationToken)
    {
        var elements = (await elementsScolariteEtudiants.ListAsync(cancellationToken))
            .Where(x => x.DossierFinancierId == dossierFinancierId)
            .ToList();
        var typeIds = elements.Select(x => x.TypeElementScolariteId).ToHashSet();
        var types = (await typesElementsScolarite.ListAsync(cancellationToken))
            .Where(x => typeIds.Contains(x.Id))
            .ToList();
        var documents = await documentsElementsScolarite.ListAsync(cancellationToken);
        var validations = await validationsElementsScolarite.ListAsync(cancellationToken);
        var paiements = (await paiementsScolarite.ListAsync(cancellationToken))
            .Where(x => x.DossierFinancierId == dossierFinancierId && x.Statut != StatutPaiementScolarite.Annule)
            .ToList();

        var documentDtos = elements
            .Select(element =>
            {
                var type = types.FirstOrDefault(x => x.Id == element.TypeElementScolariteId);
                var document = documents.FirstOrDefault(x => x.ElementScolariteEtudiantId == element.Id);
                var validation = validations.FirstOrDefault(x => x.ElementScolariteEtudiantId == element.Id);
                var estComplet = type?.EstDocumentaire != true || document?.Statut == StatutDocumentScolarite.Valide || element.Statut == StatutElementScolarite.Valide;

                return new DocumentAdministratifDossierDto
                {
                    ElementScolariteEtudiantId = element.Id,
                    TypeElementScolariteId = element.TypeElementScolariteId,
                    Code = element.Code,
                    Libelle = element.Libelle,
                    EstObligatoire = type?.EstObligatoire ?? false,
                    EstDocumentaire = type?.EstDocumentaire ?? false,
                    EstSoumisValidation = type?.EstSoumisValidation ?? false,
                    ElementStatut = element.Statut,
                    DocumentId = document?.Id,
                    NomDocument = document?.NomDocument ?? element.Libelle,
                    UrlFichier = document?.UrlFichier,
                    DocumentStatut = document?.Statut ?? StatutDocumentScolarite.EnAttente,
                    DateDepotUtc = document?.DateDepotUtc,
                    DateVerificationUtc = document?.DateVerificationUtc,
                    VerifiePar = document?.VerifiePar,
                    ValidationId = validation?.Id,
                    ValidationStatut = validation?.Statut ?? StatutValidationScolarite.EnAttente,
                    DateValidationUtc = validation?.DateValidationUtc,
                    ValidePar = validation?.ValidePar,
                    MotifRejet = validation?.MotifRejet,
                    Observation = document?.Observation ?? validation?.Observation ?? element.Observation,
                    EstComplet = estComplet
                };
            })
            .OrderBy(x => x.Libelle)
            .ToList();

        var obligatoires = documentDtos.Where(x => x.EstObligatoire && x.EstDocumentaire).ToList();
        var obligatoiresComplets = obligatoires.Count(x => x.EstComplet);
        var paiementInitialEffectue = paiements.Sum(x => x.Montant) > 0;

        return new AdministrationDossierScolariteDto
        {
            DossierFinancierId = dossierFinancierId,
            PiecesObligatoiresCompletes = obligatoires.Count == 0 || obligatoiresComplets == obligatoires.Count,
            PaiementInitialEffectue = paiementInitialEffectue,
            MontantPaiementsActifs = paiements.Sum(x => x.Montant),
            NombrePiecesObligatoires = obligatoires.Count,
            NombrePiecesObligatoiresCompletes = obligatoiresComplets,
            MotifAutorisation = paiementInitialEffectue
                ? "Paiement initial effectue."
                : "Pieces administratives obligatoires completes.",
            Documents = documentDtos
        };
    }

    private async Task<DossierScolariteContext> LoadContextAsync(CancellationToken cancellationToken)
        => new(
            await dossiersFinanciers.ListAsync(cancellationToken),
            await dossiersAdministratifs.ListAsync(cancellationToken),
            await etudiants.ListAsync(cancellationToken),
            await parcoursAcademiques.ListAsync(cancellationToken),
            await anneesAcademiques.ListAsync(cancellationToken),
            await cyclesFormation.ListAsync(cancellationToken),
            await filieres.ListAsync(cancellationToken),
            await specialites.ListAsync(cancellationToken),
            await niveauxEtude.ListAsync(cancellationToken));

    private static DossierScolariteDto ToDto(DossierFinancier dossier, DossierScolariteContext context)
    {
        var etudiant = context.Etudiants.FirstOrDefault(x => x.Id == dossier.EtudiantId);
        var parcours = context.Parcours.FirstOrDefault(x => x.Id == dossier.ParcoursAcademiqueId);
        var administratif = context.DossiersAdministratifs.FirstOrDefault(x =>
            x.EtudiantId == dossier.EtudiantId && x.ParcoursAcademiqueId == dossier.ParcoursAcademiqueId);
        var annee = parcours is null ? null : context.Annees.FirstOrDefault(x => x.Id == parcours.AnneeAcademiqueId);
        var cycle = parcours is null ? null : context.Cycles.FirstOrDefault(x => x.Id == parcours.CycleFormationId);
        var filiere = parcours is null ? null : context.Filieres.FirstOrDefault(x => x.Id == parcours.FiliereId);
        var specialite = parcours is null ? null : context.Specialites.FirstOrDefault(x => x.Id == parcours.SpecialiteId);
        var niveau = parcours is null ? null : context.Niveaux.FirstOrDefault(x => x.Id == parcours.NiveauEtudeId);

        return new DossierScolariteDto
        {
            Id = dossier.Id,
            InscriptionId = dossier.Id,
            ParcoursAcademiqueId = dossier.ParcoursAcademiqueId,
            EtudiantMatricule = etudiant?.Matricule ?? string.Empty,
            EtudiantNomComplet = etudiant is null ? string.Empty : $"{etudiant.Nom} {etudiant.Prenoms}".Trim(),
            DateInscription = DateOnly.FromDateTime(dossier.CreeLeUtc),
            AnneeAcademiqueCode = annee?.Libelle ?? string.Empty,
            AnneeAcademiqueLibelle = annee?.Libelle ?? string.Empty,
            CycleCode = cycle?.Code ?? string.Empty,
            CycleLibelle = cycle?.Libelle ?? string.Empty,
            FiliereCode = filiere?.Code ?? string.Empty,
            FiliereLibelle = filiere?.Libelle ?? string.Empty,
            SpecialiteCode = specialite?.Code,
            SpecialiteLibelle = specialite?.Libelle,
            NiveauNumero = niveau?.Numero ?? 0,
            NiveauLibelle = niveau?.Libelle ?? string.Empty,
            StatutAdministratif = administratif is null ? dossier.StatutAdministratif : MapAdministratif(administratif.Statut),
            StatutFinancier = dossier.StatutFinancier,
            StatutGlobal = dossier.StatutGlobal,
            DateCreationUtc = dossier.CreeLeUtc,
            DateDernierRecalculUtc = dossier.DateDernierRecalculUtc,
            Observation = dossier.Observation
        };
    }

    private static bool MatchParcours(
        DossierScolariteContext context,
        long parcoursAcademiqueId,
        string? anneeAcademiqueCode,
        string? cycleCode,
        int? niveauNumero,
        string? filiereCode,
        string? specialiteCode)
    {
        var parcours = context.Parcours.FirstOrDefault(x => x.Id == parcoursAcademiqueId);
        if (parcours is null)
        {
            return false;
        }

        var annee = context.Annees.FirstOrDefault(x => x.Id == parcours.AnneeAcademiqueId);
        var cycle = context.Cycles.FirstOrDefault(x => x.Id == parcours.CycleFormationId);
        var filiere = context.Filieres.FirstOrDefault(x => x.Id == parcours.FiliereId);
        var specialite = context.Specialites.FirstOrDefault(x => x.Id == parcours.SpecialiteId);
        var niveau = context.Niveaux.FirstOrDefault(x => x.Id == parcours.NiveauEtudeId);

        return MatchCode(annee?.Libelle, anneeAcademiqueCode)
            && MatchCode(cycle?.Code, cycleCode)
            && (niveauNumero is null || niveau?.Numero == niveauNumero)
            && MatchCode(filiere?.Code, filiereCode)
            && MatchCode(specialite?.Code, specialiteCode);
    }

    private static StatutDossierScolarite MapAdministratif(StatutDossierAdministratif statut)
        => statut switch
        {
            StatutDossierAdministratif.Valide => StatutDossierScolarite.Regulier,
            StatutDossierAdministratif.Incomplet => StatutDossierScolarite.Incomplet,
            StatutDossierAdministratif.Complet => StatutDossierScolarite.EnCours,
            _ => StatutDossierScolarite.EnPreparation
        };

    private static bool Contains(string? source, string value)
        => source?.Contains(value, StringComparison.OrdinalIgnoreCase) == true;

    private static bool MatchCode(string? source, string? expected)
        => string.IsNullOrWhiteSpace(expected) || string.Equals(source, expected, StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string RequireText(string? value, string errorMessage)
        => NormalizeText(value) ?? throw new InvalidOperationException(errorMessage);

    private sealed record DossierScolariteContext(
        List<DossierFinancier> DossiersFinanciers,
        List<DossierAdministratif> DossiersAdministratifs,
        List<Etudiant> Etudiants,
        List<ParcoursAcademique> Parcours,
        List<AnneeAcademique> Annees,
        List<CycleFormation> Cycles,
        List<Filiere> Filieres,
        List<Specialite> Specialites,
        List<NiveauEtude> Niveaux);
}
