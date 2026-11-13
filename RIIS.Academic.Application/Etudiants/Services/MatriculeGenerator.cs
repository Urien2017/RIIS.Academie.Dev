using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Etudiants.Services;

public class MatriculeGenerator(
    IRepository<DossierEtudiant> dossiersEtudiants,
    IRepository<AnneeAcademique> anneesAcademiques) : IMatriculeGenerator
{
    private const string Prefix = "MAT";
    private const int YearWidth = 2;
    private const int SequenceWidth = 5;

    public async Task<string> GenerateMatriculeAsync(
        long? anneeAcademiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
        var annee = anneeAcademiqueId is null
            ? anneeItems
                .Where(x => x.EstActive)
                .OrderByDescending(x => x.AnneeDebut)
                .FirstOrDefault()
            : anneeItems.FirstOrDefault(x => x.Id == anneeAcademiqueId.Value);

        if (annee is null)
        {
            throw new InvalidOperationException("Aucune année académique active n'est configurée pour générer le matricule.");
        }

        var yearCode = (annee.AnneeDebut % 100).ToString($"D{YearWidth}");
        var items = await dossiersEtudiants.ListAsync(cancellationToken);
        var used = items
            .Select(x => Normalize(x.Matricule))
            .Where(x => x is not null)
            .Select(x => x!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var nextSequence = items
            .Select(x => TryReadSequence(x.Matricule, yearCode))
            .Where(x => x is not null)
            .Select(x => x!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

        while (true)
        {
            var candidate = $"{Prefix}-{yearCode}-{nextSequence.ToString($"D{SequenceWidth}")}";
            if (!used.Contains(candidate))
            {
                return candidate;
            }

            nextSequence++;
        }
    }

    private static int? TryReadSequence(string? matricule, string yearCode)
    {
        var normalized = Normalize(matricule);
        var prefix = $"{Prefix}-{yearCode}-";
        if (normalized is null || !normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var suffix = normalized[prefix.Length..];

        return int.TryParse(suffix, out var value) ? value : null;
    }

    private static string? Normalize(string? matricule)
    {
        var normalized = matricule?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized.ToUpperInvariant();
    }
}
