using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

public static class FiliereSpecialiteSeeder
{
    private const string ResourceName = "filieres-specialites.json";

    public static async Task SeedAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var data = await LoadSeedDataAsync(cancellationToken);
        var filieresByCode = new Dictionary<string, Filiere>(StringComparer.OrdinalIgnoreCase);

        foreach (var filiereData in data.Filieres)
        {
            var filiere = await context.Filieres
                .FirstOrDefaultAsync(x => x.Code == filiereData.Code, cancellationToken);

            if (filiere is null)
            {
                filiere = new Filiere
                {
                    Code = filiereData.Code,
                    Libelle = filiereData.Libelle,
                    EstActive = true
                };

                context.Filieres.Add(filiere);
            }
            else
            {
                filiere.Libelle = filiereData.Libelle;
                filiere.EstActive = true;
            }

            filieresByCode[filiereData.Code] = filiere;
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (var filiereData in data.Filieres)
        {
            var filiere = filieresByCode[filiereData.Code];

            foreach (var specialiteData in filiereData.Specialites)
            {
                var specialite = await context.Specialites
                    .FirstOrDefaultAsync(
                        x => x.FiliereId == filiere.Id && x.Code == specialiteData.Code,
                        cancellationToken);

                if (specialite is null)
                {
                    context.Specialites.Add(new Specialite
                    {
                        FiliereId = filiere.Id,
                        Code = specialiteData.Code,
                        Libelle = specialiteData.Libelle,
                        EstActive = true
                    });
                }
                else
                {
                    specialite.Libelle = specialiteData.Libelle;
                    specialite.EstActive = true;
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<FilieresSpecialitesSeedData> LoadSeedDataAsync(
        CancellationToken cancellationToken)
    {
        var assembly = typeof(FiliereSpecialiteSeeder).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(x => x.EndsWith(ResourceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException(
                $"La ressource embarquee '{ResourceName}' est introuvable.");

        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException(
                $"Impossible d'ouvrir la ressource embarquee '{resourceName}'.");

        var data = await JsonSerializer.DeserializeAsync<FilieresSpecialitesSeedData>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        return data ?? throw new InvalidOperationException(
            $"La ressource '{ResourceName}' est vide ou invalide.");
    }

    private sealed class FilieresSpecialitesSeedData
    {
        public required List<FiliereSeedData> Filieres { get; set; }
    }

    private sealed class FiliereSeedData
    {
        public required string Code { get; set; }
        public required string Libelle { get; set; }
        public required List<SpecialiteSeedData> Specialites { get; set; }
    }

    private sealed class SpecialiteSeedData
    {
        public required string Code { get; set; }
        public required string Libelle { get; set; }
    }
}
