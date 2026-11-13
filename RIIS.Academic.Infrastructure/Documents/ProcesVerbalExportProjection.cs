using RIIS.Academic.Application.ProcesVerbaux.Dtos;

namespace RIIS.Academic.Infrastructure.Documents;

/// <summary>
/// Règles communes de projection des PV exportés.
/// Un export ne doit contenir que les EC et les types d'évaluation réellement notés.
/// </summary>
internal static class ProcesVerbalExportProjection
{
    internal sealed record EcHeader(
        string Key,
        string Label,
        IReadOnlyList<string> Columns);

    internal static List<EcHeader> BuildEcHeaders(ProcesVerbalDto procesVerbal)
        => procesVerbal.Lignes
            .SelectMany(x => x.ElementsConstitutifs)
            .GroupBy(x => x.Key)
            .Select(group =>
            {
                var first = group.First();
                var columns = new List<string>();

                if (group.Any(x => x.HasCconNotes))
                {
                    columns.Add("CCON");
                }

                if (group.Any(x => x.HasCcNotes))
                {
                    columns.Add("CC");
                }

                if (group.Any(x => x.HasSnNotes))
                {
                    columns.Add("SN");
                }

                if (group.Any(x => x.HasSrNotes))
                {
                    columns.Add("SR");
                }

                // Un EC sans aucune note n'est pas exporté. MOY est conservée
                // uniquement parce qu'au moins un type possède une note.
                if (columns.Count == 0)
                {
                    return null;
                }

                columns.Add("MOY");
                return new EcHeader(first.Key, first.ElementConstitutifLibelle, columns);
            })
            .Where(x => x is not null)
            .Cast<EcHeader>()
            .ToList();

    internal static decimal? GetEvaluationAverage(
        ProcesVerbalElementConstitutifLigneDto? detail,
        string column)
        => column switch
        {
            "CCON" => detail?.MoyenneCcon,
            "CC" => detail?.MoyenneCc,
            "SN" => detail?.MoyenneSn,
            "SR" => detail?.MoyenneSr,
            _ => null
        };

    internal static (string Parcours, string Classe) GetHeaderLabels(ProcesVerbalDto procesVerbal)
    {
        var parcours = CleanHeaderValue(procesVerbal.ParcoursLibelle, procesVerbal.AnneeAcademiqueLibelle);
        var classe = CleanHeaderValue(procesVerbal.ClassePedagogiqueLibelle, procesVerbal.AnneeAcademiqueLibelle);

        return string.Equals(parcours, classe, StringComparison.OrdinalIgnoreCase)
            ? (parcours, string.Empty)
            : (parcours, classe);
    }

    internal static string BuildCompactTitle(ProcesVerbalDto procesVerbal)
    {
        var labels = GetHeaderLabels(procesVerbal);
        var periode = string.IsNullOrWhiteSpace(procesVerbal.SemestrePedagogiqueLibelle)
            ? "Année"
            : procesVerbal.SemestrePedagogiqueLibelle;

        return string.Join(
            " - ",
            new[] { "PV", periode, labels.Parcours }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    internal static string CleanHeaderValue(string? value, string academicYear)
    {
        var text = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(academicYear))
        {
            return text;
        }

        var yearPrefix = $"{academicYear.Trim()} - ";
        var secondYearIndex = text.IndexOf(yearPrefix, academicYear.Length, StringComparison.OrdinalIgnoreCase);
        if (secondYearIndex >= 0)
        {
            return text[(secondYearIndex + yearPrefix.Length)..].Trim();
        }

        return text.StartsWith(yearPrefix, StringComparison.OrdinalIgnoreCase)
            ? text[yearPrefix.Length..].Trim()
            : text;
    }
}
