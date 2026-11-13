using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

public static class ReconstructionEtudiantsDossiersSeeder
{
    private const string ResourceName = "etudiants-dossiers-2026-2027.json";
    private static readonly DateOnly DefaultDateNaissance = new(1900, 1, 1);

    public static async Task SeedAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var data = await LoadDataAsync(cancellationToken);
        var annee = await context.AnneesAcademiques
            .FirstOrDefaultAsync(x => x.Libelle == data.AnneeAcademique.Libelle, cancellationToken)
            ?? throw new InvalidOperationException(
                $"L'année académique '{data.AnneeAcademique.Libelle}' est introuvable.");

        var records = data.Groupes.SelectMany(x => x.Etudiants).ToList();
        var etudiants = await context.Etudiants.ToListAsync(cancellationToken);
        var etudiantsByName = etudiants
            .GroupBy(x => Normalize($"{x.Nom} {x.Prenoms}"))
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            var nameKey = Normalize(record.NomCompletSource);
            if (string.IsNullOrWhiteSpace(nameKey) || etudiantsByName.ContainsKey(nameKey))
            {
                continue;
            }

            var etudiant = new Etudiant
            {
                Nom = record.Nom,
                Prenoms = record.Prenoms,
                DateNaissance = DefaultDateNaissance,
                LieuNaissance = "Non renseigné",
                Sexe = ParseSexe(record.Sexe),
                AptitudeMedicale = AptitudeMedicale.NonRenseignee,
                Nationalite = "Non renseignée"
            };

            context.Etudiants.Add(etudiant);
            etudiantsByName[nameKey] = etudiant;
        }

        await context.SaveChangesAsync(cancellationToken);

        var parcours = await context.ParcoursAcademiques
            .Include(x => x.CycleFormation)
            .Include(x => x.NiveauEtude)
            .Include(x => x.Filiere)
            .Include(x => x.Specialite)
            .Where(x => x.AnneeAcademiqueId == annee.Id)
            .ToListAsync(cancellationToken);

        var dossiers = await context.DossiersEtudiants.ToListAsync(cancellationToken);
        var dossiersByKey = dossiers.ToDictionary(x => (x.EtudiantId, x.ParcoursAcademiqueId));
        var usedMatricules = dossiers
            .Select(x => x.Matricule)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matriculesByStudent = dossiers
            .GroupBy(x => x.EtudiantId)
            .ToDictionary(x => x.Key, x => x.First().Matricule);
        var unresolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            if (!etudiantsByName.TryGetValue(Normalize(record.NomCompletSource), out var etudiant))
            {
                continue;
            }

            var parcoursAcademique = ResolveParcours(parcours, record);
            if (parcoursAcademique is null)
            {
                unresolved.Add(
                    $"{record.ClasseSource} | {record.NomCompletSource} | {record.ParcoursAcademiqueCode ?? record.ParcoursAcademiqueLibelle ?? "parcours inconnu"}");
                continue;
            }

            var dossierKey = (etudiant.Id, parcoursAcademique.Id);
            if (dossiersByKey.ContainsKey(dossierKey))
            {
                matriculesByStudent.TryAdd(etudiant.Id, dossiersByKey[dossierKey].Matricule);
                continue;
            }

            if (!matriculesByStudent.TryGetValue(etudiant.Id, out var matricule))
            {
                matricule = AllocateMatricule(record.Matricule, annee.AnneeDebut, usedMatricules);
                matriculesByStudent[etudiant.Id] = matricule;
            }

            var dossier = new DossierEtudiant
            {
                EtudiantId = etudiant.Id,
                ParcoursAcademiqueId = parcoursAcademique.Id,
                Matricule = matricule
            };

            context.DossiersEtudiants.Add(dossier);
            dossiersByKey[dossierKey] = dossier;
        }

        await context.SaveChangesAsync(cancellationToken);

        var administratifs = await context.DossiersAdministratifs
            .ToDictionaryAsync(x => (x.EtudiantId, x.ParcoursAcademiqueId), cancellationToken);
        var academiques = await context.DossiersAcademiques
            .ToDictionaryAsync(x => (x.EtudiantId, x.ParcoursAcademiqueId), cancellationToken);
        var classesParcours = await context.ClassesPedagogiques
            .GroupBy(x => x.ParcoursAcademiqueId)
            .ToDictionaryAsync(x => x.Key, x => x.OrderBy(y => y.Id).First(), cancellationToken);

        foreach (var dossier in dossiersByKey.Values.Where(x => x.Id > 0))
        {
            var key = (dossier.EtudiantId, dossier.ParcoursAcademiqueId);
            classesParcours.TryGetValue(dossier.ParcoursAcademiqueId, out var classe);

            if (!administratifs.ContainsKey(key))
            {
                context.DossiersAdministratifs.Add(new DossierAdministratif
                {
                    EtudiantId = dossier.EtudiantId,
                    ParcoursAcademiqueId = dossier.ParcoursAcademiqueId,
                    DossierEtudiantId = dossier.Id,
                    Statut = StatutDossierAdministratif.EnPreparation
                });
            }

            if (academiques.TryGetValue(key, out var dossierAcademique))
            {
                // Répare les dossiers créés par une ancienne version du
                // seeder, qui ne renseignait pas encore la classe.
                if (classe is not null && dossierAcademique.ClassePedagogiqueId != classe.Id)
                {
                    dossierAcademique.ClassePedagogiqueId = classe.Id;
                }
            }
            else
            {
                context.DossiersAcademiques.Add(new DossierAcademique
                {
                    EtudiantId = dossier.EtudiantId,
                    ParcoursAcademiqueId = dossier.ParcoursAcademiqueId,
                    DossierEtudiantId = dossier.Id,
                    ClassePedagogiqueId = classe?.Id,
                    Statut = StatutDossierAcademique.EnCours
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        if (unresolved.Count > 0)
        {
            Console.WriteLine(
                $"ReconstructionEtudiantsDossiersSeeder : {unresolved.Count} dossier(s) non créé(s), parcours absent(s) de la base actuelle.");
            foreach (var item in unresolved.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  - {item}");
            }
        }
    }

    private static ParcoursAcademique? ResolveParcours(
        IReadOnlyCollection<ParcoursAcademique> parcours,
        StudentRecord record)
        => string.IsNullOrWhiteSpace(record.CycleCodeCandidate)
            || string.IsNullOrWhiteSpace(record.FiliereCodeCandidate)
            || string.IsNullOrWhiteSpace(record.SpecialiteCodeCandidate)
            ? null
            : parcours.FirstOrDefault(x =>
                x.CycleFormation.Code.Equals(record.CycleCodeCandidate, StringComparison.OrdinalIgnoreCase)
                && x.NiveauEtude.Numero == record.NiveauNumero
                && x.Filiere.Code.Equals(record.FiliereCodeCandidate, StringComparison.OrdinalIgnoreCase)
                && x.Specialite.Code.Equals(record.SpecialiteCodeCandidate, StringComparison.OrdinalIgnoreCase));

    private static string AllocateMatricule(string? preferred, short anneeDebut, ISet<string> used)
    {
        if (!string.IsNullOrWhiteSpace(preferred) && used.Add(Normalize(preferred)))
        {
            return preferred.Trim();
        }

        var yearCode = (anneeDebut % 100).ToString("D2", CultureInfo.InvariantCulture);
        var next = used
            .Select(x => TryReadSequence(x, yearCode))
            .Where(x => x is not null)
            .Select(x => x!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

        string candidate;
        do
        {
            candidate = $"MAT-{yearCode}-{next++.ToString("D5", CultureInfo.InvariantCulture)}";
        }
        while (!used.Add(Normalize(candidate)));

        return candidate;
    }

    private static int? TryReadSequence(string value, string yearCode)
    {
        var prefix = $"MAT-{yearCode}-";
        return !value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? null
            : int.TryParse(value[prefix.Length..], out var sequence) ? sequence : null;
    }

    private static Sexe ParseSexe(string? value)
        => value?.Trim().ToUpperInvariant() switch
        {
            "M" => Sexe.Masculin,
            "F" => Sexe.Feminin,
            _ => Sexe.NonRenseigne
        };

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(chars).ToUpperInvariant().Trim();
    }

    private static async Task<ImportSeedData> LoadDataAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(ReconstructionEtudiantsDossiersSeeder).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(x => x.EndsWith(ResourceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException($"La ressource embarquée '{ResourceName}' est introuvable.");

        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"La ressource embarquée '{ResourceName}' est introuvable.");

        return await JsonSerializer.DeserializeAsync<ImportSeedData>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken)
            ?? throw new InvalidOperationException($"La ressource '{ResourceName}' est vide ou invalide.");
    }

    public sealed class ImportSeedData
    {
        public AcademicYearData AnneeAcademique { get; set; } = new();
        public List<ImportGroup> Groupes { get; set; } = [];
    }

    public sealed class AcademicYearData
    {
        public string Libelle { get; set; } = string.Empty;
    }

    public sealed class ImportGroup
    {
        public List<StudentRecord> Etudiants { get; set; } = [];
    }

    public sealed class StudentRecord
    {
        public string Nom { get; set; } = string.Empty;
        public string Prenoms { get; set; } = string.Empty;
        public string NomCompletSource { get; set; } = string.Empty;
        public string Matricule { get; set; } = string.Empty;
        public string? Sexe { get; set; }
        public string? ClasseSource { get; set; }
        public string? CycleCodeCandidate { get; set; }
        public int NiveauNumero { get; set; }
        public string? FiliereCodeCandidate { get; set; }
        public string? SpecialiteCodeCandidate { get; set; }
        public string? ParcoursAcademiqueCode { get; set; }
        public string? ParcoursAcademiqueLibelle { get; set; }
    }
}
