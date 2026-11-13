using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Dossiers.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Dossiers.Services;

public class DossierFinancierService(
    IRepository<DossierFinancier> dossiersFinanciers,
    IRepository<DossierEtudiant> dossiersEtudiants,
    IRepository<Etudiant> etudiants,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<AnneeAcademique> anneesAcademiques) : IDossierFinancierService
{
    public async Task<DossierFinancierDto?> GetDossierFinancierAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await dossiersFinanciers.GetByIdAsync(id, cancellationToken);

        return entity is null ? null : await ToDtoAsync(entity, cancellationToken);
    }

    public async Task<DossierFinancierDto?> GetDossierFinancierAsync(
        long etudiantId,
        long parcoursAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        var entity = (await dossiersFinanciers.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == parcoursAcademiqueId);

        return entity is null ? null : await ToDtoAsync(entity, cancellationToken);
    }

    public async Task<DossierFinancierDto> GetOrCreateDossierFinancierAsync(
        long etudiantId,
        long parcoursAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetDossierFinancierAsync(etudiantId, parcoursAcademiqueId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        await EnsureReferencesAsync(etudiantId, parcoursAcademiqueId, cancellationToken);

        var entity = new DossierFinancier
        {
            EtudiantId = etudiantId,
            ParcoursAcademiqueId = parcoursAcademiqueId
        };

        await dossiersFinanciers.AddAsync(entity, cancellationToken);
        await dossiersFinanciers.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(entity, cancellationToken);
    }

    private async Task<DossierFinancierDto> ToDtoAsync(DossierFinancier entity, CancellationToken cancellationToken)
    {
        var etudiant = await etudiants.GetByIdAsync(entity.EtudiantId, cancellationToken);
        var parcours = await parcoursAcademiques.GetByIdAsync(entity.ParcoursAcademiqueId, cancellationToken);
        var annee = parcours is null ? null : await anneesAcademiques.GetByIdAsync(parcours.AnneeAcademiqueId, cancellationToken);
        var dossierEtudiant = (await dossiersEtudiants.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.EtudiantId == entity.EtudiantId && x.ParcoursAcademiqueId == entity.ParcoursAcademiqueId);

        return DossierFinancierDto.FromEntity(entity, etudiant, parcours, annee, dossierEtudiant?.Matricule);
    }

    private async Task EnsureReferencesAsync(long etudiantId, long parcoursAcademiqueId, CancellationToken cancellationToken)
    {
        _ = await etudiants.GetByIdAsync(etudiantId, cancellationToken)
            ?? throw new InvalidOperationException("L'etudiant selectionne est introuvable.");
        _ = await parcoursAcademiques.GetByIdAsync(parcoursAcademiqueId, cancellationToken)
            ?? throw new InvalidOperationException("Le parcours academique selectionne est introuvable.");
    }
}
