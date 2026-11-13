using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Cours.Dtos;

public enum ReferentielValidationSeverity
{
    Information,
    Avertissement,
    Erreur
}

public sealed class ReferentielCoursValidationIssueDto
{
    public ReferentielValidationSeverity Severity { get; init; }
    public long? ParcoursAcademiqueId { get; init; }
    public long? MaquettePedagogiqueId { get; init; }
    public long? SemestrePedagogiqueId { get; init; }
    public long? ElementConstitutifId { get; init; }
    public string? ParcoursLibelle { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class ReferentielCoursValidationReportDto
{
    public int ParcoursAnalyses { get; init; }
    public int ParcoursExploitables { get; init; }
    public IReadOnlyList<ReferentielCoursValidationIssueDto> Issues { get; init; } = [];

    public int Erreurs => Issues.Count(x => x.Severity == ReferentielValidationSeverity.Erreur);
    public int Avertissements => Issues.Count(x => x.Severity == ReferentielValidationSeverity.Avertissement);
    public bool EstValide => Erreurs == 0;
}
