using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RIIS.Academic.Application.ProcesVerbaux.Dtos;
using RIIS.Academic.Application.ProcesVerbaux.Services;

namespace RIIS.Academic.Infrastructure.Documents;

public sealed class ProcesVerbalPdfExportService(IProcesVerbauxService procesVerbauxService)
    : IProcesVerbalPdfExportService
{
    public async Task<ProcesVerbalPdfExportDto?> ExporterProcesVerbalAsync(
        long procesVerbalId,
        CancellationToken cancellationToken = default)
    {
        var procesVerbal = await procesVerbauxService.GetProcesVerbalAsync(
            procesVerbalId,
            cancellationToken);

        if (procesVerbal is null)
        {
            return null;
        }

        return new ProcesVerbalPdfExportDto
        {
            FileName = BuildFileName(procesVerbal),
            Content = BuildDocument(procesVerbal)
        };
    }

    private static byte[] BuildDocument(ProcesVerbalDto procesVerbal)
    {
        var ecHeaders = ProcesVerbalExportProjection.BuildEcHeaders(procesVerbal);
        var labels = ProcesVerbalExportProjection.GetHeaderLabels(procesVerbal);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(style => style.FontFamily("Arial").FontSize(7));
                page.Header().Element(header => ComposeHeader(header, procesVerbal, labels));
                page.Content().Element(content => ComposeContent(content, procesVerbal, ecHeaders));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("RIIS Academic | Procès-verbal | ");
                    text.Span(DateTime.Today.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(
        IContainer container,
        ProcesVerbalDto procesVerbal,
        (string Parcours, string Classe) labels)
    {
        container.Column(column =>
        {
            column.Item().Text("RIIS ACADEMIC")
                .FontSize(15)
                .Bold()
                .FontColor("#1F4E79");
            column.Item().Text(ProcesVerbalExportProjection.BuildCompactTitle(procesVerbal))
                .FontSize(11)
                .Bold();
            column.Item().Text($"{procesVerbal.TypeLibelle} - {procesVerbal.SemestrePedagogiqueLibelle}")
                .FontSize(8);
            column.Item().PaddingTop(4).Element(content => ComposeInfoTable(content, procesVerbal, labels));
        });
    }

    private static void ComposeInfoTable(
        IContainer container,
        ProcesVerbalDto procesVerbal,
        (string Parcours, string Classe) labels)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(64);
                columns.RelativeColumn(1.8f);
                columns.ConstantColumn(64);
                columns.RelativeColumn(2.2f);
            });

            AddInfoPair(table, "Cycle", procesVerbal.CycleFormationLibelle, "Parcours", labels.Parcours);
            if (!string.IsNullOrWhiteSpace(labels.Classe))
            {
                AddInfoPair(table, "Classe", labels.Classe, "Année", procesVerbal.AnneeAcademiqueLibelle);
            }
            else
            {
                AddInfoPair(table, "Année", procesVerbal.AnneeAcademiqueLibelle, "Session", procesVerbal.CodeSession ?? string.Empty);
            }

            AddInfoPair(table, "Statut", procesVerbal.StatutLibelle, "Étudiants", procesVerbal.NombreLignes.ToString(CultureInfo.InvariantCulture));
            AddInfoPair(
                table,
                "Décisions",
                $"Validés : {procesVerbal.NombreValides} | Rattrapage : {procesVerbal.NombreRattrapage} | Non délibérés : {procesVerbal.NombreNonDeliberes}",
                string.Empty,
                string.Empty);
        });
    }

    private static void ComposeContent(
        IContainer container,
        ProcesVerbalDto procesVerbal,
        IReadOnlyCollection<ProcesVerbalExportProjection.EcHeader> ecHeaders)
    {
        container.PaddingTop(8).Element(content =>
        {
            if (ecHeaders.Count == 0)
            {
                ComposeSimpleTable(content, procesVerbal);
            }
            else
            {
                ComposeDetailsTable(content, procesVerbal, ecHeaders);
            }
        });
    }

    private static void ComposeDetailsTable(
        IContainer container,
        ProcesVerbalDto procesVerbal,
        IReadOnlyCollection<ProcesVerbalExportProjection.EcHeader> ecHeaders)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(58);
                columns.RelativeColumn(2.5f);
                foreach (var ec in ecHeaders)
                {
                    foreach (var column in ec.Columns)
                    {
                        columns.RelativeColumn(column == "MOY" ? 1.1f : 0.8f);
                    }
                }

                columns.ConstantColumn(48);
                columns.ConstantColumn(48);
                columns.ConstantColumn(38);
                columns.RelativeColumn(1.4f);
            });

            table.Header(header =>
            {
                header.Cell().RowSpan(2).Element(HeaderCell).Text("Matricule");
                header.Cell().RowSpan(2).Element(HeaderCell).Text("Étudiant");
                foreach (var ec in ecHeaders)
                {
                    header.Cell().ColumnSpan((uint)ec.Columns.Count).Element(HeaderCell).Text(ec.Label);
                }

                header.Cell().RowSpan(2).Element(HeaderCell).Text("Crédits");
                header.Cell().RowSpan(2).Element(HeaderCell).Text("Moy Gle");
                header.Cell().RowSpan(2).Element(HeaderCell).Text("Rang");
                header.Cell().RowSpan(2).Element(HeaderCell).Text("Décision");

                foreach (var ec in ecHeaders)
                {
                    foreach (var column in ec.Columns)
                    {
                        header.Cell().Element(HeaderCell).Text(column);
                    }
                }
            });

            foreach (var ligne in OrderedLines(procesVerbal))
            {
                table.Cell().Element(BodyCell).Text(ligne.Matricule);
                table.Cell().Element(BodyCell).Text(ligne.EtudiantNomComplet);

                foreach (var ec in ecHeaders)
                {
                    var detail = ligne.ElementsConstitutifs.FirstOrDefault(x => x.Key == ec.Key);
                    foreach (var column in ec.Columns)
                    {
                        var value = column == "MOY"
                            ? detail?.MoyenneFinale
                            : ProcesVerbalExportProjection.GetEvaluationAverage(detail, column);
                        table.Cell().Element(BodyCell).AlignCenter().Text(FormatNote(value));
                    }
                }

                table.Cell().Element(BodyCell).AlignCenter().Text(FormatCredit(ligne.CreditsAcquis));
                table.Cell().Element(BodyCell).AlignCenter().Text(FormatNote(ligne.MoyenneGenerale));
                table.Cell().Element(BodyCell).AlignCenter().Text(ligne.Rang?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
                table.Cell().Element(BodyCell).Text(ligne.DecisionJuryLibelle);
            }
        });
    }

    private static void ComposeSimpleTable(IContainer container, ProcesVerbalDto procesVerbal)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(38);
                columns.RelativeColumn(2.5f);
                columns.ConstantColumn(65);
                columns.ConstantColumn(60);
                columns.ConstantColumn(55);
                columns.RelativeColumn(1.4f);
            });

            table.Header(header =>
            {
                foreach (var text in new[] { "Rang", "Étudiant", "Matricule", "Moy Gle", "Crédits", "Décision" })
                {
                    header.Cell().Element(HeaderCell).Text(text);
                }
            });

            foreach (var ligne in OrderedLines(procesVerbal))
            {
                table.Cell().Element(BodyCell).AlignCenter().Text(ligne.Rang?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
                table.Cell().Element(BodyCell).Text(ligne.EtudiantNomComplet);
                table.Cell().Element(BodyCell).Text(ligne.Matricule);
                table.Cell().Element(BodyCell).AlignCenter().Text(FormatNote(ligne.MoyenneGenerale));
                table.Cell().Element(BodyCell).AlignCenter().Text(FormatCredit(ligne.CreditsAcquis));
                table.Cell().Element(BodyCell).Text(ligne.DecisionJuryLibelle);
            }
        });
    }

    private static IEnumerable<ProcesVerbalLigneDto> OrderedLines(ProcesVerbalDto procesVerbal)
        => procesVerbal.Lignes
            .OrderBy(x => x.Rang ?? int.MaxValue)
            .ThenBy(x => x.EtudiantNomComplet);

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

    private static IContainer HeaderCell(IContainer container)
        => container.Background("#1F4E79").Border(0.5f).BorderColor("#1F4E79").Padding(3).AlignCenter();

    private static IContainer BodyCell(IContainer container)
        => container.Border(0.5f).BorderColor("#BFBFBF").Padding(3);

    private static IContainer LabelCell(IContainer container)
        => container.Background("#E8F0FE").Border(0.5f).BorderColor("#BFBFBF").Padding(3);

    private static IContainer ValueCell(IContainer container)
        => container.Border(0.5f).BorderColor("#BFBFBF").Padding(3);

    private static string FormatNote(decimal? value)
        => value is null ? string.Empty : value.Value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatCredit(decimal? value)
        => value is null
            ? string.Empty
            : value.Value.ToString(value.Value == decimal.Truncate(value.Value) ? "0" : "0.##", CultureInfo.InvariantCulture);

    private static string BuildFileName(ProcesVerbalDto procesVerbal)
    {
        var name = string.Join(
            "-",
            new[]
            {
                "pv",
                procesVerbal.AnneeAcademiqueLibelle,
                procesVerbal.CodeSession,
                ProcesVerbalExportProjection.GetHeaderLabels(procesVerbal).Parcours
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => Slug(x!)));

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
