using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using RIIS.Academic.Application.Notes.Dtos;
using RIIS.Academic.Application.Notes.Services;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Documents;

public sealed class NotesCanevasExcelExportService(INotesCanevasService canevasService)
    : INotesCanevasExcelExportService
{
    public async Task<NotesCanevasExcelExportDto?> ExporterCanevasAsync(
        long anneeAcademiqueId,
        long classePedagogiqueId,
        long semestrePedagogiqueId,
        long? uniteEnseignementId = null,
        long? elementConstitutifId = null,
        TypeEvaluation? typeEvaluation = null,
        CancellationToken cancellationToken = default)
    {
        var canevas = await canevasService.GetCanevasAsync(
            anneeAcademiqueId,
            classePedagogiqueId,
            semestrePedagogiqueId,
            uniteEnseignementId,
            elementConstitutifId,
            typeEvaluation,
            cancellationToken);

        if (canevas is null)
        {
            return null;
        }

        return new NotesCanevasExcelExportDto
        {
            FileName = BuildFileName(canevas),
            Content = BuildWorkbook(canevas)
        };
    }

    private static byte[] BuildWorkbook(NotesCanevasDto canevas)
    {
        using var workbook = new XLWorkbook();
        workbook.Properties.Author = "RIIS Academic";
        workbook.Properties.Title = "Canevas de saisie des notes";

        BuildInstructionsSheet(workbook, canevas);
        BuildParametersSheet(workbook, canevas);
        BuildGridSheet(workbook, canevas);
        BuildStatusesSheet(workbook, canevas);
        BuildEvaluationsSheet(workbook, canevas);
        BuildControlSheet(workbook, canevas);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void BuildInstructionsSheet(XLWorkbook workbook, NotesCanevasDto canevas)
    {
        var sheet = workbook.Worksheets.Add("Instructions");
        sheet.Cell("A1").Value = "Canevas de saisie des notes";
        sheet.Cell("A1").Style.Font.Bold = true;
        sheet.Cell("A1").Style.Font.FontSize = 16;
        sheet.Cell("A3").Value = "Contexte";
        sheet.Cell("B3").Value = $"{canevas.AnneeAcademiqueLibelle} | {canevas.ParcoursAcademiqueLibelle} | {canevas.SemestrePedagogiqueLibelle}";
        sheet.Cell("A5").Value = "Règles";
        sheet.Cell("A6").Value = "1";
        sheet.Cell("B6").Value = "Saisir uniquement les notes dans Grille_Saisie, entre 0 et 20.";
        sheet.Cell("A7").Value = "2";
        sheet.Cell("B7").Value = "Laisser vide une note qui n'est pas encore saisie. Une cellule vide n'efface pas une note existante.";
        sheet.Cell("A8").Value = "3";
        sheet.Cell("B8").Value = "Utiliser Statuts pour les absences justifiées, absences non justifiées et dispenses.";
        sheet.Cell("A9").Value = "4";
        sheet.Cell("B9").Value = "Les colonnes techniques masquées et les moyennes sont gérées par RIIS Academic.";
        sheet.Cell("A10").Value = "5";
        sheet.Cell("B10").Value = "Le fichier doit être importé dans RIIS Academic sans renommer les feuilles ni les en-têtes techniques.";
        sheet.Columns("A").Width = 14;
        sheet.Columns("B").Width = 110;
        sheet.Rows("6:10").Height = 28;
        sheet.Range("A1:B1").Merge();
        sheet.Range("A1:B1").Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
        sheet.Range("A1:B1").Style.Font.FontColor = XLColor.White;
        sheet.Range("A3:A5").Style.Font.Bold = true;
        sheet.Range("B6:B10").Style.Alignment.WrapText = true;
    }

    private static void BuildParametersSheet(XLWorkbook workbook, NotesCanevasDto canevas)
    {
        var sheet = workbook.Worksheets.Add("ParametresImport");
        var rows = new (string Key, object Value)[]
        {
            ("Format", "RIIS-NOTES-1"),
            ("AnneeAcademiqueId", canevas.AnneeAcademiqueId),
            ("AnneeAcademique", canevas.AnneeAcademiqueLibelle),
            ("ParcoursAcademiqueId", canevas.ParcoursAcademiqueId),
            ("ParcoursAcademiqueCode", canevas.ParcoursAcademiqueCode),
            ("ParcoursAcademique", canevas.ParcoursAcademiqueLibelle),
            ("ClassePedagogiqueId", canevas.ClassePedagogiqueId),
            ("ClassePedagogique", canevas.ClassePedagogiqueLibelle),
            ("MaquettePedagogiqueId", canevas.MaquettePedagogiqueId),
            ("MaquettePedagogiqueCode", canevas.MaquettePedagogiqueCode),
            ("MaquettePedagogiqueVersion", canevas.MaquettePedagogiqueVersion),
            ("SemestrePedagogiqueId", canevas.SemestrePedagogiqueId),
            ("NumeroSemestre", canevas.NumeroSemestre),
            ("SemestrePedagogique", canevas.SemestrePedagogiqueLibelle),
            ("GenereLeUtc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture))
        };

        sheet.Cell("A1").Value = "Paramètre";
        sheet.Cell("B1").Value = "Valeur";
        for (var index = 0; index < rows.Length; index++)
        {
            sheet.Cell(index + 2, 1).Value = rows[index].Key;
            sheet.Cell(index + 2, 2).Value = rows[index].Value.ToString();
        }

        StyleHeader(sheet.Range("A1:B1"));
        sheet.Columns("A").Width = 30;
        sheet.Columns("B").Width = 70;
        sheet.SheetView.FreezeRows(1);
    }

    private static void BuildGridSheet(XLWorkbook workbook, NotesCanevasDto canevas)
    {
        var sheet = workbook.Worksheets.Add("Grille_Saisie");
        var groups = canevas.Evaluations
            .GroupBy(x => x.MaquetteElementConstitutifId)
            .ToList();
        var noteByKey = canevas.Notes.ToDictionary(
            x => (x.DossierAcademiqueId, x.EvaluationAcademiqueId));
        var evaluationColumns = new Dictionary<long, int>();
        var averageColumns = new Dictionary<long, int>();
        var currentColumn = 4;

        sheet.Cell("A1").Value = "GRILLE DE SAISIE DES NOTES";
        sheet.Cell("A1").Style.Font.Bold = true;
        sheet.Cell("A1").Style.Font.FontSize = 16;
        sheet.Cell("A2").Value = $"{canevas.AnneeAcademiqueLibelle} | {canevas.ParcoursAcademiqueLibelle} | {canevas.ClassePedagogiqueLibelle} | {canevas.SemestrePedagogiqueLibelle}";
        sheet.Cell("A4").Value = "Remplir uniquement les cellules de notes. Les moyennes sont indicatives et recalculées par l'application.";
        sheet.Cell("A4").Style.Font.Italic = true;

        sheet.Cell(7, 1).Value = "DossierAcademiqueId";
        sheet.Cell(7, 2).Value = "Matricule";
        sheet.Cell(7, 3).Value = "Nom complet";
        sheet.Cell(8, 1).Value = "TECHNIQUE";
        sheet.Cell(8, 2).Value = "IDENTITE";
        sheet.Cell(8, 3).Value = "IDENTITE";

        foreach (var group in groups)
        {
            var groupEvaluations = group.ToList();
            var firstColumn = currentColumn;
            foreach (var evaluation in groupEvaluations)
            {
                evaluationColumns[evaluation.Id] = currentColumn;
                sheet.Cell(7, currentColumn).Value = GetEvaluationHeader(evaluation);
                sheet.Cell(8, currentColumn).Value = GetEvaluationHeader(evaluation);
                sheet.Cell(9, currentColumn).Value = evaluation.Id;
                currentColumn++;
            }

            averageColumns[group.Key] = currentColumn;
            sheet.Cell(7, currentColumn).Value = "Moy EC";
            sheet.Cell(8, currentColumn).Value = "MOY";
            sheet.Cell(9, currentColumn).Value = "CALCUL";
            sheet.Range(7, firstColumn, 7, currentColumn)
                .Merge()
                .Value = $"{groupEvaluations.First().ElementConstitutifCode} - {groupEvaluations.First().ElementConstitutifLibelle}";
            currentColumn++;
        }

        var lastColumn = currentColumn - 1;
        var firstStudentRow = 10;
        for (var studentIndex = 0; studentIndex < canevas.Etudiants.Count; studentIndex++)
        {
            var student = canevas.Etudiants[studentIndex];
            var row = firstStudentRow + studentIndex;
            sheet.Cell(row, 1).Value = student.DossierAcademiqueId;
            sheet.Cell(row, 2).Value = student.Matricule;
            sheet.Cell(row, 3).Value = student.NomComplet;

            foreach (var evaluation in canevas.Evaluations)
            {
                var column = evaluationColumns[evaluation.Id];
                var note = noteByKey.GetValueOrDefault((student.DossierAcademiqueId, evaluation.Id));
                if (note?.Valeur is not null)
                {
                    sheet.Cell(row, column).Value = note.Valeur.Value;
                }
            }

            foreach (var group in groups)
            {
                var averageColumn = averageColumns[group.Key];
                sheet.Cell(row, averageColumn).FormulaA1 = BuildAverageFormula(
                    row,
                    group.ToList(),
                    evaluationColumns);
            }
        }

        sheet.Range(1, 1, 1, lastColumn).Merge();
        sheet.Range(2, 1, 2, lastColumn).Merge();
        sheet.Range(4, 1, 4, lastColumn).Merge();
        StyleHeader(sheet.Range(7, 1, 8, lastColumn));
        ApplyAcademicGroupBorders(sheet, groups, averageColumns, firstStudentRow, canevas.Etudiants.Count);
        sheet.Range(9, 1, 9, lastColumn).Style.Font.FontColor = XLColor.Gray;
        sheet.Range(9, 1, 9, lastColumn).Style.Font.FontSize = 8;
        sheet.Row(9).Hide();
        sheet.Column(1).Hide();
        sheet.Range(1, 1, 1, lastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
        sheet.Range(1, 1, 1, lastColumn).Style.Font.FontColor = XLColor.White;
        sheet.Range(1, 1, 1, lastColumn).Style.Font.Bold = true;
        sheet.Range(2, 1, 2, lastColumn).Style.Font.Bold = true;
        if (canevas.Etudiants.Count > 0)
        {
            sheet.Range(firstStudentRow, 4, firstStudentRow + canevas.Etudiants.Count - 1, lastColumn)
                .Style.NumberFormat.Format = "0.00";
            sheet.Range(firstStudentRow, 4, firstStudentRow + canevas.Etudiants.Count - 1, lastColumn)
                .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        if (canevas.Etudiants.Count > 0 && currentColumn > 4)
        {
            var firstNoteColumn = 4;
            var lastNoteColumn = currentColumn - 1;
            var validationRange = sheet.Range(
                firstStudentRow,
                firstNoteColumn,
                firstStudentRow + canevas.Etudiants.Count - 1,
                lastNoteColumn);
            validationRange.CreateDataValidation().Decimal.Between(0, 20);
        }

        for (var column = 2; column <= lastColumn; column++)
        {
            sheet.Column(column).Width = column == 3 ? 32 : 13;
        }

        if (canevas.Etudiants.Count > 0)
        {
            sheet.Range(8, 1, firstStudentRow + canevas.Etudiants.Count - 1, lastColumn).SetAutoFilter();
        }
        sheet.SheetView.FreezeRows(9);
        sheet.SheetView.FreezeColumns(3);
    }

    private static void ApplyAcademicGroupBorders(
        IXLWorksheet sheet,
        IReadOnlyList<IGrouping<long, NotesCanevasEvaluationDto>> groups,
        IReadOnlyDictionary<long, int> averageColumns,
        int firstStudentRow,
        int studentCount)
    {
        var lastRow = Math.Max(9, firstStudentRow + studentCount - 1);

        for (var index = 0; index < groups.Count; index++)
        {
            var group = groups[index];
            var groupEvaluations = group.ToList();
            var isLastGroup = index == groups.Count - 1;
            var nextGroup = isLastGroup ? null : groups[index + 1];
            var endsUe = isLastGroup
                || nextGroup!.First().UniteEnseignementId != groupEvaluations.First().UniteEnseignementId;

            var border = sheet.Range(
                7,
                averageColumns[group.Key],
                lastRow,
                averageColumns[group.Key]).Style.Border;

            border.RightBorder = endsUe
                ? XLBorderStyleValues.Medium
                : XLBorderStyleValues.Thin;
            border.RightBorderColor = endsUe
                ? XLColor.FromHtml("#1F4E79")
                : XLColor.FromHtml("#C9D2DC");
        }
    }

    private static void BuildStatusesSheet(XLWorkbook workbook, NotesCanevasDto canevas)
    {
        var sheet = workbook.Worksheets.Add("Statuts");
        var headers = new[]
        {
            "DossierAcademiqueId", "Matricule", "Nom complet", "EvaluationId", "Code evaluation", "StatutPresence", "Observation"
        };
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        var row = 2;
        foreach (var student in canevas.Etudiants)
        {
            foreach (var evaluation in canevas.Evaluations)
            {
                var note = canevas.Notes.FirstOrDefault(x =>
                    x.DossierAcademiqueId == student.DossierAcademiqueId
                    && x.EvaluationAcademiqueId == evaluation.Id);
                sheet.Cell(row, 1).Value = student.DossierAcademiqueId;
                sheet.Cell(row, 2).Value = student.Matricule;
                sheet.Cell(row, 3).Value = student.NomComplet;
                sheet.Cell(row, 4).Value = evaluation.Id;
                sheet.Cell(row, 5).Value = evaluation.Code;
                sheet.Cell(row, 6).Value = note?.StatutPresence.ToString() ?? StatutPresenceEvaluation.Present.ToString();
                sheet.Cell(row, 7).Value = note?.Observation ?? string.Empty;
                row++;
            }
        }

        StyleHeader(sheet.Range(1, 1, 1, headers.Length));
        sheet.Columns("A").Width = 20;
        sheet.Columns("B").Width = 16;
        sheet.Columns("C").Width = 32;
        sheet.Columns("D").Width = 14;
        sheet.Columns("E").Width = 18;
        sheet.Columns("F").Width = 24;
        sheet.Columns("G").Width = 45;
        if (row > 2)
        {
            sheet.Range(2, 1, row - 1, 7).SetAutoFilter();
            sheet.Range(2, 6, row - 1, 6).CreateDataValidation().List(
                $"{nameof(StatutPresenceEvaluation.Present)},{nameof(StatutPresenceEvaluation.AbsenceJustifiee)},{nameof(StatutPresenceEvaluation.AbsenceNonJustifiee)},{nameof(StatutPresenceEvaluation.Dispense)}");
        }
        sheet.SheetView.FreezeRows(1);
    }

    private static void BuildEvaluationsSheet(XLWorkbook workbook, NotesCanevasDto canevas)
    {
        var sheet = workbook.Worksheets.Add("ReferentielEvaluations");
        var headers = new[]
        {
            "EvaluationId", "Semestre", "Code", "Type", "Numero", "Libelle", "ECCode", "ECLibelle", "UECode", "UELibelle", "Bareme", "PonderationPourcentage"
        };
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        for (var index = 0; index < canevas.Evaluations.Count; index++)
        {
            var evaluation = canevas.Evaluations[index];
            var row = index + 2;
            sheet.Cell(row, 1).Value = evaluation.Id;
            sheet.Cell(row, 2).Value = $"S{evaluation.NumeroSemestre}";
            sheet.Cell(row, 3).Value = evaluation.Code;
            sheet.Cell(row, 4).Value = GetTypeCode(evaluation.Type);
            sheet.Cell(row, 5).Value = evaluation.Numero;
            sheet.Cell(row, 6).Value = evaluation.Libelle;
            sheet.Cell(row, 7).Value = evaluation.ElementConstitutifCode;
            sheet.Cell(row, 8).Value = evaluation.ElementConstitutifLibelle;
            sheet.Cell(row, 9).Value = evaluation.UniteEnseignementCode;
            sheet.Cell(row, 10).Value = evaluation.UniteEnseignementLibelle;
            sheet.Cell(row, 11).Value = evaluation.Bareme;
            sheet.Cell(row, 12).Value = evaluation.PonderationPourcentage;
        }

        StyleHeader(sheet.Range(1, 1, 1, headers.Length));
        sheet.Columns("A:B").Width = 14;
        sheet.Columns("C").Width = 18;
        sheet.Columns("D:E").Width = 12;
        sheet.Columns("F").Width = 40;
        sheet.Columns("G").Width = 14;
        sheet.Columns("H").Width = 38;
        sheet.Columns("I").Width = 14;
        sheet.Columns("J").Width = 38;
        sheet.Columns("K:L").Width = 18;
        if (canevas.Evaluations.Count > 0)
        {
        sheet.Range(1, 1, canevas.Evaluations.Count + 1, headers.Length).SetAutoFilter();
        }
        sheet.SheetView.FreezeRows(1);
    }

    private static void BuildControlSheet(XLWorkbook workbook, NotesCanevasDto canevas)
    {
        var sheet = workbook.Worksheets.Add("Controle");
        sheet.Cell("A1").Value = "Contrôles du canevas";
        sheet.Cell("A1").Style.Font.Bold = true;
        sheet.Cell("A3").Value = "Étudiants générés";
        sheet.Cell("B3").Value = canevas.Etudiants.Count;
        sheet.Cell("A4").Value = "Évaluations générées";
        sheet.Cell("B4").Value = canevas.Evaluations.Count;
        sheet.Cell("A5").Value = "Notes existantes préchargées";
        sheet.Cell("B5").Value = canevas.Notes.Count;
        sheet.Cell("A7").Value = "À vérifier avant import";
        sheet.Cell("A8").Value = "Les matricules et noms sont informatifs ; l'import utilise DossierAcademiqueId.";
        sheet.Cell("A9").Value = "Une note vide conserve la valeur déjà enregistrée.";
        sheet.Cell("A10").Value = "Les moyennes ne sont jamais importées comme des notes.";
        sheet.Columns("A").Width = 90;
        sheet.Columns("B").Width = 20;
        sheet.Range("A1:B1").Merge();
        sheet.Range("A1:B1").Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
        sheet.Range("A1:B1").Style.Font.FontColor = XLColor.White;
        sheet.Range("A7:A7").Style.Font.Bold = true;
    }

    private static string BuildAverageFormula(
        int row,
        IReadOnlyCollection<NotesCanevasEvaluationDto> evaluations,
        IReadOnlyDictionary<long, int> evaluationColumns)
    {
        var groups = new List<string>();
        var ccon = evaluations.Where(x => x.Type == TypeEvaluation.ControleContinu).ToList();
        var cc = evaluations.Where(x => x.Type == TypeEvaluation.ControleConnaissance).ToList();
        var session = evaluations.Where(x => x.Type is TypeEvaluation.SessionNormale or TypeEvaluation.SessionRattrapage).ToList();

        if (ccon.Count > 0)
        {
            groups.Add($"AVERAGE({BuildRange(row, ccon, evaluationColumns)})");
        }

        if (cc.Count > 0)
        {
            groups.Add($"AVERAGE({BuildRange(row, cc, evaluationColumns)})");
        }

        if (session.Count > 0)
        {
            var sn = session.FirstOrDefault(x => x.Type == TypeEvaluation.SessionNormale) ?? session[0];
            var sr = session.FirstOrDefault(x => x.Type == TypeEvaluation.SessionRattrapage);
            groups.Add(sr is null
                ? Cell(row, evaluationColumns[sn.Id])
                : $"IF(COUNT({Cell(row, evaluationColumns[sr.Id])})>0,{Cell(row, evaluationColumns[sr.Id])},{Cell(row, evaluationColumns[sn.Id])})");
        }

        if (groups.Count == 0)
        {
            return "=\"\"";
        }

        var average = groups.Count == 1
            ? groups[0]
            : $"AVERAGE({string.Join(",", groups)})";
        var countExpression = string.Join("+", groups.Select(x => $"COUNT({x})"));
        return $"=IF(({countExpression})=0,\"\",ROUND({average},2))";
    }

    private static string BuildRange(
        int row,
        IReadOnlyCollection<NotesCanevasEvaluationDto> evaluations,
        IReadOnlyDictionary<long, int> evaluationColumns)
    {
        var cells = evaluations.Select(x => Cell(row, evaluationColumns[x.Id])).ToList();
        return cells.Count == 1 ? cells[0] : $"{cells[0]}:{cells[^1]}";
    }

    private static string Cell(int row, int column)
        => $"{ColumnName(column)}{row}";

    private static void StyleHeader(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Alignment.WrapText = true;
    }

    private static string GetTypeCode(TypeEvaluation type)
        => type switch
        {
            TypeEvaluation.ControleContinu => "CCON",
            TypeEvaluation.ControleConnaissance => "CC",
            TypeEvaluation.SessionNormale => "SN",
            TypeEvaluation.SessionRattrapage => "SR",
            _ => "EV"
        };

    private static string GetEvaluationHeader(NotesCanevasEvaluationDto evaluation)
        => evaluation.Numero <= 1
            ? GetTypeCode(evaluation.Type)
            : $"{GetTypeCode(evaluation.Type)}{evaluation.Numero}";

    private static string BuildFileName(NotesCanevasDto canevas)
    {
        var semestre = canevas.NumeroSemestre == 0 ? "general" : $"s{canevas.NumeroSemestre}";
        var name = string.Join(
            "-",
            new[]
            {
                "canevas-notes",
                canevas.AnneeAcademiqueLibelle,
                canevas.ParcoursAcademiqueCode,
                semestre,
                canevas.ClassePedagogiqueLibelle
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(Slug));

        return $"{name}.xlsx";
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

    private static string ColumnName(int index)
    {
        var name = string.Empty;
        while (index > 0)
        {
            index--;
            name = (char)('A' + (index % 26)) + name;
            index /= 26;
        }

        return name;
    }
}
