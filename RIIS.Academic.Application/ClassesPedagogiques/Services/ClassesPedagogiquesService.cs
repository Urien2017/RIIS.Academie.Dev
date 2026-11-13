using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.ClassesPedagogiques.Dtos;
using RIIS.Academic.Application.Common.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.ClassesPedagogiques.Services;

public class ClassesPedagogiquesService(
    IRepository<ClassePedagogique> classesPedagogiques,
    IRepository<ParcoursAcademique> parcoursAcademiques) : IClassesPedagogiquesService
{
    public async Task<List<ClassePedagogiqueDto>> GetClassesPedagogiquesAsync(
        CancellationToken cancellationToken = default)
    {
        var classes = await classesPedagogiques.ListAsync(cancellationToken);
        var ouvertures = await parcoursAcademiques.ListAsync(cancellationToken);

        return classes
            .Select(classe => ToDto(classe, ouvertures))
            .OrderBy(x => x.ParcoursAcademiqueLibelle)
            .ThenBy(x => x.Libelle)
            .ToList();
    }

    public async Task<ClassePedagogiqueDto?> GetClassePedagogiqueAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var classes = await classesPedagogiques.ListAsync(cancellationToken);
        var classe = classes.FirstOrDefault(x => x.Id == id);

        if (classe is null)
        {
            return null;
        }

        var ouvertures = await parcoursAcademiques.ListAsync(cancellationToken);

        return ToDto(classe, ouvertures);
    }

    public Task<ClassePedagogiqueDto> CreateDefaultClassePedagogiqueAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ClassePedagogiqueDto());
    }

    public async Task SaveClassePedagogiqueAsync(
        ClassePedagogiqueDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.ParcoursAcademiqueId <= 0)
        {
            throw new InvalidOperationException("Le parcours cycle/filière/spécialité de la classe est obligatoire.");
        }

        dto.Libelle = RequireText(dto.Libelle, "Le libellé de la classe est obligatoire.");

        var ouvertureExists = (await parcoursAcademiques.ListAsync(cancellationToken)).Any(x => x.Id == dto.ParcoursAcademiqueId);
        if (!ouvertureExists)
        {
            throw new InvalidOperationException("Le parcours cycle/filière/spécialité sélectionné est introuvable.");
        }

        var existingClasses = await classesPedagogiques.ListAsync(cancellationToken);
        var duplicate = existingClasses.FirstOrDefault(x =>
            x.Id != dto.Id
            && x.ParcoursAcademiqueId == dto.ParcoursAcademiqueId);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                "Ce programme académique possède déjà une classe pédagogique. Modifiez la classe existante au lieu d'en créer une seconde.");
        }

        if (dto.Id == 0)
        {
            throw new InvalidOperationException(
                "Les classes pédagogiques sont créées automatiquement avec leur programme académique.");
        }
        else
        {
            var entity = await classesPedagogiques.GetByIdAsync(dto.Id, cancellationToken);
            if (entity is null) return;

            entity.ParcoursAcademiqueId = dto.ParcoursAcademiqueId;
            entity.Libelle = dto.Libelle;
            entity.Effectif = dto.Effectif;
        }

        await classesPedagogiques.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureClassesForParcoursAsync(CancellationToken cancellationToken = default)
    {
        var parcours = await parcoursAcademiques.ListAsync(cancellationToken);
        var classes = await classesPedagogiques.ListAsync(cancellationToken);
        var classesParParcours = classes
            .GroupBy(x => x.ParcoursAcademiqueId)
            .ToDictionary(x => x.Key, x => x.First());

        var changements = false;

        foreach (var programme in parcours)
        {
            if (classesParParcours.ContainsKey(programme.Id))
            {
                continue;
            }

            await classesPedagogiques.AddAsync(new ClassePedagogique
            {
                ParcoursAcademiqueId = programme.Id,
                Libelle = BuildClasseLibelle(programme),
                Effectif = 0
            }, cancellationToken);

            changements = true;
        }

        if (changements)
        {
            await classesPedagogiques.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteClassePedagogiqueAsync(long id, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Une classe pédagogique est automatiquement liée à son programme académique et ne peut pas être supprimée seule.");
    }

    public async Task<List<LookupDto>> GetParcoursAcademiquesLookupAsync(CancellationToken cancellationToken = default)
    {
        var items = await parcoursAcademiques.ListAsync(cancellationToken);

        return items
            .Where(x => x.EstActive)
            .OrderBy(x => x.Libelle)
            .Select(x => new LookupDto { Id = x.Id, Libelle = FormatCodeLibelle(x.Code, x.Libelle) })
            .ToList();
    }

    private static ClassePedagogiqueDto ToDto(
        ClassePedagogique classe,
        IReadOnlyCollection<ParcoursAcademique> ouvertures)
    {
        var ouverture = ouvertures.FirstOrDefault(x => x.Id == classe.ParcoursAcademiqueId);

        return new ClassePedagogiqueDto
        {
            Id = classe.Id,
            ParcoursAcademiqueId = classe.ParcoursAcademiqueId,
            ParcoursAcademiqueLibelle = ouverture is null
                ? string.Empty
                : FormatCodeLibelle(ouverture.Code, ouverture.Libelle),
            Libelle = classe.Libelle,
            Effectif = classe.Effectif
        };
    }

    private static string FormatCodeLibelle(string code, string libelle)
        => string.IsNullOrWhiteSpace(code) ? libelle : $"{code} - {libelle}";

    private static string BuildClasseLibelle(ParcoursAcademique programme)
    {
        var libelle = string.IsNullOrWhiteSpace(programme.Libelle)
            ? programme.Code
            : programme.Libelle;

        return libelle.Length <= 200 ? libelle : libelle[..200];
    }

    private static string RequireText(string? value, string errorMessage)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return normalized;
    }
}
