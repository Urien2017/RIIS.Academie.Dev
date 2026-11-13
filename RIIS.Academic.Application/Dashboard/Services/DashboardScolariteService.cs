using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Dashboard.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Dashboard.Services;

public class DashboardScolariteService(
    IRepository<AnneeAcademique> anneesAcademiques,
    IRepository<CycleFormation> cyclesFormation,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<Etudiant> etudiants,
    IRepository<DossierFinancier> dossiersFinanciers,
    IRepository<DossierEtudiant> dossiersEtudiants,
    IRepository<ElementScolariteEtudiant> elementsScolariteEtudiants,
    IRepository<TypeElementScolarite> typesElementsScolarite,
    IRepository<TarifScolarite> tarifsScolarite,
    IRepository<PaiementScolarite> paiementsScolarite) : IDashboardScolariteService
{
    public async Task<DashboardScolariteDto> GetDashboardScolariteAsync(
        DashboardScolariteFilterDto? filter = null,
        CancellationToken cancellationToken = default)
    {
        filter ??= new DashboardScolariteFilterDto();

        var annees = await anneesAcademiques.ListAsync(cancellationToken);
        var cycles = await cyclesFormation.ListAsync(cancellationToken);
        var parcours = await parcoursAcademiques.ListAsync(cancellationToken);
        var dossiers = await dossiersFinanciers.ListAsync(cancellationToken);
        var elements = await elementsScolariteEtudiants.ListAsync(cancellationToken);
        var typesElements = await typesElementsScolarite.ListAsync(cancellationToken);
        var tarifs = await tarifsScolarite.ListAsync(cancellationToken);
        var paiements = await paiementsScolarite.ListAsync(cancellationToken);
        var etudiantsItems = await etudiants.ListAsync(cancellationToken);
        var dossierEtudiantItems = await dossiersEtudiants.ListAsync(cancellationToken);

        var parcoursFiltres = parcours.Where(x =>
            (filter.AnneeAcademiqueId is null || x.AnneeAcademiqueId == filter.AnneeAcademiqueId)
            && (filter.CycleFormationId is null || x.CycleFormationId == filter.CycleFormationId)
            && (filter.ParcoursAcademiqueId is null || x.Id == filter.ParcoursAcademiqueId))
            .ToDictionary(x => x.Id);

        var dossiersFiltres = dossiers
            .Where(x => parcoursFiltres.ContainsKey(x.ParcoursAcademiqueId))
            .ToList();
        var dossierIds = dossiersFiltres.Select(x => x.Id).ToHashSet();
        var elementsFiltres = elements.Where(x => dossierIds.Contains(x.DossierFinancierId)).ToList();
        var paiementsFiltres = paiements
            .Where(x => dossierIds.Contains(x.DossierFinancierId)
                && x.Statut != StatutPaiementScolarite.Annule)
            .ToList();
        var retardsPaiement = BuildRetardsPaiement(
            dossiersFiltres,
            parcoursFiltres,
            tarifs,
            paiementsFiltres,
            etudiantsItems,
            dossierEtudiantItems,
            typesElements);

        var totalAttendu = elementsFiltres.Sum(x => x.MontantAttendu);
        var totalRestant = elementsFiltres.Sum(x => x.MontantRestant);
        var totalEncaisse = paiementsFiltres.Sum(x => x.Montant);
        var totalAffecte = paiementsFiltres.Sum(x => x.MontantAffecte);
        var totalNonAffecte = paiementsFiltres.Sum(x => x.MontantNonAffecte);

        var result = new DashboardScolariteDto
        {
            Annees = annees.OrderByDescending(x => x.AnneeDebut).Select(x => new DashboardScolariteOptionDto { Id = x.Id, Libelle = x.Libelle }).ToList(),
            Cycles = cycles.OrderBy(x => x.OrdreAffichage).Select(x => new DashboardScolariteOptionDto { Id = x.Id, Libelle = x.Code + " - " + x.Libelle }).ToList(),
            ParcoursDisponibles = parcours.OrderBy(x => x.Libelle).Select(x => new DashboardScolariteOptionDto { Id = x.Id, Libelle = x.Code + " - " + x.Libelle }).ToList(),
            Kpi = new DashboardScolariteKpiDto
            {
                NombreDossiers = dossiersFiltres.Count,
                TotalAttendu = totalAttendu,
                TotalEncaisse = totalEncaisse,
                TotalAffecte = totalAffecte,
                TotalNonAffecte = totalNonAffecte,
                TotalRestant = totalRestant,
                TauxRecouvrement = Percent(totalEncaisse, totalAttendu),
                NombreDossiersSoldes = dossiersFiltres.Count(dossier =>
                    GetStatutDossier(dossier, elementsFiltres, paiementsFiltres) == StatutDossierFinance.Solde),
                NombreDossiersEnCours = dossiersFiltres.Count(dossier =>
                    GetStatutDossier(dossier, elementsFiltres, paiementsFiltres) == StatutDossierFinance.EnCours)
            },
            EvolutionEncaissements = paiementsFiltres
                .GroupBy(x => new DateOnly(x.DatePaiement.Year, x.DatePaiement.Month, 1))
                .OrderBy(x => x.Key)
                .Select(x => new DashboardScolariteEvolutionDto
                {
                    Periode = x.Key.ToString("MM/yyyy"),
                    Ordre = x.Key.Year * 100 + x.Key.Month,
                    Encaisse = x.Sum(p => p.Montant),
                    Affecte = x.Sum(p => p.MontantAffecte)
                }).ToList(),
            RepartitionModesPaiement = paiementsFiltres
                .GroupBy(x => string.IsNullOrWhiteSpace(x.ModePaiement) ? "Non précisé" : x.ModePaiement)
                .OrderByDescending(x => x.Sum(p => p.Montant))
                .Select(x => new DashboardScolariteModeDto { Libelle = x.Key, Montant = x.Sum(p => p.Montant), Nombre = x.Count() })
                .ToList(),
            RepartitionStatuts =
            [
                new() { Libelle = "Soldés", Nombre = dossiersFiltres.Count(d => GetStatutDossier(d, elementsFiltres, paiementsFiltres) == StatutDossierFinance.Solde) },
                new() { Libelle = "En cours", Nombre = dossiersFiltres.Count(d => GetStatutDossier(d, elementsFiltres, paiementsFiltres) == StatutDossierFinance.EnCours) },
                new() { Libelle = "Sans paiement", Nombre = dossiersFiltres.Count(d => GetStatutDossier(d, elementsFiltres, paiementsFiltres) == StatutDossierFinance.SansPaiement) }
            ],
            Parcours = parcoursFiltres.Values
                .Select(p =>
                {
                    var parcoursDossiers = dossiersFiltres.Where(d => d.ParcoursAcademiqueId == p.Id).ToList();
                    var ids = parcoursDossiers.Select(d => d.Id).ToHashSet();
                    var parcoursElements = elementsFiltres.Where(e => ids.Contains(e.DossierFinancierId)).ToList();
                    var parcoursPaiements = paiementsFiltres.Where(x => ids.Contains(x.DossierFinancierId)).ToList();
                    var attendu = parcoursElements.Sum(x => x.MontantAttendu);
                    return new DashboardScolariteParcoursDto
                    {
                        ParcoursAcademiqueId = p.Id,
                        Parcours = p.Code + " - " + p.Libelle,
                        NombreDossiers = parcoursDossiers.Count,
                        TotalAttendu = attendu,
                        TotalEncaisse = parcoursPaiements.Sum(x => x.Montant),
                        TotalRestant = parcoursElements.Sum(x => x.MontantRestant),
                        TauxRecouvrement = Percent(parcoursPaiements.Sum(x => x.Montant), attendu)
                    };
                })
                .Where(x => x.NombreDossiers > 0)
                .OrderByDescending(x => x.TotalEncaisse)
                .ToList(),
            PaiementsRecents = paiementsFiltres
                .OrderByDescending(x => x.DatePaiement)
                .ThenByDescending(x => x.Id)
                .Take(15)
                .Select(p =>
                {
                    var dossier = dossiersFiltres.First(x => x.Id == p.DossierFinancierId);
                    var etudiant = etudiantsItems.FirstOrDefault(x => x.Id == dossier.EtudiantId);
                    return new DashboardScolaritePaiementRecentDto
                    {
                        DatePaiement = p.DatePaiement,
                        Etudiant = etudiant is null
                            ? $"Dossier #{dossier.Id}"
                            : $"{etudiant.Nom} {etudiant.Prenoms}".Trim(),
                        Parcours = parcoursFiltres[dossier.ParcoursAcademiqueId].Libelle,
                        ModePaiement = p.ModePaiement,
                        Montant = p.Montant
                    };
                }).ToList(),
            RetardsPaiement = retardsPaiement
        };

        return result;
    }

    private static decimal Percent(decimal value, decimal total)
        => total <= 0 ? 0 : Math.Round(value * 100m / total, 2);

    private static StatutDossierFinance GetStatutDossier(
        DossierFinancier dossier,
        IReadOnlyCollection<ElementScolariteEtudiant> elements,
        IReadOnlyCollection<PaiementScolarite> paiements)
    {
        if (!paiements.Any(x => x.DossierFinancierId == dossier.Id))
        {
            return StatutDossierFinance.SansPaiement;
        }

        var restant = elements
            .Where(x => x.DossierFinancierId == dossier.Id)
            .Sum(x => x.MontantRestant);

        return restant <= 0
            ? StatutDossierFinance.Solde
            : StatutDossierFinance.EnCours;
    }

    private static List<DashboardScolariteRetardPaiementGroupeDto> BuildRetardsPaiement(
        IReadOnlyCollection<DossierFinancier> dossiers,
        IReadOnlyDictionary<long, ParcoursAcademique> parcours,
        IReadOnlyCollection<TarifScolarite> tarifs,
        IReadOnlyCollection<PaiementScolarite> paiements,
        IReadOnlyCollection<Etudiant> etudiants,
        IReadOnlyCollection<DossierEtudiant> dossiersEtudiants,
        IReadOnlyCollection<TypeElementScolarite> typesElementsScolarite)
    {
        var aujourdHui = DateOnly.FromDateTime(DateTime.Today);
        var typesParId = typesElementsScolarite.ToDictionary(x => x.Id);
        var tarifsApplicables = tarifs
            .Where(x => x.EstActif
                && x.DateNotification.HasValue
                && x.DateNotification.Value <= aujourdHui
                && parcours.ContainsKey(x.ParcoursAcademiqueId))
            .GroupBy(x => new { x.ParcoursAcademiqueId, x.TypeElementScolariteId })
            .Select(x => x
                .OrderByDescending(tarif => tarif.Priorite)
                .ThenByDescending(tarif => tarif.Id)
                .First())
            .ToList();

        var retards = new List<DashboardScolariteRetardPaiementEtudiantDtoWithType>();

        foreach (var tarif in tarifsApplicables)
        {
            var dateNotification = tarif.DateNotification!.Value;
            var dossiersDuParcours = dossiers
                .Where(x => x.ParcoursAcademiqueId == tarif.ParcoursAcademiqueId);

            foreach (var dossier in dossiersDuParcours)
            {
                var montantPayeAvantNotification = paiements
                    .Where(p => p.DossierFinancierId == dossier.Id
                        && p.DatePaiement <= dateNotification
                        && (p.TypeElementScolariteId == tarif.TypeElementScolariteId
                            || p.TarifScolariteId == tarif.Id))
                    .Sum(p => p.Montant);

                if (montantPayeAvantNotification >= tarif.Montant)
                {
                    continue;
                }

                var etudiant = etudiants.FirstOrDefault(x => x.Id == dossier.EtudiantId);
                var parcoursItem = parcours[tarif.ParcoursAcademiqueId];
                var type = typesParId.GetValueOrDefault(tarif.TypeElementScolariteId);

                retards.Add(new DashboardScolariteRetardPaiementEtudiantDtoWithType
                {
                    TypeElementScolariteId = tarif.TypeElementScolariteId,
                    TypeElementScolariteLibelle = type is null
                        ? $"Type #{tarif.TypeElementScolariteId}"
                        : FormatCodeLibelle(type.Code, type.Libelle),
                    DossierFinancierId = dossier.Id,
                    Matricule = dossiersEtudiants.FirstOrDefault(x =>
                        x.EtudiantId == dossier.EtudiantId
                        && x.ParcoursAcademiqueId == dossier.ParcoursAcademiqueId)?.Matricule ?? string.Empty,
                    Etudiant = etudiant is null
                        ? $"Dossier #{dossier.Id}"
                        : $"{etudiant.Nom} {etudiant.Prenoms}".Trim(),
                    Parcours = FormatCodeLibelle(parcoursItem.Code, parcoursItem.Libelle),
                    DateNotification = dateNotification,
                    MontantAttendu = tarif.Montant,
                    MontantPayeAvantNotification = montantPayeAvantNotification,
                    MontantRestant = Math.Max(0, tarif.Montant - montantPayeAvantNotification)
                });
            }
        }

        return retards
            .GroupBy(x => new { x.TypeElementScolariteId, x.TypeElementScolariteLibelle })
            .OrderBy(x => x.Key.TypeElementScolariteLibelle)
            .Select(x => new DashboardScolariteRetardPaiementGroupeDto
            {
                TypeElementScolariteId = x.Key.TypeElementScolariteId,
                TypeElementScolariteLibelle = x.Key.TypeElementScolariteLibelle,
                Etudiants = x
                    .OrderBy(y => y.Etudiant)
                    .Select(y => new DashboardScolariteRetardPaiementEtudiantDto
                    {
                        DossierFinancierId = y.DossierFinancierId,
                        Matricule = y.Matricule,
                        Etudiant = y.Etudiant,
                        Parcours = y.Parcours,
                        DateNotification = y.DateNotification,
                        MontantAttendu = y.MontantAttendu,
                        MontantPayeAvantNotification = y.MontantPayeAvantNotification,
                        MontantRestant = y.MontantRestant
                    })
                    .ToList()
            })
            .ToList();
    }

    private static string FormatCodeLibelle(string code, string libelle)
        => string.IsNullOrWhiteSpace(code) ? libelle : $"{code} - {libelle}";

    private enum StatutDossierFinance
    {
        Solde,
        EnCours,
        SansPaiement
    }

    private sealed class DashboardScolariteRetardPaiementEtudiantDtoWithType
        : DashboardScolariteRetardPaiementEtudiantDto
    {
        public long TypeElementScolariteId { get; set; }
        public string TypeElementScolariteLibelle { get; set; } = string.Empty;
    }
}
