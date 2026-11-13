using System.Globalization;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RIIS.Academic.Application.Releves.Dtos;
using RIIS.Academic.Application.Releves.Services;

namespace RIIS.Academic.Infrastructure.Documents;

public class ReleveNotePdfExportService(IRelevesNotesService relevesNotesService)
    : IReleveNotePdfExportService
{
    public async Task<ReleveNotePdfExportDto?> ExporterReleveAnnuelAsync(
        long dossierAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        var releve = await relevesNotesService.GenererReleveAnnuelAsync(
            dossierAcademiqueId,
            cancellationToken);

        if (releve is null)
        {
            return null;
        }

        return new ReleveNotePdfExportDto
        {
            FileName = BuildFileName(releve),
            Content = BuildDocument(releve)
        };
    }

    private static byte[] BuildDocument(ReleveNoteAnnuelDto releve)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(style => style.FontFamily("Arial").FontSize(8.5f));
                page.Header().Element(container => ComposeHeader(container, releve));
                page.Content().Element(container => ComposeContent(container, releve));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("RIIS Academic | Relevé de notes annuel | ");
                    text.Span(DateTime.Today.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, ReleveNoteAnnuelDto releve)
    {
        var qrCode = DecodeQrCode(releve.QrCodeDataUrl);

        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("RIIS ACADEMIC")
                    .FontSize(16)
                    .Bold()
                    .FontColor("#1F4E79");
                column.Item().Text("RELEVE DE NOTES ANNUEL / ANNUAL TRANSCRIPT")
                    .FontSize(12)
                    .Bold();
                column.Item().Text($"{releve.CycleFormationLibelle} - {releve.AnneeAcademiqueLibelle}")
                    .FontSize(9);
            });

            if (qrCode.Length > 0)
            {
                row.ConstantItem(76).Image(qrCode);
            }
        });
    }

    private static void ComposeContent(IContainer container, ReleveNoteAnnuelDto releve)
    {
        container.Column(column =>
        {
            column.Spacing(7);
            column.Item().PaddingTop(8).Element(content => ComposeInfoTable(content, releve));

            foreach (var semestre in releve.Semestres.OrderBy(x => x.Numero))
            {
                column.Item().PaddingTop(5).Text(semestre.Libelle)
                    .FontSize(11)
                    .Bold()
                    .FontColor("#1F4E79");
                column.Item().Element(content => ComposeSemesterTable(content, semestre));
            }

            column.Item().PaddingTop(7).Text("RECAPITULATIF ANNUEL")
                .FontSize(11)
                .Bold()
                .FontColor("#1F4E79");
            column.Item().Element(content => ComposeResumeTable(content, releve));
            column.Item().PaddingTop(8).AlignCenter().Text(
                    $"DECISION : {releve.Decision}    MENTION : {releve.Mention}")
                .FontSize(11)
                .Bold()
                .FontColor("#1F4E79");
            column.Item().PaddingTop(12).AlignRight().Text("Le Directeur");
        });
    }

    private static void ComposeInfoTable(IContainer container, ReleveNoteAnnuelDto releve)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(92);
                columns.RelativeColumn();
                columns.ConstantColumn(92);
                columns.RelativeColumn();
            });

            AddInfoPair(table, "Nom et prénoms", releve.EtudiantNomComplet, "Matricule", releve.Matricule);
            AddInfoPair(table, "Né(e) le", releve.DateNaissance == default
                ? string.Empty
                : releve.DateNaissance.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                "Lieu", releve.LieuNaissance);
            AddInfoPair(table, "Filière", releve.FiliereLibelle, "Spécialité", releve.SpecialiteLibelle ?? string.Empty);
            AddInfoPair(table, "Niveau", releve.NiveauLibelle, "Année", releve.AnneeAcademiqueLibelle);
        });
    }

    private static void AddInfoPair(
        TableDescriptor table,
        string firstLabel,
        string firstValue,
        string secondLabel,
        string secondValue)
    {
        table.Cell().Element(LabelCell).Text(firstLabel);
        table.Cell().Element(ValueCell).Text(firstValue);
        table.Cell().Element(LabelCell).Text(secondLabel);
        table.Cell().Element(ValueCell).Text(secondValue);
    }

    private static void ComposeSemesterTable(
        IContainer container,
        ReleveNoteSemestreDto semestre)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.4f);
                columns.ConstantColumn(48);
                columns.RelativeColumn(3.2f);
                columns.ConstantColumn(48);
                columns.ConstantColumn(48);
                columns.ConstantColumn(48);
                columns.ConstantColumn(48);
                columns.ConstantColumn(42);
            });

            AddHeaderCell(table, "UE");
            AddHeaderCell(table, "Code");
            AddHeaderCell(table, "EC");
            AddHeaderCell(table, "Session");
            AddHeaderCell(table, "Note/20");
            AddHeaderCell(table, "Décision");
            AddHeaderCell(table, "Crédit");
            AddHeaderCell(table, "Grade");

            foreach (var ligne in semestre.Lignes)
            {
                AddBodyCell(table, ligne.UniteEnseignementLibelle);
                AddBodyCell(table, ligne.ElementConstitutifCode ?? string.Empty);
                AddBodyCell(table, ligne.ElementConstitutifLibelle);
                AddBodyCell(table, ligne.Session);
                AddBodyCell(table, FormatNote(ligne.NoteSur20));
                AddBodyCell(table, ligne.Decision);
                AddBodyCell(table, FormatCredit(ligne.CreditsCapitalises));
                AddBodyCell(table, ligne.Grade);
            }

            AddBodyCell(table, "TOTAL", bold: true);
            AddBodyCell(table, string.Empty, bold: true);
            AddBodyCell(table, string.Empty, bold: true);
            AddBodyCell(table, string.Empty, bold: true);
            AddBodyCell(table, FormatNote(semestre.TotalNotes), bold: true);
            AddBodyCell(table, string.Empty, bold: true);
            AddBodyCell(table, FormatCredit(semestre.CreditsCapitalises), bold: true);
            AddBodyCell(table, semestre.Grade, bold: true);
        });
    }

    private static void ComposeResumeTable(IContainer container, ReleveNoteAnnuelDto releve)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.5f);
                columns.RelativeColumn();
                columns.RelativeColumn(1.5f);
                columns.RelativeColumn(2);
            });

            AddHeaderCell(table, "Période");
            AddHeaderCell(table, "Moyenne");
            AddHeaderCell(table, "Crédits capitalisés");
            AddHeaderCell(table, "Décision / Mention");

            foreach (var item in releve.Resume)
            {
                AddBodyCell(table, item.Libelle);
                AddBodyCell(table, FormatNote(item.Moyenne));
                AddBodyCell(table, FormatCredit(item.CreditsCapitalises));
                AddBodyCell(table, item.Libelle == "ANNUELLE"
                    ? $"{releve.Decision} - {releve.Mention}"
                    : string.Empty);
            }
        });
    }

    private static void AddHeaderCell(TableDescriptor table, string text)
        => table.Cell().Element(HeaderCell).AlignCenter().Text(text).Bold().FontColor(Colors.White);

    private static void AddBodyCell(TableDescriptor table, string text, bool bold = false)
    {
        var cell = table.Cell().Element(BodyCell).AlignCenter();
        if (bold)
        {
            cell.Text(text).Bold();
        }
        else
        {
            cell.Text(text);
        }
    }

    private static IContainer LabelCell(IContainer container)
        => container.Background("#E8F0FE").Border(0.5f).BorderColor("#BFBFBF").Padding(3);

    private static IContainer ValueCell(IContainer container)
        => container.Border(0.5f).BorderColor("#BFBFBF").Padding(3);

    private static IContainer HeaderCell(IContainer container)
        => container.Background("#1F4E79").Border(0.5f).BorderColor("#1F4E79").Padding(3);

    private static IContainer BodyCell(IContainer container)
        => container.Border(0.5f).BorderColor("#BFBFBF").Padding(3);

    private static byte[] DecodeQrCode(string dataUrl)
    {
        const string prefix = "data:image/png;base64,";

        if (!dataUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        try
        {
            return Convert.FromBase64String(dataUrl[prefix.Length..]);
        }
        catch (FormatException)
        {
            return [];
        }
    }

    private static string FormatNote(decimal? value)
        => value is null ? string.Empty : value.Value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatCredit(decimal value)
        => value == decimal.Truncate(value)
            ? value.ToString("00", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string BuildFileName(ReleveNoteAnnuelDto releve)
    {
        var name = string.Join(
            "-",
            new[] { "releve", releve.Matricule, releve.EtudiantNomComplet, releve.AnneeAcademiqueLibelle }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(Slug));

        return $"{name}.pdf";
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
