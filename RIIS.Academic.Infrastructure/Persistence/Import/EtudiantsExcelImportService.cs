using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Import;

/// <summary>
/// Importe les listes d'étudiants des classeurs administratifs sans les brancher
/// au démarrage de l'application. L'opération est idempotente et transactionnelle.
///
/// Les classeurs actuels ne contiennent ni matricule ni date de naissance. Les
/// doublons sont donc d'abord recherchés par nom complet normalisé. Une date de
/// naissance présente dans un futur classeur sera utilisée comme second critère.
/// </summary>
public sealed class EtudiantsExcelImportService
{
    private const string AcademicYearLabel = "2026-2027";
    private const short AcademicYearStart = 2026;
    private static readonly DateOnly DefaultDateOfBirth = new(2000, 1, 1);

    public async Task<EtudiantsExcelImportResult> ImportAsync(
        RiisAcademicDbContext context,
        string niveau1Path,
        string niveau2Path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sourceRows = ReadWorkbook(niveau1Path)
            .Concat(ReadWorkbook(niveau2Path))
            .ToList();

        if (sourceRows.Count == 0)
        {
            throw new InvalidOperationException("Aucune ligne étudiant exploitable n'a été trouvée dans les classeurs.");
        }

        var annee = await context.AnneesAcademiques
            .SingleOrDefaultAsync(x => x.Libelle == AcademicYearLabel, cancellationToken)
            ?? throw new InvalidOperationException(
                $"L'année académique '{AcademicYearLabel}' est introuvable. Exécutez d'abord les référentiels.");

        var parcours = await context.ParcoursAcademiques
            .Where(x => x.AnneeAcademiqueId == annee.Id)
            .ToListAsync(cancellationToken);
        var classes = await context.ClassesPedagogiques
            .ToListAsync(cancellationToken);
        var students = await context.Etudiants.ToListAsync(cancellationToken);
        var dossiers = await context.DossiersEtudiants.ToListAsync(cancellationToken);
        var administratifs = await context.DossiersAdministratifs.ToListAsync(cancellationToken);
        var academiques = await context.DossiersAcademiques.ToListAsync(cancellationToken);

        var result = new EtudiantsExcelImportResult(sourceRows.Count);
        var studentsByName = students
            .GroupBy(x => Normalize($"{x.Nom} {x.Prenoms}"))
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
        var dossiersByMatricule = dossiers
            .Where(x => !string.IsNullOrWhiteSpace(x.Matricule))
            .GroupBy(x => Normalize(x.Matricule))
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var dossiersByStudentAndParcours = dossiers
            .ToDictionary(x => (x.EtudiantId, x.ParcoursAcademiqueId));
        var usedMatricules = dossiers
            .Select(x => Normalize(x.Matricule))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nextMatriculeSequence = dossiers
            .Select(x => ReadMatriculeSequence(x.Matricule))
            .Where(x => x is not null)
            .Select(x => x!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        foreach (var row in sourceRows)
        {
            var parcoursCode = ResolveParcoursCode(row);
            if (parcoursCode is null)
            {
                result.Unresolved.Add(
                    $"{row.SourceFile} / {row.Worksheet}: {row.FullName} - spécialité absente ou non reconnue");
                continue;
            }

            var parcoursMatches = parcours
                .Where(x => string.Equals(x.Code, parcoursCode, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (parcoursMatches.Count != 1)
            {
                result.Unresolved.Add(
                    $"{row.SourceFile} / {row.Worksheet}: {row.FullName} - parcours '{parcoursCode}' introuvable ou ambigu");
                continue;
            }

            var parcoursAcademique = parcoursMatches[0];
            var nameKey = Normalize(row.FullName);
            if (string.IsNullOrWhiteSpace(nameKey))
            {
                result.InvalidRows.Add($"{row.SourceFile} / {row.Worksheet}: nom vide");
                continue;
            }

            Etudiant? etudiant = null;
            if (!string.IsNullOrWhiteSpace(row.Matricule)
                && dossiersByMatricule.TryGetValue(Normalize(row.Matricule), out var dossierByMatricule))
            {
                etudiant = students.FirstOrDefault(x => x.Id == dossierByMatricule.EtudiantId);
            }

            if (etudiant is null && studentsByName.TryGetValue(nameKey, out var sameNameStudents))
            {
                var candidates = row.DateNaissance is null
                    ? sameNameStudents
                    : sameNameStudents.Where(x => x.DateNaissance == row.DateNaissance.Value).ToList();

                if (candidates.Count > 1)
                {
                    result.Conflicts.Add(
                        $"{row.FullName}: plusieurs étudiants portent ce nom ; matricule ou date de naissance requis");
                    continue;
                }

                etudiant = candidates.SingleOrDefault();
            }

            if (etudiant is null)
            {
                var (nom, prenoms) = SplitFullName(row.FullName);
                if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(prenoms))
                {
                    result.InvalidRows.Add($"{row.FullName}: impossible de séparer nom et prénoms");
                    continue;
                }

                etudiant = new Etudiant
                {
                    Nom = nom.ToUpperInvariant(),
                    Prenoms = prenoms,
                    DateNaissance = row.DateNaissance ?? DefaultDateOfBirth,
                    LieuNaissance = "Non renseigné",
                    Sexe = row.Sexe,
                    AptitudeMedicale = AptitudeMedicale.NonRenseignee,
                    Nationalite = "Camerounaise"
                };
                context.Etudiants.Add(etudiant);
                students.Add(etudiant);
                studentsByName[nameKey] = [etudiant];
                result.StudentsInserted++;
            }
            else
            {
                result.StudentsExisting++;
            }

            // Le service de dossier interdit deux parcours pour le même étudiant
            // pendant la même année académique. On conserve cette règle pendant
            // l'import afin de ne pas créer une situation impossible dans l'UI.
            var otherSameYear = dossiers.Any(x =>
                x.EtudiantId == etudiant.Id
                && x.ParcoursAcademiqueId != parcoursAcademique.Id
                && parcours.FirstOrDefault(p => p.Id == x.ParcoursAcademiqueId)?.AnneeAcademiqueId == annee.Id);
            if (otherSameYear)
            {
                result.Conflicts.Add(
                    $"{row.FullName}: un autre dossier existe déjà pour l'année '{AcademicYearLabel}'");
                continue;
            }

            if (etudiant.Id == 0)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            if (!dossiersByStudentAndParcours.TryGetValue(
                    (etudiant.Id, parcoursAcademique.Id),
                    out var dossierEtudiant))
            {
                var matricule = AllocateMatricule(
                    row.Matricule,
                    usedMatricules,
                    ref nextMatriculeSequence);
                dossierEtudiant = new DossierEtudiant
                {
                    EtudiantId = etudiant.Id,
                    ParcoursAcademiqueId = parcoursAcademique.Id,
                    Matricule = matricule
                };
                context.DossiersEtudiants.Add(dossierEtudiant);
                dossiers.Add(dossierEtudiant);
                dossiersByStudentAndParcours[(etudiant.Id, parcoursAcademique.Id)] = dossierEtudiant;
                dossiersByMatricule[Normalize(matricule)] = dossierEtudiant;
                result.DossiersInserted++;
            }
            else
            {
                result.DossiersExisting++;
            }

            await context.SaveChangesAsync(cancellationToken);

            if (!administratifs.Any(x =>
                    x.EtudiantId == etudiant.Id
                    && x.ParcoursAcademiqueId == parcoursAcademique.Id))
            {
                context.DossiersAdministratifs.Add(new DossierAdministratif
                {
                    EtudiantId = etudiant.Id,
                    ParcoursAcademiqueId = parcoursAcademique.Id,
                    DossierEtudiantId = dossierEtudiant.Id,
                    Statut = StatutDossierAdministratif.EnPreparation
                });
            }

            var academique = academiques.FirstOrDefault(x =>
                x.EtudiantId == etudiant.Id
                && x.ParcoursAcademiqueId == parcoursAcademique.Id);
            if (academique is null)
            {
                var classeId = classes
                    .Where(x => x.ParcoursAcademiqueId == parcoursAcademique.Id)
                    .OrderBy(x => x.Id)
                    .Select(x => (long?)x.Id)
                    .FirstOrDefault();

                context.DossiersAcademiques.Add(new DossierAcademique
                {
                    EtudiantId = etudiant.Id,
                    ParcoursAcademiqueId = parcoursAcademique.Id,
                    DossierEtudiantId = dossierEtudiant.Id,
                    ClassePedagogiqueId = classeId,
                    Statut = StatutDossierAcademique.EnCours
                });
            }
            else if (academique.DossierEtudiantId is null)
            {
                academique.DossierEtudiantId = dossierEtudiant.Id;
            }

            result.RowsApplied++;
            result.AddedOrRepairedAdministrativeAndAcademic = true;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static IEnumerable<SourceStudentRow> ReadWorkbook(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Classeur étudiant introuvable.", path);
        }

        using var workbook = new XLWorkbook(path);
        foreach (var worksheet in workbook.Worksheets)
        {
            var usedRange = worksheet.RangeUsed();
            if (usedRange is null)
            {
                continue;
            }

            var headerRow = usedRange.RowsUsed()
                .FirstOrDefault(row => row.CellsUsed()
                    .Any(cell => Normalize(cell.GetString()).Contains("NOMS ET PRENOMS", StringComparison.Ordinal)));
            if (headerRow is null)
            {
                continue;
            }

            var metadata = ReadMetadata(worksheet, headerRow.RowNumber());
            var columns = headerRow.CellsUsed()
                .ToDictionary(
                    cell => Normalize(cell.GetString()),
                    cell => cell.ColumnNumber(),
                    StringComparer.OrdinalIgnoreCase);

            var fullNameColumn = FindColumn(columns, "NOMS ET PRENOMS");
            var sexColumn = FindColumn(columns, "SEXES", "SEXE");
            var specialtyColumn = FindColumn(columns, "SPECIALITE");
            var filiereColumn = FindColumn(columns, "FILIERES", "FILIERE");
            var matriculeColumn = FindColumn(columns, "MATRICULE");
            var dateColumn = FindColumn(columns, "DATE NAISSANCE", "DATE DE NAISSANCE");

            foreach (var row in usedRange.RowsUsed().Where(x => x.RowNumber() > headerRow.RowNumber()))
            {
                var fullName = row.Cell(fullNameColumn).GetString().Trim();
                if (string.IsNullOrWhiteSpace(fullName)
                    || Normalize(fullName) is "NOMS ET PRENOMS" or "TOTAL")
                {
                    continue;
                }

                yield return new SourceStudentRow(
                    Path.GetFileName(path),
                    worksheet.Name,
                    fullName,
                    sexColumn is null ? Sexe.NonRenseigne : ParseSexe(row.Cell(sexColumn.Value).GetString()),
                    specialtyColumn is null ? null : NullIfEmpty(row.Cell(specialtyColumn.Value).GetString()),
                    filiereColumn is null ? metadata.GetValueOrDefault("FILIERE") : NullIfEmpty(row.Cell(filiereColumn.Value).GetString()) ?? metadata.GetValueOrDefault("FILIERE"),
                    matriculeColumn is null ? null : NullIfEmpty(row.Cell(matriculeColumn.Value).GetString()),
                    dateColumn is null ? null : ParseDate(row.Cell(dateColumn.Value)));
            }
        }
    }

    private static Dictionary<string, string> ReadMetadata(IXLWorksheet worksheet, int headerRow)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var rowNumber = 1; rowNumber < headerRow; rowNumber++)
        {
            foreach (var cell in worksheet.Row(rowNumber).CellsUsed())
            {
                var value = cell.GetString();
                var separator = value.IndexOf(':');
                if (separator <= 0)
                {
                    continue;
                }

                var key = Normalize(value[..separator]);
                var content = value[(separator + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(content))
                {
                    metadata[key] = content;
                }
            }
        }

        return metadata;
    }

    private static int? FindColumn(IReadOnlyDictionary<string, int> columns, params string[] names)
        => columns
            .Where(x => names.Any(name => x.Key.Contains(Normalize(name), StringComparison.Ordinal)))
            .Select(x => (int?)x.Value)
            .FirstOrDefault();

    private static string? ResolveParcoursCode(SourceStudentRow row)
    {
        var specialty = Normalize(row.Specialite);
        var worksheet = Normalize(row.Worksheet);
        var filiere = Normalize(row.Filiere);

        if (row.Niveau == 1)
        {
            if (specialty is "MEC" || filiere.Contains("MANAGEMENT") || filiere.Contains("ECONOMIE"))
            {
                return "2026-2027-PREPA-N1-MEC";
            }

            if (specialty is "ING" || filiere.Contains("INGENIERIE") || worksheet.Contains("ING"))
            {
                return "2026-2027-PREPA-N1-ING-GEN";
            }
        }

        if (row.Niveau == 2)
        {
            if (specialty is "MEC" || filiere.Contains("MANAGEMENT") || filiere.Contains("ECONOMIE") || worksheet.Contains("MANAGEMENT"))
            {
                return "2026-2027-PREPA-N2-MEC";
            }

            if (specialty.Contains("INFORMATIQUE"))
            {
                return "2026-2027-PREPA-N2-ING-INF";
            }

            if (specialty.Contains("GENERALISTE") || specialty.Contains("GENERAL"))
            {
                return "2026-2027-PREPA-N2-ING-GEN";
            }
        }

        return null;
    }

    private static (string Nom, string Prenoms) SplitFullName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length < 2
            ? (fullName.Trim(), string.Empty)
            : (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private static string AllocateMatricule(
        string? preferred,
        ISet<string> used,
        ref int nextSequence)
    {
        if (!string.IsNullOrWhiteSpace(preferred) && used.Add(Normalize(preferred)))
        {
            return preferred.Trim();
        }

        string candidate;
        do
        {
            candidate = $"MAT-{AcademicYearStart % 100:00}-{nextSequence++:00000}";
        }
        while (!used.Add(Normalize(candidate)));

        return candidate;
    }

    private static int? ReadMatriculeSequence(string? matricule)
    {
        var normalized = Normalize(matricule);
        var prefix = $"MAT-{AcademicYearStart % 100:00}-";
        return normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(normalized[prefix.Length..], out var sequence)
            ? sequence
            : null;
    }

    private static DateOnly? ParseDate(IXLCell cell)
    {
        if (cell.TryGetValue<DateTime>(out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        var value = cell.GetString().Trim();
        return DateOnly.TryParse(value, CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static Sexe ParseSexe(string? value)
        => Normalize(value) switch
        {
            "M" => Sexe.Masculin,
            "F" => Sexe.Feminin,
            _ => Sexe.NonRenseigne
        };

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var chars = decomposed
            .Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark)
            .ToArray();
        var normalized = new string(chars).ToUpperInvariant();
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private sealed record SourceStudentRow(
        string SourceFile,
        string Worksheet,
        string FullName,
        Sexe Sexe,
        string? Specialite,
        string? Filiere,
        string? Matricule,
        DateOnly? DateNaissance)
    {
        public int Niveau => Worksheet.Contains('2', StringComparison.Ordinal) ? 2 : 1;
    }
}

public sealed class EtudiantsExcelImportResult(int sourceRows)
{
    public int SourceRows { get; } = sourceRows;
    public int StudentsInserted { get; set; }
    public int StudentsExisting { get; set; }
    public int DossiersInserted { get; set; }
    public int DossiersExisting { get; set; }
    public int RowsApplied { get; set; }
    public bool AddedOrRepairedAdministrativeAndAcademic { get; set; }
    public List<string> Unresolved { get; } = [];
    public List<string> InvalidRows { get; } = [];
    public List<string> Conflicts { get; } = [];
}
