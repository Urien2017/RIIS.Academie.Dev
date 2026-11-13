using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Dossiers.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Dossiers.Services;

public class DossierAcademiqueService(
    IRepository<DossierAcademique> dossiersAcademiques,
    IRepository<DossierEtudiant> dossiersEtudiants,
    IRepository<Etudiant> etudiants,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<AnneeAcademique> anneesAcademiques,
    IRepository<ClassePedagogique> classesPedagogiques) : IDossierAcademiqueService
{
    public async Task<DossierAcademiqueDto?> GetDossierAcademiqueAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await dossiersAcademiques.GetByIdAsync(id, cancellationToken);

        return entity is null ? null : await ToDtoAsync(entity, cancellationToken);
    }

    public async Task<DossierAcademiqueDto?> GetDossierAcademiqueAsync(
        long etudiantId,
        long parcoursAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        var entity = (await dossiersAcademiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == parcoursAcademiqueId);

        return entity is null ? null : await ToDtoAsync(entity, cancellationToken);
    }

    public async Task<DossierAcademiqueDto> GetOrCreateDossierAcademiqueAsync(
        long etudiantId,
        long parcoursAcademiqueId,
        long? classePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var existing = (await dossiersAcademiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == parcoursAcademiqueId);

        if (existing is not null)
        {
            if (classePedagogiqueId is not null && existing.ClassePedagogiqueId != classePedagogiqueId)
            {
                var tracked = await dossiersAcademiques.GetByIdAsync(existing.Id, cancellationToken);
                if (tracked is not null)
                {
                    tracked.ClassePedagogiqueId = classePedagogiqueId;
                    await dossiersAcademiques.SaveChangesAsync(cancellationToken);
                    existing = tracked;
                }
            }

            return await ToDtoAsync(existing, cancellationToken);
        }

        await EnsureReferencesAsync(etudiantId, parcoursAcademiqueId, classePedagogiqueId, cancellationToken);

        var entity = new DossierAcademique
        {
            EtudiantId = etudiantId,
            ParcoursAcademiqueId = parcoursAcademiqueId,
            ClassePedagogiqueId = classePedagogiqueId
        };

        await dossiersAcademiques.AddAsync(entity, cancellationToken);
        await dossiersAcademiques.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(entity, cancellationToken);
    }

    private async Task<DossierAcademiqueDto> ToDtoAsync(DossierAcademique entity, CancellationToken cancellationToken)
    {
        var etudiant = await etudiants.GetByIdAsync(entity.EtudiantId, cancellationToken);
        var parcours = await parcoursAcademiques.GetByIdAsync(entity.ParcoursAcademiqueId, cancellationToken);
        var annee = parcours is null ? null : await anneesAcademiques.GetByIdAsync(parcours.AnneeAcademiqueId, cancellationToken);
        var classe = entity.ClassePedagogiqueId is null ? null : await classesPedagogiques.GetByIdAsync(entity.ClassePedagogiqueId.Value, cancellationToken);
        var dossierEtudiant = (await dossiersEtudiants.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.EtudiantId == entity.EtudiantId && x.ParcoursAcademiqueId == entity.ParcoursAcademiqueId);

        return DossierAcademiqueDto.FromEntity(entity, etudiant, parcours, annee, classe, dossierEtudiant?.Matricule);
    }

    private async Task EnsureReferencesAsync(long etudiantId, long parcoursAcademiqueId, long? classePedagogiqueId, CancellationToken cancellationToken)
    {
        _ = await etudiants.GetByIdAsync(etudiantId, cancellationToken)
            ?? throw new InvalidOperationException("L'etudiant selectionne est introuvable.");
        _ = await parcoursAcademiques.GetByIdAsync(parcoursAcademiqueId, cancellationToken)
            ?? throw new InvalidOperationException("Le parcours academique selectionne est introuvable.");

        if (classePedagogiqueId is not null)
        {
            _ = await classesPedagogiques.GetByIdAsync(classePedagogiqueId.Value, cancellationToken)
                ?? throw new InvalidOperationException("La classe pedagogique selectionnee est introuvable.");
        }
    }
}
