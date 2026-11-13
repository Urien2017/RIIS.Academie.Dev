using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Dossiers.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Dossiers.Services;

public class DossierAdministratifService(
    IRepository<DossierAdministratif> dossiersAdministratifs,
    IRepository<DossierEtudiant> dossiersEtudiants,
    IRepository<Etudiant> etudiants,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<AnneeAcademique> anneesAcademiques) : IDossierAdministratifService
{
    public async Task<DossierAdministratifDto?> GetDossierAdministratifAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await dossiersAdministratifs.GetByIdAsync(id, cancellationToken);

        return entity is null ? null : await ToDtoAsync(entity, cancellationToken);
    }

    public async Task<DossierAdministratifDto?> GetDossierAdministratifAsync(
        long etudiantId,
        long parcoursAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        var entity = (await dossiersAdministratifs.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == parcoursAcademiqueId);

        return entity is null ? null : await ToDtoAsync(entity, cancellationToken);
    }

    public async Task<DossierAdministratifDto> GetOrCreateDossierAdministratifAsync(
        long etudiantId,
        long parcoursAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetDossierAdministratifAsync(etudiantId, parcoursAcademiqueId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        await EnsureReferencesAsync(etudiantId, parcoursAcademiqueId, cancellationToken);

        var entity = new DossierAdministratif
        {
            EtudiantId = etudiantId,
            ParcoursAcademiqueId = parcoursAcademiqueId
        };

        await dossiersAdministratifs.AddAsync(entity, cancellationToken);
        await dossiersAdministratifs.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(entity, cancellationToken);
    }

    private async Task<DossierAdministratifDto> ToDtoAsync(DossierAdministratif entity, CancellationToken cancellationToken)
    {
        var etudiant = await etudiants.GetByIdAsync(entity.EtudiantId, cancellationToken);
        var parcours = await parcoursAcademiques.GetByIdAsync(entity.ParcoursAcademiqueId, cancellationToken);
        var annee = parcours is null ? null : await anneesAcademiques.GetByIdAsync(parcours.AnneeAcademiqueId, cancellationToken);
        var dossierEtudiant = (await dossiersEtudiants.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.EtudiantId == entity.EtudiantId && x.ParcoursAcademiqueId == entity.ParcoursAcademiqueId);

        return DossierAdministratifDto.FromEntity(entity, etudiant, parcours, annee, dossierEtudiant?.Matricule);
    }

    private async Task EnsureReferencesAsync(long etudiantId, long parcoursAcademiqueId, CancellationToken cancellationToken)
    {
        _ = await etudiants.GetByIdAsync(etudiantId, cancellationToken)
            ?? throw new InvalidOperationException("L'etudiant selectionne est introuvable.");
        _ = await parcoursAcademiques.GetByIdAsync(parcoursAcademiqueId, cancellationToken)
            ?? throw new InvalidOperationException("Le parcours academique selectionne est introuvable.");
    }
}
