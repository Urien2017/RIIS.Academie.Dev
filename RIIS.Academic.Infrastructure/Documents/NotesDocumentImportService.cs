using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RIIS.Academic.Application.Notes.Dtos;
using RIIS.Academic.Application.Notes.Services;
using RIIS.Academic.Domain;
using RIIS.Academic.Infrastructure.Persistence;

namespace RIIS.Academic.Infrastructure.Documents;

/// <summary>
/// Lit les fiches de report Word et les documents numérisés, puis rattache les
/// notes à une évaluation précise après validation de l'aperçu.
/// </summary>
public sealed class NotesDocumentImportService(
    RiisAcademicDbContext context,
    IConfiguration configuration) : INotesDocumentImportService
{
    private static readonly HashSet<string> ScanExtensions =
    [".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff"];

    private static readonly Regex OcrMatriculeRegex = new(
        @"(?<![A-Z0-9])(?:MAT[- ]?\d{2}[- ]?\d{3,}|\d{2}[A-Z]{2,}\d+[A-Z0-9]*|[A-Z]{2,}\d+[A-Z0-9]*)(?![A-Z0-9])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex OcrNoteRegex = new(
        @"(?<note>(?:20|1?\d)(?:[.,]\d{1,2})?)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ReportHeaderRegex = new(
        @"FICHE\s+DE\s+REPORT\s+DES\s+NOTES\s+(?:(?<type>CCON|CC|SN|SR)\s+|[_\- ]+)?CYCLE\s+(?<cycle>[\p{L}\p{M}0-9][\p{L}\p{M}0-9 '\-/]*?)(?=\s+(?:FILIERE|CLASSE|SEMESTRE|UE|N\s*[°ºo]?|MATRICULES|NOMS|NOTES)\b|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex ReportUeRegex = new(
        @"\bUE\s*:\s*(?<value>.+?)(?=\s+EC\s*:|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex ReportEcRegex = new(
        @"\bEC\s*:\s*(?<value>.+?)(?=\s+(?:N\s*[°ºo]?|MATRICULES|NOMS|NOTES)\b|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex ReportFiliereRegex = new(
        @"\bFILIERE\s*:\s*(?<value>.+?)(?=\s+(?:CLASSE|SEMESTRE|UE|EC)\s*:|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex ReportClasseRegex = new(
        @"\bCLASSE\s*:\s*(?<value>.+?)(?=\s+(?:SEMESTRE|UE|EC)\s*:|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex ReportSemestreRegex = new(
        @"\bSEMESTRE\s*:\s*(?<value>[IVXLCDM]+|\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex AcademicYearRegex = new(
        @"\b(?<value>20\d{2}\s*[-/]\s*20\d{2})\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly SemaphoreSlim operationGate = new(1, 1);

    public async Task<NotesDocumentPreviewDto> AnalyserAsync(
        Stream fichier,
        string nomFichier,
        CancellationToken cancellationToken = default)
    {
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            await using var buffer = new MemoryStream();
            await fichier.CopyToAsync(buffer, cancellationToken);
            var content = buffer.ToArray();
            var extension = Path.GetExtension(nomFichier).ToLowerInvariant();

            return extension == ".docx"
                ? AnalyseWord(content, nomFichier)
                : ScanExtensions.Contains(extension)
                    ? await AnalyseDocumentNumeriseAsync(content, extension, nomFichier, cancellationToken)
                    : new NotesDocumentPreviewDto
                    {
                        FileName = nomFichier,
                        Errors = [$"Format '{extension}' non pris en charge. Utilisez un fichier .docx ou un scan PDF/image."]
                    };
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async Task<NotesDocumentImportResultDto> ImporterAsync(
        NotesDocumentPreviewDto apercu,
        NotesDocumentImportContextDto contexte,
        CancellationToken cancellationToken = default)
    {
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            return await ImporterCoreAsync(apercu, contexte, cancellationToken);
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async Task<NotesDocumentImportResultDto> ImporterCoreAsync(
        NotesDocumentPreviewDto apercu,
        NotesDocumentImportContextDto contexte,
        CancellationToken cancellationToken)
    {
        var result = new NotesDocumentImportResultDto();
        result.Errors.AddRange(apercu.Errors);

        if (apercu.Lignes.Count == 0)
        {
            result.Errors.Add("Aucune ligne de notes n'a été extraite du document.");
            return result;
        }

        var classe = await context.ClassesPedagogiques
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == contexte.ClassePedagogiqueId, cancellationToken);
        var semestre = await context.SemestresPedagogiques
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == contexte.SemestrePedagogiqueId, cancellationToken);

        if (classe is null)
        {
            result.Errors.Add("La classe pédagogique sélectionnée est introuvable.");
        }

        if (semestre is null)
        {
            result.Errors.Add("Le semestre pédagogique sélectionné est introuvable.");
        }

        if (classe is null || semestre is null)
        {
            return result;
        }

        var parcoursId = classe.ParcoursAcademiqueId;
        var parcours = await context.ParcoursAcademiques
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == parcoursId && x.AnneeAcademiqueId == contexte.AnneeAcademiqueId, cancellationToken);
        if (parcours is null)
        {
            result.Errors.Add("La classe ne correspond pas à l'année académique sélectionnée.");
            return result;
        }

        if (semestre.MaquettePedagogiqueId != 0)
        {
            var maquetteEstAffectee = await context.AffectationsMaquettes
                .AsNoTracking()
                .AnyAsync(x => x.ParcoursAcademiqueId == parcoursId
                    && x.MaquettePedagogiqueId == semestre.MaquettePedagogiqueId
                    && x.EstActive, cancellationToken);
            var maquetteLegacy = await context.MaquettesPedagogiques
                .AsNoTracking()
                .AnyAsync(x => x.Id == semestre.MaquettePedagogiqueId
                    && x.ParcoursAcademiqueId == parcoursId, cancellationToken);

            if (!maquetteEstAffectee && !maquetteLegacy)
            {
                result.Errors.Add("Le semestre sélectionné n'appartient pas au programme de la classe.");
            }
        }

        var maquetteElements = await context.MaquetteElementsConstitutifs
            .AsNoTracking()
            .Where(x => x.SemestrePedagogiqueId == contexte.SemestrePedagogiqueId)
            .ToListAsync(cancellationToken);
        var elements = await context.ElementsConstitutifs
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var elementCode = ExtractMetadataCode(apercu.ElementConstitutifExtrait);
        var element = contexte.ElementConstitutifId is not null
            ? elements.FirstOrDefault(x => x.Id == contexte.ElementConstitutifId.Value)
            : string.IsNullOrWhiteSpace(elementCode)
                ? null
                : elements.FirstOrDefault(x => string.Equals(x.Code, elementCode, StringComparison.OrdinalIgnoreCase));

        if (element is null)
        {
            result.Errors.Add("Sélectionnez l'EC correspondant à la colonne Notes/20 du document.");
        }
        else if (!maquetteElements.Any(x => x.ElementConstitutifId == element.Id)
            || (contexte.UniteEnseignementId is not null
                && element.UniteEnseignementId != contexte.UniteEnseignementId.Value))
        {
            result.Errors.Add("L'EC sélectionné n'appartient pas au semestre ou à l'UE sélectionnée.");
        }

        var evaluations = await context.EvaluationsAcademiques
            .AsNoTracking()
            .Where(x => x.AnneeAcademiqueId == contexte.AnneeAcademiqueId)
            .ToListAsync(cancellationToken);
        var evaluation = ResolveEvaluation(
            evaluations,
            maquetteElements,
            element,
            contexte,
            result);

        if (result.Errors.Count > 0 || evaluation is null)
        {
            return result;
        }

        result.EvaluationAcademiqueId = evaluation.Id;

        var dossierEtudiants = await context.DossiersEtudiants
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var dossierAcademiques = await context.DossiersAcademiques
            .Where(x => x.ClassePedagogiqueId == classe.Id
                || (x.ClassePedagogiqueId == null && x.ParcoursAcademiqueId == parcoursId))
            .ToListAsync(cancellationToken);
        var matricules = dossierEtudiants
            .GroupBy(x => NormalizeMatricule(x.Matricule), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var entries = new List<(DossierAcademique Dossier, NotesDocumentRowDto Ligne)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ligne in apercu.Lignes)
        {
            if (!string.IsNullOrWhiteSpace(ligne.Erreur))
            {
                result.Errors.Add($"Ligne {ligne.Numero}: {ligne.Erreur}");
                continue;
            }

            var matricule = NormalizeMatricule(ligne.Matricule);
            if (!seen.Add(matricule))
            {
                result.Errors.Add($"Le matricule {ligne.Matricule} apparaît plusieurs fois dans le document.");
                continue;
            }

            if (ligne.Valeur is null && ligne.StatutPresence == StatutPresenceEvaluation.Present)
            {
                result.NotesIgnorees++;
                result.Warnings.Add($"Le matricule {ligne.Matricule} n'a pas encore de note : ligne ignorée.");
                continue;
            }

            if (!matricules.TryGetValue(matricule, out var dossierEtudiant))
            {
                result.Errors.Add($"Le matricule {ligne.Matricule} est absent des dossiers étudiants.");
                continue;
            }

            var dossier = dossierAcademiques.FirstOrDefault(x =>
                x.EtudiantId == dossierEtudiant.EtudiantId
                && (x.ClassePedagogiqueId == classe.Id || x.ClassePedagogiqueId == null));
            if (dossier is null)
            {
                result.Errors.Add($"Aucun dossier académique trouvé pour le matricule {ligne.Matricule}.");
                continue;
            }

            entries.Add((dossier, ligne));
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        var dossierIds = entries.Select(x => x.Dossier.Id).ToHashSet();
        var existingNotes = await context.NotesEvaluations
            .Where(x => x.EvaluationAcademiqueId == evaluation.Id && dossierIds.Contains(x.DossierAcademiqueId))
            .ToListAsync(cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var (dossier, ligne) in entries)
        {
            var note = existingNotes.FirstOrDefault(x => x.DossierAcademiqueId == dossier.Id);
            if (note is null)
            {
                note = new NoteEvaluation
                {
                    DossierAcademiqueId = dossier.Id,
                    EvaluationAcademiqueId = evaluation.Id
                };
                context.NotesEvaluations.Add(note);
            }

            note.Valeur = ligne.Valeur;
            note.StatutPresence = ligne.StatutPresence;
            note.SaisieLeUtc = now;
            note.SaisiePar = "import-document";
            result.NotesTraitees++;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        result.Warnings.AddRange(apercu.Warnings);
        return result;
    }

    private static EvaluationAcademique? ResolveEvaluation(
        IReadOnlyCollection<EvaluationAcademique> evaluations,
        IReadOnlyCollection<MaquetteElementConstitutif> maquetteElements,
        ElementConstitutif? element,
        NotesDocumentImportContextDto contexte,
        NotesDocumentImportResultDto result)
    {
        if (element is null)
        {
            return null;
        }

        var maquetteElementIds = maquetteElements
            .Where(x => x.ElementConstitutifId == element.Id)
            .Select(x => x.Id)
            .ToHashSet();
        var candidates = evaluations
            .Where(x => maquetteElementIds.Contains(x.MaquetteElementConstitutifId))
            .Where(x => contexte.TypeEvaluation is null || x.Type == contexte.TypeEvaluation.Value)
            .ToList();

        if (contexte.EvaluationAcademiqueId is not null)
        {
            var selected = candidates.FirstOrDefault(x => x.Id == contexte.EvaluationAcademiqueId.Value);
            if (selected is null)
            {
                result.Errors.Add("L'évaluation ciblée ne correspond pas à l'EC, au semestre ou au type sélectionné.");
            }

            return selected;
        }

        if (contexte.TypeEvaluation is null)
        {
            result.Errors.Add("Sélectionnez le type de l'évaluation ou une évaluation ciblée.");
            return null;
        }

        if (candidates.Count == 0)
        {
            result.Errors.Add("Aucune évaluation correspondante n'a été trouvée pour l'EC sélectionné.");
            return null;
        }

        if (candidates.Count > 1)
        {
            result.Errors.Add("Plusieurs évaluations correspondent à cette sélection. Choisissez l'évaluation ciblée.");
            return null;
        }

        return candidates[0];
    }

    private NotesDocumentPreviewDto AnalyseWord(byte[] content, string nomFichier)
    {
        var result = new NotesDocumentPreviewDto
        {
            FileName = nomFichier,
            SourceFormat = "Word (.docx)"
        };

        using var stream = new MemoryStream(content, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
        var tables = document.MainDocumentPart?.Document?.Body?.Elements<Table>().ToList() ?? [];
        var table = tables.FirstOrDefault();
        if (table is null)
        {
            result.Errors.Add("Aucune table de notes n'a été trouvée dans le document Word.");
            return result;
        }

        var rows = table.Elements<TableRow>().ToList();
        ExtractDocumentMetadata(ReadWordMetadata(document.MainDocumentPart), result);
        var headerIndex = -1;
        var matriculeIndex = -1;
        var nameIndex = -1;
        var noteIndex = -1;
        for (var i = 0; i < rows.Count; i++)
        {
            var headers = rows[i].Elements<TableCell>().Select(GetCellText).ToList();
            matriculeIndex = FindColumn(headers, "matricule");
            nameIndex = FindColumn(headers, "nom", "prenom");
            noteIndex = FindColumn(headers, "note");
            if (matriculeIndex >= 0 && nameIndex >= 0 && noteIndex >= 0)
            {
                headerIndex = i;
                break;
            }
        }

        if (headerIndex < 0)
        {
            result.Errors.Add("La table Word doit contenir les colonnes Matricules, Noms et prénoms et Notes/20.");
            return result;
        }

        for (var i = headerIndex + 1; i < rows.Count; i++)
        {
            var cells = rows[i].Elements<TableCell>().Select(GetCellText).ToList();
            if (cells.Count <= Math.Max(matriculeIndex, Math.Max(nameIndex, noteIndex)))
            {
                continue;
            }

            var matricule = NormalizeCell(cells[matriculeIndex]);
            var nom = NormalizeCell(cells[nameIndex]);
            if (string.IsNullOrWhiteSpace(matricule) || string.Equals(matricule, "matricules", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var ligne = BuildRow(i - headerIndex, matricule, nom, cells[noteIndex]);
            AddRow(result, ligne);
        }

        if (result.Lignes.Count == 0 && result.Errors.Count == 0)
        {
            result.Errors.Add("Aucune ligne étudiant n'a été trouvée dans la table Word.");
        }

        return result;
    }

    private async Task<NotesDocumentPreviewDto> AnalyseDocumentNumeriseAsync(
        byte[] content,
        string extension,
        string nomFichier,
        CancellationToken cancellationToken)
    {
        var result = new NotesDocumentPreviewDto
        {
            FileName = nomFichier,
            SourceFormat = "Document numérisé (OCR)"
        };

        var executable = configuration["NotesImport:OcrExecutablePath"];
        if (string.IsNullOrWhiteSpace(executable))
        {
            executable = "tesseract";
        }

        var language = configuration["NotesImport:OcrLanguage"];
        if (string.IsNullOrWhiteSpace(language))
        {
            language = "fra+eng";
        }

        var temporaryDirectory = Path.Combine(Path.GetTempPath(), "riis-notes-ocr", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
        var inputPath = Path.Combine(temporaryDirectory, $"scan{extension}");

        try
        {
            await File.WriteAllBytesAsync(inputPath, content, cancellationToken);
            var text = await RunOcrAsync(executable, language, inputPath, cancellationToken);
            ExtractDocumentMetadata(
                text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries),
                result);
            ParseOcrText(text, result);
            if (result.Lignes.Count == 0 && result.Errors.Count == 0)
            {
                result.Errors.Add("L'OCR n'a pas reconnu de ligne contenant un matricule et une note.");
            }
        }
        catch (Win32Exception)
        {
            result.Errors.Add("Le moteur OCR est introuvable. Installez Tesseract ou configurez NotesImport:OcrExecutablePath.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            result.Errors.Add($"Échec de la lecture OCR : {ex.Message}");
        }
        finally
        {
            try
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
            catch
            {
                // Le fichier temporaire sera nettoyé par le système si le scan est encore verrouillé.
            }
        }

        return result;
    }

    private static void ExtractDocumentMetadata(
        IEnumerable<string> lignes,
        NotesDocumentPreviewDto result)
    {
        var lignesNormalisees = lignes
            .Select(NormalizeCell)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        foreach (var ligne in lignesNormalisees)
        {
            TryApplyDocumentMetadata(ligne, result);
        }

        // Certains documents Word découpent le titre sur plusieurs paragraphes.
        TryApplyDocumentMetadata(string.Join(' ', lignesNormalisees), result);
    }

    private static IEnumerable<string> ReadWordMetadata(MainDocumentPart? mainDocumentPart)
    {
        if (mainDocumentPart?.Document is not null)
        {
            yield return mainDocumentPart.Document.InnerText;
            yield return string.Join(' ', mainDocumentPart.Document.Descendants<Text>().Select(x => x.Text));
            yield return ReadOpenXmlText(mainDocumentPart);

            foreach (var paragraph in mainDocumentPart.Document.Descendants<Paragraph>())
            {
                yield return paragraph.InnerText;
            }

            foreach (var cell in mainDocumentPart.Document.Descendants<TableCell>())
            {
                yield return cell.InnerText;
            }
        }

        // Les modèles Word peuvent stocker le titre dans un en-tête ou un pied
        // de page. Ces parties doivent être analysées comme le document principal.
        foreach (var headerPart in mainDocumentPart?.HeaderParts ?? [])
        {
            if (headerPart.Header is null)
            {
                continue;
            }

            yield return headerPart.Header.InnerText;
            yield return string.Join(' ', headerPart.Header.Descendants<Text>().Select(x => x.Text));
            yield return ReadOpenXmlText(headerPart);
            foreach (var paragraph in headerPart.Header.Descendants<Paragraph>())
            {
                yield return paragraph.InnerText;
            }
        }

        foreach (var footerPart in mainDocumentPart?.FooterParts ?? [])
        {
            if (footerPart.Footer is null)
            {
                continue;
            }

            yield return footerPart.Footer.InnerText;
            yield return string.Join(' ', footerPart.Footer.Descendants<Text>().Select(x => x.Text));
            yield return ReadOpenXmlText(footerPart);
            foreach (var paragraph in footerPart.Footer.Descendants<Paragraph>())
            {
                yield return paragraph.InnerText;
            }
        }
    }

    private static string ReadOpenXmlText(OpenXmlPart part)
    {
        using var stream = part.GetStream();
        var xml = XDocument.Load(stream);

        // Cette lecture contourne les cas où Word sépare un libellé entre
        // plusieurs w:r/w:t et où InnerText ne conserve pas les séparations.
        return string.Join(' ', xml
            .Descendants()
            .Where(x => x.Name.LocalName == "t")
            .Select(x => x.Value));
    }

    private static bool TryApplyDocumentMetadata(
        string texte,
        NotesDocumentPreviewDto result)
    {
        var normalized = NormalizeCell(texte);
        var metadataFound = false;
        var yearMatch = AcademicYearRegex.Match(normalized);
        if (yearMatch.Success)
        {
            result.AnneeAcademiqueLibelleExtrait = NormalizeCell(yearMatch.Groups["value"].Value)
                .Replace(" ", string.Empty, StringComparison.Ordinal);
            metadataFound = true;
        }
        var headerMatch = ReportHeaderRegex.Match(normalized);
        if (headerMatch.Success)
        {
            // Les anciens modèles de fiche laissent un emplacement vide entre
            // « NOTES » et « CYCLE ». Pour cette fiche de report, cet emplacement
            // correspond à la session normale lorsque aucun type explicite n'est
            // imprimé. Les valeurs explicites CC, CCON et SR restent prioritaires.
            result.TypeEvaluationExtrait = ParseEvaluationType(headerMatch.Groups["type"].Value)
                ?? TypeEvaluation.SessionNormale;
            result.CycleLibelleExtrait = NormalizeCell(headerMatch.Groups["cycle"].Value);
            metadataFound = true;
        }

        var filiereMatch = ReportFiliereRegex.Match(normalized);
        if (filiereMatch.Success)
        {
            result.FiliereLibelleExtrait = NormalizeCell(filiereMatch.Groups["value"].Value);
            metadataFound = true;
        }

        var classeMatch = ReportClasseRegex.Match(normalized);
        if (classeMatch.Success)
        {
            result.ClasseLibelleExtrait = NormalizeCell(classeMatch.Groups["value"].Value);
            metadataFound = true;
        }

        var semestreMatch = ReportSemestreRegex.Match(normalized);
        if (semestreMatch.Success
            && TryParseSemesterNumber(semestreMatch.Groups["value"].Value, out var semestreNumero))
        {
            result.NumeroSemestreExtrait = semestreNumero;
            result.SemestreLibelleExtrait = NormalizeCell(semestreMatch.Groups["value"].Value);
            metadataFound = true;
        }

        var ueMatch = ReportUeRegex.Match(normalized);
        if (ueMatch.Success)
        {
            result.UniteEnseignementExtrait = NormalizeCell(ueMatch.Groups["value"].Value);
            metadataFound = true;
        }

        var ecMatch = ReportEcRegex.Match(normalized);
        if (ecMatch.Success)
        {
            result.ElementConstitutifExtrait = NormalizeCell(ecMatch.Groups["value"].Value);
            metadataFound = true;
        }

        return metadataFound;
    }

    private static bool TryParseSemesterNumber(string value, out byte number)
    {
        if (byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
            && number is >= 1 and <= 8)
        {
            return true;
        }

        number = NormalizeKey(value) switch
        {
            "I" => 1,
            "II" => 2,
            "III" => 3,
            "IV" => 4,
            "V" => 5,
            "VI" => 6,
            "VII" => 7,
            "VIII" => 8,
            _ => 0
        };
        return number != 0;
    }

    private static TypeEvaluation? ParseEvaluationType(string value)
        => NormalizeKey(value) switch
        {
            "CCON" => TypeEvaluation.ControleContinu,
            "CC" => TypeEvaluation.ControleConnaissance,
            "SN" => TypeEvaluation.SessionNormale,
            "SR" => TypeEvaluation.SessionRattrapage,
            _ => null
        };

    private static string? ExtractMetadataCode(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeCell(value).Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

    private static async Task<string> RunOcrAsync(
        string executable,
        string language,
        string inputPath,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(inputPath);
        startInfo.ArgumentList.Add("stdout");
        startInfo.ArgumentList.Add("-l");
        startInfo.ArgumentList.Add(language);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Impossible de démarrer le moteur OCR.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                ? "Le moteur OCR a échoué."
                : error.Trim());
        }

        return output;
    }

    private static void ParseOcrText(string text, NotesDocumentPreviewDto result)
    {
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeCell)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        for (var i = 0; i < lines.Count; i++)
        {
            var match = OcrMatriculeRegex.Match(lines[i]);
            if (!match.Success)
            {
                continue;
            }

            var matricule = NormalizeMatricule(match.Value);
            var rest = NormalizeCell(lines[i].Substring(match.Index + match.Length));
            if (TryBuildOcrRow(i, matricule, rest, lines, out var ligne, out var consumed))
            {
                AddRow(result, ligne!);
                i += consumed;
            }
        }
    }

    private static bool TryBuildOcrRow(
        int index,
        string matricule,
        string rest,
        IReadOnlyList<string> lines,
        out NotesDocumentRowDto? ligne,
        out int consumed)
    {
        ligne = null;
        consumed = 0;
        var name = rest;
        var noteText = string.Empty;
        var noteMatch = OcrNoteRegex.Match(rest);
        if (noteMatch.Success)
        {
            noteText = noteMatch.Groups["note"].Value;
            name = NormalizeCell(rest[..noteMatch.Index]);
        }
        else if (index + 2 < lines.Count)
        {
            name = NormalizeCell(lines[index + 1]);
            noteText = NormalizeCell(lines[index + 2]);
            consumed = 2;
        }

        if (string.IsNullOrWhiteSpace(name) || !TryParseValue(noteText, out var value, out var status, out var error))
        {
            return false;
        }

        ligne = new NotesDocumentRowDto
        {
            Numero = index + 1,
            Matricule = matricule,
            NomComplet = name,
            Valeur = value,
            StatutPresence = status,
            ValeurBrute = noteText,
            Erreur = error
        };
        return true;
    }

    private static NotesDocumentRowDto BuildRow(int numero, string matricule, string nom, string valeurBrute)
    {
        TryParseValue(valeurBrute, out var value, out var status, out var error);
        return new NotesDocumentRowDto
        {
            Numero = numero,
            Matricule = NormalizeMatricule(matricule),
            NomComplet = nom,
            Valeur = value,
            StatutPresence = status,
            ValeurBrute = valeurBrute,
            Erreur = error
        };
    }

    private static void AddRow(NotesDocumentPreviewDto result, NotesDocumentRowDto ligne)
    {
        if (result.Lignes.Any(x => string.Equals(x.Matricule, ligne.Matricule, StringComparison.OrdinalIgnoreCase)))
        {
            result.Errors.Add($"Le matricule {ligne.Matricule} apparaît plusieurs fois dans le document.");
            return;
        }

        result.Lignes.Add(ligne);
        if (!string.IsNullOrWhiteSpace(ligne.Erreur))
        {
            result.Errors.Add($"Ligne {ligne.Numero}, matricule {ligne.Matricule} : {ligne.Erreur}");
        }
    }

    private static bool TryParseValue(
        string? raw,
        out decimal? value,
        out StatutPresenceEvaluation status,
        out string? error)
    {
        value = null;
        status = StatutPresenceEvaluation.Present;
        error = null;
        var normalized = NormalizeCell(raw);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            // Une cellule vide est considérée comme une note nulle.
            // Cela permet aussi au contrôle numérique de démarrer à 0.
            value = 0m;
            return true;
        }

        var token = NormalizeKey(normalized);
        if (token is "ABS" or "ABSENCE" or "A")
        {
            status = StatutPresenceEvaluation.AbsenceNonJustifiee;
            return true;
        }

        if (token is "AJ" or "ABSENCEJUSTIFIEE")
        {
            status = StatutPresenceEvaluation.AbsenceJustifiee;
            return true;
        }

        if (token is "DISP" or "DISPENSE")
        {
            status = StatutPresenceEvaluation.Dispense;
            return true;
        }

        var number = normalized.Replace(',', '.');
        if (!decimal.TryParse(number, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed))
        {
            error = $"La note '{normalized}' n'est pas numérique.";
            return false;
        }

        if (parsed < 0m || parsed > 20m)
        {
            error = $"La note '{normalized}' doit être comprise entre 0 et 20.";
            return false;
        }

        value = parsed;
        return true;
    }

    private static int FindColumn(IReadOnlyList<string> headers, params string[] tokens)
    {
        var match = headers
            .Select((header, index) => (header, index))
            .FirstOrDefault(x => tokens.All(token => NormalizeKey(x.header).Contains(NormalizeKey(token), StringComparison.Ordinal)));

        return match.header is null ? -1 : match.index;
    }

    private static string GetCellText(TableCell cell)
        => NormalizeCell(cell.InnerText);

    private static string NormalizeCell(string? value)
        => string.Join(' ', (value ?? string.Empty)
            .Replace('\u00a0', ' ')
            .Replace('\u00ad', ' ')
            .Replace('\u200b', ' ')
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string NormalizeKey(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (char.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("/", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
    }

    private static string NormalizeMatricule(string value)
        => NormalizeCell(value).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
}
