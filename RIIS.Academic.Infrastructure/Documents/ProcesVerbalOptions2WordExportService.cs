using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.ProcesVerbaux.Dtos;
using RIIS.Academic.Application.ProcesVerbaux.Services;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Documents;

/// <summary>
/// Génère la fiche Options 2 à partir de la sélection de SaisieNotes.
/// Cette fiche est un support vierge de saisie manuelle et ne dépend d'aucun PV.
/// </summary>
public sealed class ProcesVerbalOptions2WordExportService(
    IRepository<AnneeAcademique> anneesAcademiques,
    IRepository<ClassePedagogique> classesPedagogiques,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<SemestrePedagogique> semestresPedagogiques,
    IRepository<AffectationMaquette> affectationsMaquettes,
    IRepository<MaquettePedagogique> maquettesPedagogiques,
    IRepository<DossierAcademique> dossiersAcademiques,
    IRepository<DossierEtudiant> dossiersEtudiants,
    IRepository<Etudiant> etudiants,
    IRepository<Filiere> filieres,
    IRepository<CycleFormation> cyclesFormation,
    IRepository<UniteEnseignement> unitesEnseignement,
    IRepository<ElementConstitutif> elementsConstitutifs,
    IRepository<MaquetteElementConstitutif> maquetteElementsConstitutifs)
    : IProcesVerbalOptions2WordExportService
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public async Task<ProcesVerbalWordExportDto?> ExporterFicheAsync(
        long anneeAcademiqueId,
        long classePedagogiqueId,
        long semestrePedagogiqueId,
        long? uniteEnseignementId = null,
        long? elementConstitutifId = null,
        TypeEvaluation? typeEvaluation = null,
        CancellationToken cancellationToken = default)
    {
        var annee = (await anneesAcademiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == anneeAcademiqueId);
        var classe = (await classesPedagogiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == classePedagogiqueId);

        if (annee is null || classe is null)
        {
            return null;
        }

        var parcours = (await parcoursAcademiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == classe.ParcoursAcademiqueId
                && x.AnneeAcademiqueId == anneeAcademiqueId);
        var semestre = (await semestresPedagogiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == semestrePedagogiqueId);

        if (parcours is null || semestre is null)
        {
            return null;
        }

        var maquette = await ResolveMaquetteAsync(parcours.Id, cancellationToken);
        if (maquette is null || semestre.MaquettePedagogiqueId != maquette.Id)
        {
            return null;
        }

        var filiere = (await filieres.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == parcours.FiliereId);
        var cycle = (await cyclesFormation.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == parcours.CycleFormationId);
        var ue = await ResolveUniteEnseignementAsync(
            uniteEnseignementId,
            elementConstitutifId,
            cancellationToken);
        var elements = await ResolveElementsConstitutifsAsync(
            semestre.Id,
            uniteEnseignementId,
            elementConstitutifId,
            cancellationToken);
        var lignes = await LoadStudentsAsync(
            parcours.Id,
            classe.Id,
            cancellationToken);

        var fiche = new FicheReportData
        {
            Filiere = filiere?.Libelle ?? string.Empty,
            Cycle = cycle?.Libelle ?? string.Empty,
            // Une fiche Options 2 sans filtre de type est une fiche de report
            // de session normale. Cela évite de produire un titre ambigu que
            // l'importeur ne pourrait pas rattacher à une évaluation.
            EvaluationType = GetTypeCode(typeEvaluation ?? TypeEvaluation.SessionNormale),
            Classe = RemoveAcademicYear(classe.Libelle),
            Semestre = semestre.NumeroSemestre,
            UniteEnseignement = ue is null
                ? string.Empty
                : FormatCodeLibelle(ue.Code, ue.Libelle),
            ElementsConstitutifs = string.Join(", ", elements),
            Lignes = lignes
        };

        return new ProcesVerbalWordExportDto
        {
            FileName = BuildFileName(fiche),
            Content = BuildDocumentFromTemplate(fiche)
        };
    }

    private async Task<MaquettePedagogique?> ResolveMaquetteAsync(
        long parcoursAcademiqueId,
        CancellationToken cancellationToken)
    {
        var affectations = (await affectationsMaquettes.ListAsync(cancellationToken))
            .Where(x => x.ParcoursAcademiqueId == parcoursAcademiqueId)
            .ToList();

        var affectation = affectations
            .Where(x => x.EstActive && x.EstPrincipale)
            .OrderByDescending(x => x.Id)
            .FirstOrDefault()
            ?? affectations
                .Where(x => x.EstActive)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault()
            ?? affectations
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

        var maquettes = await maquettesPedagogiques.ListAsync(cancellationToken);

        return affectation is not null
            ? maquettes.FirstOrDefault(x => x.Id == affectation.MaquettePedagogiqueId)
            : maquettes.FirstOrDefault(x => x.ParcoursAcademiqueId == parcoursAcademiqueId);
    }

    private async Task<UniteEnseignement?> ResolveUniteEnseignementAsync(
        long? uniteEnseignementId,
        long? elementConstitutifId,
        CancellationToken cancellationToken)
    {
        var unites = await unitesEnseignement.ListAsync(cancellationToken);

        if (uniteEnseignementId is not null)
        {
            return unites.FirstOrDefault(x => x.Id == uniteEnseignementId.Value);
        }

        if (elementConstitutifId is null)
        {
            return null;
        }

        var element = (await elementsConstitutifs.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == elementConstitutifId.Value);

        return element is null
            ? null
            : unites.FirstOrDefault(x => x.Id == element.UniteEnseignementId);
    }

    private async Task<List<string>> ResolveElementsConstitutifsAsync(
        long semestrePedagogiqueId,
        long? uniteEnseignementId,
        long? elementConstitutifId,
        CancellationToken cancellationToken)
    {
        var liens = (await maquetteElementsConstitutifs.ListAsync(cancellationToken))
            .Where(x => x.SemestrePedagogiqueId == semestrePedagogiqueId)
            .OrderBy(x => x.OrdreAffichage)
            .ThenBy(x => x.Id)
            .ToList();
        var elements = await elementsConstitutifs.ListAsync(cancellationToken);
        var elementById = elements.ToDictionary(x => x.Id);

        var selectedElements = liens
            .Select(x => elementById.TryGetValue(x.ElementConstitutifId, out var element)
                ? element
                : null)
            .Where(x => x is not null)
            .Cast<ElementConstitutif>()
            .Where(x => elementConstitutifId is null || x.Id == elementConstitutifId.Value)
            .Where(x => uniteEnseignementId is null || x.UniteEnseignementId == uniteEnseignementId.Value)
            .ToList();

        // Keep the explicit UI selection visible even when the selected EC is
        // stale after changing semester or class. The UI resets it on a normal
        // semester change, but this fallback makes the exported document
        // faithful to the values sent by the browser.
        if (selectedElements.Count == 0 && elementConstitutifId is not null)
        {
            selectedElements = elements
                .Where(x => x.Id == elementConstitutifId.Value)
                .Where(x => uniteEnseignementId is null || x.UniteEnseignementId == uniteEnseignementId.Value)
                .ToList();
        }

        return selectedElements
            .Select(x => FormatCodeLibelle(x.Code, x.Libelle))
            .ToList();
    }

    private async Task<List<FicheReportStudent>> LoadStudentsAsync(
        long parcoursAcademiqueId,
        long classePedagogiqueId,
        CancellationToken cancellationToken)
    {
        var dossierAcademiqueItems = (await dossiersAcademiques.ListAsync(cancellationToken))
            .Where(x => x.ParcoursAcademiqueId == parcoursAcademiqueId
                && (x.ClassePedagogiqueId == classePedagogiqueId || x.ClassePedagogiqueId is null)
                && x.Statut != StatutDossierAcademique.Abandonne)
            .ToList();
        var dossierEtudiants = (await dossiersEtudiants.ListAsync(cancellationToken))
            .Where(x => x.ParcoursAcademiqueId == parcoursAcademiqueId)
            .ToList();
        var etudiantItems = await etudiants.ListAsync(cancellationToken);

        var dossierAcademiqueByEtudiant = dossierAcademiqueItems
            .GroupBy(x => x.EtudiantId)
            .ToDictionary(x => x.Key, x => x.First());
        var etudiantById = etudiantItems.ToDictionary(x => x.Id);

        return dossierEtudiants
            .Where(x => dossierAcademiqueByEtudiant.ContainsKey(x.EtudiantId))
            .Select(x =>
            {
                etudiantById.TryGetValue(x.EtudiantId, out var etudiant);
                return new FicheReportStudent
                {
                    Matricule = x.Matricule,
                    NomComplet = etudiant is null
                        ? string.Empty
                        : $"{etudiant.Nom} {etudiant.Prenoms}".Trim()
                };
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.NomComplet))
            .OrderBy(x => x.NomComplet, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static byte[] BuildDocumentFromTemplate(FicheReportData fiche)
    {
        var templatePath = ResolveTemplatePath();
        using var output = new MemoryStream();

        using (var templateArchive = ZipFile.OpenRead(templatePath))
        using (var outputArchive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in templateArchive.Entries)
            {
                var outputEntry = outputArchive.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                using var inputStream = entry.Open();
                using var outputStream = outputEntry.Open();

                if (!entry.FullName.Equals("word/document.xml", StringComparison.OrdinalIgnoreCase))
                {
                    inputStream.CopyTo(outputStream);
                    continue;
                }

                var documentXml = XDocument.Load(inputStream, LoadOptions.PreserveWhitespace);
                ReplaceHeader(documentXml, fiche);
                ReplaceStudentsTable(documentXml, fiche);

                using var writer = new StreamWriter(
                    outputStream,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                documentXml.Save(writer, SaveOptions.DisableFormatting);
            }
        }

        return output.ToArray();
    }

    private static string ResolveTemplatePath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Documents", "Templates", "FicheReportOptions2.docx"),
            Path.Combine(AppContext.BaseDirectory, "RIIS.Academic.Infrastructure", "Documents", "Templates", "FicheReportOptions2.docx"),
            Path.Combine(Directory.GetCurrentDirectory(), "Documents", "Templates", "FicheReportOptions2.docx"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "RIIS.Academic.Infrastructure", "Documents", "Templates", "FicheReportOptions2.docx")
        };

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException(
                "Le modèle Word 'FicheReportOptions2.docx' est introuvable.",
                "FicheReportOptions2.docx");
    }

    private static void ReplaceHeader(XDocument documentXml, FicheReportData fiche)
    {
        ReplaceParagraph(
            documentXml,
            "FICHE DE REPORT DES NOTES",
            $"FICHE DE REPORT DES NOTES {fiche.EvaluationType} CYCLE {fiche.Cycle}".Trim());
        ReplaceParagraph(documentXml, "FILIERE:", $"FILIERE: {fiche.Filiere}");
        ReplaceParagraph(documentXml, "CLASSE:", $"CLASSE: {fiche.Classe}");
        ReplaceParagraph(documentXml, "SEMESTRE:", $"SEMESTRE: {ToRoman(fiche.Semestre)}");
        ReplaceParagraph(documentXml, "UE:", $"UE: {fiche.UniteEnseignement}");
        ReplaceOrInsertParagraph(documentXml, "UE:", "EC:", $"EC: {fiche.ElementsConstitutifs}");
    }

    private static void ReplaceParagraph(XDocument documentXml, string prefix, string replacement)
    {
        var paragraph = documentXml
            .Descendants(W + "p")
            .FirstOrDefault(x => string.Concat(x.Descendants(W + "t").Select(t => t.Value))
                .TrimStart()
                .StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        if (paragraph is null)
        {
            return;
        }

        var textNodes = paragraph.Descendants(W + "t").ToList();
        if (textNodes.Count == 0)
        {
            return;
        }

        textNodes[0].Value = replacement;
        foreach (var textNode in textNodes.Skip(1))
        {
            textNode.Value = string.Empty;
        }
    }

    private static void ReplaceOrInsertParagraph(
        XDocument documentXml,
        string anchorPrefix,
        string prefix,
        string replacement)
    {
        var paragraph = documentXml
            .Descendants(W + "p")
            .FirstOrDefault(x => string.Concat(x.Descendants(W + "t").Select(t => t.Value))
                .TrimStart()
                .StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        if (paragraph is not null)
        {
            SetParagraphText(paragraph, replacement);
            return;
        }

        var anchor = documentXml
            .Descendants(W + "p")
            .FirstOrDefault(x => string.Concat(x.Descendants(W + "t").Select(t => t.Value))
                .TrimStart()
                .StartsWith(anchorPrefix, StringComparison.OrdinalIgnoreCase));

        if (anchor is null)
        {
            return;
        }

        var inserted = new XElement(anchor);
        SetParagraphText(inserted, replacement);
        anchor.AddAfterSelf(inserted);
    }

    private static void SetParagraphText(XElement paragraph, string replacement)
    {
        var textNodes = paragraph.Descendants(W + "t").ToList();
        if (textNodes.Count == 0)
        {
            return;
        }

        textNodes[0].Value = replacement;
        foreach (var textNode in textNodes.Skip(1))
        {
            textNode.Value = string.Empty;
        }
    }

    private static void ReplaceStudentsTable(XDocument documentXml, FicheReportData fiche)
    {
        var table = documentXml.Descendants(W + "tbl").FirstOrDefault();
        if (table is null)
        {
            throw new InvalidOperationException("Le tableau des étudiants est introuvable dans la fiche de report.");
        }

        var rows = table.Elements(W + "tr").ToList();
        if (rows.Count < 2)
        {
            throw new InvalidOperationException("Le tableau de la fiche de report ne contient pas de ligne modèle.");
        }

        var dataRowTemplate = new XElement(rows[1]);
        foreach (var row in rows.Skip(1))
        {
            row.Remove();
        }

        for (var index = 0; index < fiche.Lignes.Count; index++)
        {
            var ligne = fiche.Lignes[index];
            var row = new XElement(dataRowTemplate);
            var cells = row.Elements(W + "tc").ToList();

            if (cells.Count < 4)
            {
                continue;
            }

            SetCellText(cells[0], (index + 1).ToString(CultureInfo.InvariantCulture));
            SetCellText(cells[1], ligne.Matricule);
            SetCellText(cells[2], ligne.NomComplet);
            SetCellText(cells[3], string.Empty);
            table.Add(row);
        }
    }

    private static void SetCellText(XElement cell, string value)
    {
        var textNodes = cell.Descendants(W + "t").ToList();
        if (textNodes.Count == 0)
        {
            var text = new XElement(
                W + "t",
                new XAttribute(XNamespace.Xml + "space", "preserve"),
                value);
            var run = cell.Descendants(W + "r").FirstOrDefault();

            if (run is not null)
            {
                run.Add(text);
            }
            else
            {
                var paragraph = cell.Element(W + "p") ?? new XElement(W + "p");
                if (paragraph.Parent is null)
                {
                    cell.Add(paragraph);
                }

                paragraph.Add(new XElement(W + "r", text));
            }

            return;
        }

        textNodes[0].Value = value;
        foreach (var textNode in textNodes.Skip(1))
        {
            textNode.Value = string.Empty;
        }
    }

    private static string FormatCodeLibelle(string? code, string libelle)
        => string.IsNullOrWhiteSpace(code) ? libelle : $"{code} - {libelle}";

    private static string GetTypeCode(TypeEvaluation type)
        => type switch
        {
            TypeEvaluation.ControleContinu => "CCON",
            TypeEvaluation.ControleConnaissance => "CC",
            TypeEvaluation.SessionNormale => "SN",
            TypeEvaluation.SessionRattrapage => "SR",
            _ => type.ToString()
        };

    private static string RemoveAcademicYear(string value)
        => Regex.Replace(
            value.Trim(),
            @"^\d{4}-\d{4}\s*-\s*",
            string.Empty,
            RegexOptions.CultureInvariant);

    private static string ToRoman(byte value)
        => value switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            6 => "VI",
            7 => "VII",
            8 => "VIII",
            _ => value.ToString(CultureInfo.InvariantCulture)
        };

    private static string BuildFileName(FicheReportData fiche)
    {
        var name = string.Join(
            "-",
            new[]
            {
                "fiche-report-options-2",
                fiche.Filiere,
                fiche.Classe,
                $"S{fiche.Semestre}"
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(Slug));

        return $"{name}.docx";
    }

    private static string Slug(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-')
            .ToArray();

        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-");
        }

        return slug.Trim('-');
    }

    private sealed class FicheReportData
    {
        public string Filiere { get; set; } = string.Empty;
        public string Cycle { get; set; } = string.Empty;
        public string EvaluationType { get; set; } = string.Empty;
        public string Classe { get; set; } = string.Empty;
        public byte Semestre { get; set; }
        public string UniteEnseignement { get; set; } = string.Empty;
        public string ElementsConstitutifs { get; set; } = string.Empty;
        public List<FicheReportStudent> Lignes { get; set; } = [];
    }

    private sealed class FicheReportStudent
    {
        public string Matricule { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
    }
}
