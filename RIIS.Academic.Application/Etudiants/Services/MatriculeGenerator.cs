using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Etudiants.Services;

public class MatriculeGenerator(IRepository<Etudiant> etudiants) : IMatriculeGenerator
{
    private const string Prefix = "ETU";
    private const int Width = 6;

    public async Task<string> GenerateMatriculeAsync(CancellationToken cancellationToken = default)
    {
        var items = await etudiants.ListAsync(cancellationToken);
        var used = items
            .Select(x => Normalize(x.Matricule))
            .Where(x => x is not null)
            .Select(x => x!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var nextSequence = items
            .Select(x => TryReadSequence(x.Matricule))
            .Where(x => x is not null)
            .Select(x => x!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

        while (true)
        {
            var candidate = $"{Prefix}{nextSequence.ToString($"D{Width}")}";
            if (!used.Contains(candidate))
            {
                return candidate;
            }

            nextSequence++;
        }
    }

    private static int? TryReadSequence(string? matricule)
    {
        var normalized = Normalize(matricule);
        if (normalized is null || !normalized.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var suffix = normalized[Prefix.Length..];

        return int.TryParse(suffix, out var value) ? value : null;
    }

    private static string? Normalize(string? matricule)
    {
        var normalized = matricule?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized.ToUpperInvariant();
    }
}
