using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Application.Notes.Dtos;
using RIIS.Academic.Application.Notes.Services;
using RIIS.Academic.Domain;
using RIIS.Academic.Infrastructure.Persistence;

namespace RIIS.Academic.Infrastructure.Documents;

public sealed class NotesCanevasExcelImportService(RiisAcademicDbContext context)
    : INotesCanevasExcelImportService
{
    private readonly SemaphoreSlim operationGate = new(1, 1);

    public async Task<NotesCanevasImportResultDto> ImporterAsync(
        Stream fichier,
        CancellationToken cancellationToken = default)
    {
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            return await ImporterCoreAsync(fichier, cancellationToken);
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async Task<NotesCanevasImportResultDto> ImporterCoreAsync(
        Stream fichier,
        CancellationToken cancellationToken)
    {
        var result = new NotesCanevasImportResultDto();

        // Le flux fourni par Blazor InputFile n'autorise pas les lectures synchrones.
        // ClosedXML effectue des lectures synchrones : on matérialise donc le fichier
        // en mémoire avec une copie asynchrone avant de le remettre à ClosedXML.
        await using var workbookStream = new MemoryStream();
        await fichier.CopyToAsync(workbookStream, cancellationToken);
        workbookStream.Position = 0;

        using var workbook = new XLWorkbook(workbookStream);
        var parameters = ReadParameters(workbook, result);
        if (result.Errors.Count > 0)
        {
            return result;
        }

        if (!parameters.TryGetValue("Format", out var format)
            || !string.Equals(format, "RIIS-NOTES-1", StringComparison.Ordinal))
        {
            result.Errors.Add("Le fichier n'est pas un canevas RIIS-NOTES-1.");
            return result;
        }

        var classeId = ReadRequiredLong(parameters, "ClassePedagogiqueId", result);
        var parcoursId = ReadRequiredLong(parameters, "ParcoursAcademiqueId", result);
        var semestreId = ReadRequiredLong(parameters, "SemestrePedagogiqueId", result);
        var anneeId = ReadRequiredLong(parameters, "AnneeAcademiqueId", result);
        if (result.Errors.Count > 0)
        {
            return result;
        }

        var grid = GetWorksheet(workbook, "Grille_Saisie", result);
        var statuses = GetWorksheet(workbook, "Statuts", result);
        if (result.Errors.Count > 0)
        {
            return result;
        }

        var evaluations = await context.EvaluationsAcademiques
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var dossiers = await context.DossiersAcademiques
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var classe = await context.ClassesPedagogiques
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == classeId, cancellationToken);
        var semestre = semestreId == 0
            ? null
            : await context.SemestresPedagogiques
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == semestreId, cancellationToken);
        var maquetteElements = await context.MaquetteElementsConstitutifs
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (classe is null)
        {
            result.Errors.Add($"La classe pédagogique {classeId} est introuvable.");
        }
        else if (classe.ParcoursAcademiqueId != parcoursId)
        {
            result.Errors.Add($"La classe {classeId} n'appartient pas au parcours {parcoursId} du canevas.");
        }

        if (semestreId != 0 && semestre is null)
        {
            result.Errors.Add($"Le semestre pédagogique {semestreId} est introuvable.");
        }

        var evaluationColumns = ReadEvaluationColumns(grid, result);
        var evaluationById = evaluations.ToDictionary(x => x.Id);
        var dossierById = dossiers.ToDictionary(x => x.Id);
        var maquetteElementById = maquetteElements.ToDictionary(x => x.Id);
        foreach (var evaluationId in evaluationColumns.Keys)
        {
            if (!evaluationById.TryGetValue(evaluationId, out var evaluation))
            {
                result.Errors.Add($"L'évaluation {evaluationId} n'existe pas.");
                continue;
            }

            if (evaluation.AnneeAcademiqueId != anneeId)
            {
                result.Errors.Add($"L'évaluation {evaluationId} n'appartient pas à l'année du canevas.");
            }

            if (!maquetteElementById.TryGetValue(evaluation.MaquetteElementConstitutifId, out var maquetteElement)
                || (semestre is not null && maquetteElement.SemestrePedagogiqueId != semestre.Id))
            {
                result.Errors.Add($"L'évaluation {evaluationId} n'appartient pas au semestre du canevas.");
            }
        }

        var entries = ReadGridEntries(
            grid,
            evaluationColumns,
            evaluationById,
            dossierById,
            classe,
            parcoursId,
            semestre,
            result);
        ApplyStatuses(
            statuses,
            entries,
            evaluationById,
            evaluationColumns,
            dossierById,
            classe,
            parcoursId,
            semestre,
            result);

        if (result.Errors.Count > 0)
        {
            return result;
        }

        var keys = entries.Keys.ToHashSet();
        var existingNotes = (await context.NotesEvaluations
                .ToListAsync(cancellationToken))
            .Where(x => keys.Contains((x.DossierAcademiqueId, x.EvaluationAcademiqueId)))
            .ToList();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var entry in entries.Values)
        {
            var existing = existingNotes.FirstOrDefault(x =>
                x.DossierAcademiqueId == entry.DossierAcademiqueId
                && x.EvaluationAcademiqueId == entry.EvaluationAcademiqueId);

            if (existing is null)
            {
                context.NotesEvaluations.Add(new NoteEvaluation
                {
                    DossierAcademiqueId = entry.DossierAcademiqueId,
                    EvaluationAcademiqueId = entry.EvaluationAcademiqueId,
                    Valeur = entry.Valeur,
                    StatutPresence = entry.StatutPresence,
                    Observation = Normalize(entry.Observation),
                    SaisieLeUtc = now,
                    SaisiePar = "import-excel"
                });
            }
            else
            {
                if (entry.Valeur.HasValue)
                {
                    existing.Valeur = entry.Valeur;
                }

                existing.StatutPresence = entry.StatutPresence;
                existing.Observation = Normalize(entry.Observation) ?? existing.Observation;
                existing.SaisieLeUtc = now;
                existing.SaisiePar = "import-excel";
            }

            result.NotesTraitees++;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static Dictionary<string, string> ReadParameters(
        XLWorkbook workbook,
        NotesCanevasImportResultDto result)
    {
        var sheet = GetWorksheet(workbook, "ParametresImport", result);
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (sheet is null)
        {
            return values;
        }

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        for (var row = 2; row <= lastRow; row++)
        {
            var key = sheet.Cell(row, 1).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(key))
            {
                values[key] = sheet.Cell(row, 2).GetString().Trim();
            }
        }

        return values;
    }

    private static IXLWorksheet? GetWorksheet(
        XLWorkbook workbook,
        string name,
        NotesCanevasImportResultDto result)
    {
        var sheet = workbook.Worksheets.FirstOrDefault(x =>
            string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        if (sheet is null)
        {
            result.Errors.Add($"La feuille Excel '{name}' est obligatoire.");
        }

        return sheet;
    }

    private static long ReadRequiredLong(
        IReadOnlyDictionary<string, string> values,
        string key,
        NotesCanevasImportResultDto result)
    {
        if (!values.TryGetValue(key, out var raw)
            || !long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            result.Errors.Add($"Le paramètre '{key}' est absent ou invalide.");
            return 0;
        }

        return value;
    }

    private static Dictionary<long, int> ReadEvaluationColumns(
        IXLWorksheet? sheet,
        NotesCanevasImportResultDto result)
    {
        var columns = new Dictionary<long, int>();
        if (sheet is null)
        {
            return columns;
        }

        var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (var column = 4; column <= lastColumn; column++)
        {
            var raw = sheet.Cell(9, column).GetString().Trim();
            if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var evaluationId))
            {
                continue;
            }

            if (!columns.TryAdd(evaluationId, column))
            {
                result.Errors.Add($"L'évaluation {evaluationId} apparaît plusieurs fois dans la grille.");
            }
        }

        if (columns.Count == 0)
        {
            result.Errors.Add("Aucune colonne d'évaluation exploitable n'a été trouvée dans Grille_Saisie.");
        }

        return columns;
    }

    private static Dictionary<(long DossierId, long EvaluationId), ImportEntry> ReadGridEntries(
        IXLWorksheet? sheet,
        IReadOnlyDictionary<long, int> evaluationColumns,
        IReadOnlyDictionary<long, EvaluationAcademique> evaluationById,
        IReadOnlyDictionary<long, DossierAcademique> dossierById,
        ClassePedagogique? classe,
        long parcoursId,
        SemestrePedagogique? semestre,
        NotesCanevasImportResultDto result)
    {
        var entries = new Dictionary<(long, long), ImportEntry>();
        if (sheet is null)
        {
            return entries;
        }

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        for (var row = 10; row <= lastRow; row++)
        {
            if (!TryReadLong(sheet.Cell(row, 1), out var dossierId))
            {
                continue;
            }

            if (!dossierById.TryGetValue(dossierId, out var dossier))
            {
                result.Errors.Add($"Le dossier académique {dossierId} n'existe pas.");
                continue;
            }

            if (!DossierAppartientAuCanevas(dossier, classe, parcoursId))
            {
                result.Errors.Add($"Le dossier {dossierId} n'appartient pas au parcours/classe du canevas.");
                continue;
            }

            foreach (var (evaluationId, column) in evaluationColumns)
            {
                var cell = sheet.Cell(row, column);
                if (cell.IsEmpty())
                {
                    continue;
                }

                if (!evaluationById.TryGetValue(evaluationId, out var evaluation))
                {
                    result.Errors.Add($"L'évaluation {evaluationId} n'existe pas.");
                    continue;
                }

                if (!TryReadDecimal(cell, out var value)
                    || value < 0
                    || value > evaluation.Bareme)
                {
                    result.Errors.Add($"La cellule {cell.Address} contient une note invalide pour l'évaluation {evaluationId}.");
                    continue;
                }

                var key = (dossierId, evaluationId);
                entries[key] = new ImportEntry(dossierId, evaluationId, value, StatutPresenceEvaluation.Present, null);
            }
        }

        return entries;
    }

    private static void ApplyStatuses(
        IXLWorksheet? sheet,
        IDictionary<(long DossierId, long EvaluationId), ImportEntry> entries,
        IReadOnlyDictionary<long, EvaluationAcademique> evaluationById,
        IReadOnlyDictionary<long, int> evaluationColumns,
        IReadOnlyDictionary<long, DossierAcademique> dossierById,
        ClassePedagogique? classe,
        long parcoursId,
        SemestrePedagogique? semestre,
        NotesCanevasImportResultDto result)
    {
        if (sheet is null)
        {
            return;
        }

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        for (var row = 2; row <= lastRow; row++)
        {
            if (!TryReadLong(sheet.Cell(row, 1), out var dossierId)
                || !TryReadLong(sheet.Cell(row, 4), out var evaluationId))
            {
                continue;
            }

            if (!dossierById.TryGetValue(dossierId, out var dossier)
                || !DossierAppartientAuCanevas(dossier, classe, parcoursId)
                || !evaluationColumns.ContainsKey(evaluationId)
                || !evaluationById.ContainsKey(evaluationId))
            {
                result.Errors.Add($"La ligne {row} de Statuts référence un dossier ou une évaluation hors du canevas.");
                continue;
            }

            var statusText = sheet.Cell(row, 6).GetString().Trim();
            var observation = sheet.Cell(row, 7).GetString();
            if (string.IsNullOrWhiteSpace(statusText))
            {
                statusText = nameof(StatutPresenceEvaluation.Present);
            }

            if (!Enum.TryParse<StatutPresenceEvaluation>(statusText, true, out var status))
            {
                result.Errors.Add($"Le statut '{statusText}' de la ligne {row} est invalide.");
                continue;
            }

            var key = (dossierId, evaluationId);
            if (!entries.TryGetValue(key, out var entry))
            {
                if (status == StatutPresenceEvaluation.Present && string.IsNullOrWhiteSpace(observation))
                {
                    continue;
                }

                entry = new ImportEntry(dossierId, evaluationId, null, status, observation);
            }
            else
            {
                entry = entry with
                {
                    StatutPresence = status,
                    Observation = observation
                };
            }

            if (status != StatutPresenceEvaluation.Present)
            {
                entry = entry with { Valeur = null };
            }

            entries[key] = entry;
        }
    }

    private static bool DossierAppartientAuCanevas(
        DossierAcademique dossier,
        ClassePedagogique? classe,
        long parcoursId)
    {
        // Règle métier actuelle : un parcours est dispensé dans une seule classe.
        // La classe portée directement par les anciens dossiers peut donc être
        // absente ou obsolète ; le parcours reste la relation de référence.
        return classe is not null
            && classe.ParcoursAcademiqueId == parcoursId
            && dossier.ParcoursAcademiqueId == parcoursId;
    }

    private static bool TryReadLong(IXLCell cell, out long value)
        => long.TryParse(cell.GetString().Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    private static bool TryReadDecimal(IXLCell cell, out decimal value)
    {
        var raw = cell.GetString()
            .Trim()
            .Replace("\u00A0", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(raw))
        {
            value = default;
            return false;
        }

        // Excel peut fournir une note décimale avec une virgule ou un point.
        // Ne pas utiliser NumberStyles.Number avec la culture invariant en
        // premier : "11,5" serait alors lu comme 115 (virgule de milliers).
        var lastComma = raw.LastIndexOf(',');
        var lastPoint = raw.LastIndexOf('.');
        if (lastComma >= 0 && lastPoint >= 0)
        {
            var decimalSeparator = lastComma > lastPoint ? ',' : '.';
            var thousandsSeparator = decimalSeparator == ',' ? '.' : ',';
            raw = raw.Replace(thousandsSeparator.ToString(), string.Empty, StringComparison.Ordinal)
                .Replace(decimalSeparator.ToString(), ".", StringComparison.Ordinal);
        }
        else if (lastComma >= 0)
        {
            raw = raw.Replace(',', '.');
        }

        return decimal.TryParse(
            raw,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record ImportEntry(
        long DossierAcademiqueId,
        long EvaluationAcademiqueId,
        decimal? Valeur,
        StatutPresenceEvaluation StatutPresence,
        string? Observation);
}
