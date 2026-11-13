using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using RIIS.Academic.Application.Dossiers.Dtos;
using RIIS.Academic.Application.Dossiers.Services;
using RIIS.Academic.Application.Referentiels.Dtos;
using RIIS.Academic.Application.Referentiels.Services;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Documents;

public sealed class DossierEtudiantExcelExportService(
    IDossierEtudiantService dossierEtudiantService,
    IReferentielsService referentielsService) : IDossierEtudiantExcelExportService
{
    public async Task<DossierEtudiantExcelExportDto> ExporterDossiersAsync(
        long? parcoursAcademiqueId = null,
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        long? niveauEtudeId = null,
        long? etudiantId = null,
        CancellationToken cancellationToken = default)
    {
        var parcours = await referentielsService.GetParcoursAcademiquesAsync(
            anneeAcademiqueId,
            cycleFormationId,
            niveauEtudeId,
            inclureInactifs: true,
            cancellationToken: cancellationToken);

        var dossiers = await dossierEtudiantService.GetDossiersEtudiantAnnuelsAsync(
            parcoursAcademiqueId,
            cancellationToken);

        var parcoursIds = parcours.Select(x => x.Id).ToHashSet();
        dossiers = dossiers
            .Where(x => parcoursIds.Contains(x.ParcoursAcademiqueId))
            .Where(x => etudiantId is null || x.EtudiantId == etudiantId.Value)
            .ToList();

        return new DossierEtudiantExcelExportDto
        {
            FileName = BuildFileName(
                dossiers,
                parcours,
                parcoursAcademiqueId,
                anneeAcademiqueId,
                cycleFormationId,
                niveauEtudeId),
            Content = BuildWorkbook(dossiers, parcours)
        };
    }

    private static byte[] BuildWorkbook(
        IReadOnlyCollection<DossierEtudiantAnnuelDto> dossiers,
        IReadOnlyCollection<ParcoursAcademiqueDto> parcoursAcademiques)
    {
        using var workbook = new XLWorkbook();
        workbook.Properties.Author = "RIIS Academic";
        workbook.Properties.Title = "Liste des dossiers étudiants";

        var parcoursById = parcoursAcademiques.ToDictionary(x => x.Id);
        var groupedDossiers = dossiers
            .GroupBy(x => x.ParcoursAcademiqueId)
            .OrderBy(group => ResolveParcours(group.First(), parcoursById), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (groupedDossiers.Count == 0)
        {
            AddParcoursWorksheet(workbook, "Aucun dossier", null, null, []);
        }
        else
        {
            var usedWorksheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in groupedDossiers)
            {
                var parcours = parcoursById.TryGetValue(group.Key, out var parcoursDto)
                    ? parcoursDto
                    : null;

                AddParcoursWorksheet(
                    workbook,
                    ResolveParcours(group.First(), parcoursById),
                    parcours?.FiliereLibelle,
                    parcours?.SpecialiteLibelle,
                    group.OrderBy(DisplayEtudiant, StringComparer.OrdinalIgnoreCase).ToList(),
                    usedWorksheetNames);
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void AddParcoursWorksheet(
        XLWorkbook workbook,
        string parcours,
        string? filiere,
        string? specialite,
        IReadOnlyCollection<DossierEtudiantAnnuelDto> dossiers,
        HashSet<string>? usedWorksheetNames = null)
    {
        const int columnCount = 11;
        const int headerRow = 9;
        var parcoursSansAnnee = BuildParcoursDisplayName(parcours, filiere, specialite);
        var worksheetName = BuildWorksheetName(
            string.IsNullOrWhiteSpace(specialite) ? parcoursSansAnnee : specialite,
            usedWorksheetNames);
        var worksheet = workbook.Worksheets.Add(worksheetName);

        worksheet.Range(1, 1, 1, columnCount).Merge();
        worksheet.Cell(1, 1).Value = "Liste des dossiers étudiants";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1F4E79");

        SetHeaderLine(worksheet, 2, columnCount, "Année académique", FirstValue(dossiers, DisplayAnnee));
        SetHeaderLine(worksheet, 3, columnCount, "Parcours académique", parcoursSansAnnee);
        SetHeaderLine(worksheet, 4, columnCount, "Classe pédagogique", JoinDistinct(dossiers, DisplayClasse));
        SetHeaderLine(worksheet, 5, columnCount, "Filière", string.IsNullOrWhiteSpace(filiere) ? "-" : filiere);
        SetHeaderLine(worksheet, 6, columnCount, "Spécialité", string.IsNullOrWhiteSpace(specialite) ? "-" : specialite);

        SetHeaderLine(worksheet, 7, columnCount, "Total étudiants", dossiers.Count.ToString(CultureInfo.InvariantCulture));

        worksheet.Range(8, 1, 8, columnCount).Merge();
        worksheet.Cell(8, 1).Value = $"Exporté le {DateTime.Now:dd/MM/yyyy HH:mm}";
        worksheet.Cell(8, 1).Style.Font.Italic = true;

        var headers = new[]
        {
            "Matricule",
            "Étudiant",
            "Sexe",
            "Téléphone personnel",
            "Téléphone secondaire",
            "Téléphone parent 1",
            "Téléphone parent 2",
            "Dossier administratif",
            "Dossier académique",
            "Dossier financier",
            "Créé le"
        };

        for (var column = 0; column < headers.Length; column++)
        {
            worksheet.Cell(headerRow, column + 1).Value = headers[column];
        }

        var headerRange = worksheet.Range(headerRow, 1, headerRow, columnCount);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        var row = headerRow + 1;
        foreach (var dossier in dossiers)
        {
            worksheet.Cell(row, 1).Value = dossier.Matricule;
            worksheet.Cell(row, 2).Value = DisplayEtudiant(dossier);
            worksheet.Cell(row, 3).Value = DisplaySexe(dossier.Etudiant?.Sexe);
            worksheet.Cell(row, 4).Value = dossier.Etudiant?.TelephonePrincipal ?? "-";
            worksheet.Cell(row, 5).Value = dossier.Etudiant?.TelephoneSecondaire ?? "-";
            worksheet.Cell(row, 6).Value = dossier.Etudiant?.TelephoneParent1 ?? "-";
            worksheet.Cell(row, 7).Value = dossier.Etudiant?.TelephoneParent2 ?? "-";
            worksheet.Cell(row, 8).Value = DisplayStatut(dossier.Administratif?.Statut);
            worksheet.Cell(row, 9).Value = DisplayStatut(dossier.Academique?.Statut);
            worksheet.Cell(row, 10).Value = DisplayStatut(dossier.Financier?.StatutGlobal);

            var dateCreation = GetDateCreation(dossier);
            if (dateCreation is not null)
            {
                worksheet.Cell(row, 11).Value = dateCreation.Value.ToLocalTime();
                worksheet.Cell(row, 11).Style.DateFormat.Format = "dd/mm/yyyy";
            }

            row++;
        }

        var lastRow = Math.Max(headerRow, row - 1);
        var dataRange = worksheet.Range(headerRow, 1, lastRow, columnCount);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.OutsideBorderColor = XLColor.LightGray;
        dataRange.Style.Border.InsideBorderColor = XLColor.LightGray;
        dataRange.SetAutoFilter();

        worksheet.SheetView.FreezeRows(headerRow);
        worksheet.Columns().AdjustToContents();
        worksheet.Column(2).Width = Math.Min(worksheet.Column(2).Width, 34);
        worksheet.Column(3).Width = Math.Min(worksheet.Column(3).Width, 14);
        worksheet.Columns(4, 7).Width = 20;
        worksheet.Columns(8, 10).Width = 24;
        worksheet.Row(1).Height = 24;
    }

    private static void SetHeaderLine(
        IXLWorksheet worksheet,
        int row,
        int columnCount,
        string label,
        string value)
    {
        worksheet.Cell(row, 1).Value = $"{label} :";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Range(row, 2, row, columnCount).Merge();
        worksheet.Cell(row, 2).Value = value;
    }

    private static string FirstValue(
        IReadOnlyCollection<DossierEtudiantAnnuelDto> dossiers,
        Func<DossierEtudiantAnnuelDto, string> selector)
        => dossiers.Select(selector).FirstOrDefault(x => x != "-") ?? "-";

    private static string JoinDistinct(
        IReadOnlyCollection<DossierEtudiantAnnuelDto> dossiers,
        Func<DossierEtudiantAnnuelDto, string> selector)
        => string.Join(", ", dossiers
            .Select(selector)
            .Where(x => x != "-")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

    private static string BuildWorksheetName(
        string value,
        HashSet<string>? usedWorksheetNames)
    {
        var baseName = string.IsNullOrWhiteSpace(value) || value == "-"
            ? "Sans parcours"
            : value;

        foreach (var character in new[] { ':', '\\', '/', '?', '*', '[', ']' })
        {
            baseName = baseName.Replace(character, '-');
        }

        baseName = baseName.Trim();
        if (baseName.Length > 31)
        {
            baseName = baseName[..31];
        }

        if (usedWorksheetNames is null)
        {
            return baseName;
        }

        var candidate = baseName;
        var suffix = 2;
        while (!usedWorksheetNames.Add(candidate))
        {
            var suffixText = $" ({suffix++})";
            var maxBaseLength = Math.Max(1, 31 - suffixText.Length);
            candidate = $"{baseName[..Math.Min(baseName.Length, maxBaseLength)]}{suffixText}";
        }

        return candidate;
    }

    private static string DisplayEtudiant(DossierEtudiantAnnuelDto item)
        => item.Etudiant?.NomComplet
            ?? item.Academique?.EtudiantNomComplet
            ?? item.Financier?.EtudiantNomComplet
            ?? item.Administratif?.EtudiantNomComplet
            ?? "-";

    private static string DisplayParcours(DossierEtudiantAnnuelDto item)
        => item.Academique?.ParcoursLibelle
            ?? item.Financier?.ParcoursLibelle
            ?? item.Administratif?.ParcoursLibelle
            ?? "-";

    private static string ResolveParcours(
        DossierEtudiantAnnuelDto item,
        IReadOnlyDictionary<long, ParcoursAcademiqueDto> parcoursById)
        => parcoursById.TryGetValue(item.ParcoursAcademiqueId, out var parcours)
            ? parcours.Libelle
            : DisplayParcours(item);

    private static string DisplayAnnee(DossierEtudiantAnnuelDto item)
        => item.Academique?.AnneeAcademiqueLibelle
            ?? item.Financier?.AnneeAcademiqueLibelle
            ?? item.Administratif?.AnneeAcademiqueLibelle
            ?? "-";

    private static string DisplayClasse(DossierEtudiantAnnuelDto item)
        => string.IsNullOrWhiteSpace(item.Academique?.ClassePedagogiqueLibelle)
            ? "-"
            : item.Academique.ClassePedagogiqueLibelle;

    private static string DisplayStatut(object? statut)
        => statut?.ToString() ?? "-";

    private static string DisplaySexe(Sexe? sexe)
        => sexe switch
        {
            Sexe.Masculin => "M",
            Sexe.Feminin => "F",
            _ => "-"
        };

    private static DateTime? GetDateCreation(DossierEtudiantAnnuelDto item)
        => item.Academique?.CreeLeUtc
            ?? item.Administratif?.CreeLeUtc
            ?? item.Financier?.CreeLeUtc;

    private static string BuildFileName(
        IReadOnlyCollection<DossierEtudiantAnnuelDto> dossiers,
        IReadOnlyCollection<ParcoursAcademiqueDto> parcoursAcademiques,
        long? parcoursAcademiqueId,
        long? anneeAcademiqueId,
        long? cycleFormationId,
        long? niveauEtudeId)
    {
        var parcours = "tous-parcours";
        if (parcoursAcademiqueId is not null)
        {
            var parcoursDto = parcoursAcademiques.FirstOrDefault(x => x.Id == parcoursAcademiqueId.Value);
            parcours = parcoursDto is null
                ? RemoveAcademicYear(dossiers.Select(DisplayParcours).FirstOrDefault(x => x != "-") ?? "parcours")
                : BuildParcoursDisplayName(
                    parcoursDto.Libelle,
                    parcoursDto.FiliereLibelle,
                    parcoursDto.SpecialiteLibelle);
        }

        if (parcoursAcademiqueId is null)
        {
            var filters = new[]
            {
                JoinDistinctFilterValues(parcoursAcademiques.Select(x => x.AnneeAcademiqueLibelle)),
                JoinDistinctFilterValues(parcoursAcademiques.Select(x => x.CycleCode)),
                JoinDistinctFilterValues(parcoursAcademiques.Select(x => x.NiveauLibelle))
            };

            var suffix = string.Join(
                "-",
                filters
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => Slug(x!)));

            if (!string.IsNullOrWhiteSpace(suffix))
            {
                parcours = $"{parcours}-{suffix}";
            }
        }

        return $"dossiers-etudiants-{Slug(parcours)}.xlsx";
    }

    private static string? JoinDistinctFilterValues(IEnumerable<string?> values)
    {
        var distinctValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Select(Slug)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return distinctValues.Length == 0
            ? null
            : string.Join("-", distinctValues);
    }

    private static string BuildParcoursDisplayName(
        string parcours,
        string? filiere,
        string? specialite)
    {
        var parts = RemoveAcademicYear(parcours)
            .Split(" - ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !string.Equals(part, filiere, StringComparison.OrdinalIgnoreCase))
            .Where(part => !string.Equals(part, specialite, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return parts.Length == 0 ? RemoveAcademicYear(parcours) : string.Join(" - ", parts);
    }

    private static string RemoveAcademicYear(string value)
    {
        var result = Regex.Replace(
            value,
            @"^\s*\d{4}\s*[-/]\s*\d{4}\s*(?:[-–—]\s*)?",
            string.Empty,
            RegexOptions.CultureInvariant);

        return string.IsNullOrWhiteSpace(result)
            ? value.Trim()
            : result.Trim();
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
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
}
