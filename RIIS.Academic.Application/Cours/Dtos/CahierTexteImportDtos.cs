using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Cours.Dtos;

/// <summary>
/// Resultat produit par le scanner/OCR ou par une saisie manuelle avant
/// rattachement aux referentiels academiques.
/// </summary>
public sealed class CahierTexteImportRequestDto
{
    public long AnneeAcademiqueId { get; init; }
    public long ParcoursAcademiqueId { get; init; }
    public long ClassePedagogiqueId { get; init; }
    public long? EnseignantUtilisateurId { get; init; }
    public string? EnseignantNom { get; init; }
    public DateOnly? DateOuverture { get; init; }
    public IReadOnlyList<CahierTextePageImportDto> Pages { get; init; } = [];
}

public sealed class CahierTextePageImportDto
{
    public int NumeroPage { get; init; }
    public string FichierOriginalUri { get; init; } = string.Empty;
    public string EmpreinteSha256 { get; init; } = string.Empty;
    public string? TexteOcr { get; init; }
    public decimal? ConfianceOcr { get; init; }
    public IReadOnlyList<SeanceCoursImportDto> Seances { get; init; } = [];
}

public sealed class SeanceCoursImportDto
{
    public int OrdreExtraction { get; init; }
    public DateOnly? DateCours { get; init; }
    public TimeOnly? HeureDebut { get; init; }
    public TimeOnly? HeureFin { get; init; }
    public NatureSeanceCours Nature { get; init; } = NatureSeanceCours.Cours;
    public long? MaquetteElementConstitutifId { get; init; }
    public string? ElementConstitutifCode { get; init; }
    public string? ElementConstitutifLibelle { get; init; }
    public string? Module { get; init; }
    public string? NumeroChapitre { get; init; }
    public string? TitreLecon { get; init; }
    public string? Paragraphe { get; init; }
    public string? PointsEssentiels { get; init; }
    public string? PointsPrincipaux { get; init; }
    public string? ExercicesApplications { get; init; }
    public string? DevoirsMaison { get; init; }
    public string? Evaluation { get; init; }
    public string? CompetencesObjectifs { get; init; }
    public string? Observation { get; init; }
    public decimal? ConfianceExtraction { get; init; }
}

public enum CahierTexteValidationSeverity
{
    Information,
    Avertissement,
    Erreur
}

public sealed class CahierTexteValidationIssueDto
{
    public CahierTexteValidationSeverity Severity { get; init; }
    public int? NumeroPage { get; init; }
    public int? OrdreExtraction { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class SeanceCoursValidationDto
{
    public int NumeroPage { get; init; }
    public int OrdreExtraction { get; init; }
    public bool EstValide { get; init; }
    public long? MaquetteElementConstitutifId { get; init; }
    public long? SemestrePedagogiqueId { get; init; }
    public long? UniteEnseignementId { get; init; }
    public long? ElementConstitutifId { get; init; }
    public string? ElementConstitutifCode { get; init; }
    public string? ElementConstitutifLibelle { get; init; }
    public IReadOnlyList<CahierTexteValidationIssueDto> Issues { get; init; } = [];
}

public sealed class CahierTexteValidationReportDto
{
    public long? CahierTexteId { get; init; }
    public IReadOnlyList<CahierTexteValidationIssueDto> Issues { get; init; } = [];
    public IReadOnlyList<SeanceCoursValidationDto> Seances { get; init; } = [];

    public int Erreurs => Issues.Count(x => x.Severity == CahierTexteValidationSeverity.Erreur)
        + Seances.SelectMany(x => x.Issues).Count(x => x.Severity == CahierTexteValidationSeverity.Erreur);

    public int Avertissements => Issues.Count(x => x.Severity == CahierTexteValidationSeverity.Avertissement)
        + Seances.SelectMany(x => x.Issues).Count(x => x.Severity == CahierTexteValidationSeverity.Avertissement);

    public bool EstValide => Erreurs == 0 && Seances.Count > 0;
}

public sealed class CahierTexteImportResultDto
{
    public long? CahierTexteId { get; init; }
    public int PagesImportees { get; init; }
    public int PagesIgnorees { get; init; }
    public IReadOnlyList<CahierTexteValidationIssueDto> Issues { get; init; } = [];
    public CahierTexteValidationReportDto? Validation { get; init; }
    public bool EstValide => Issues.All(x => x.Severity != CahierTexteValidationSeverity.Erreur);
}

public sealed class CahierTexteSeancesSaveResultDto
{
    public int SeancesEnregistrees { get; init; }
    public int SeancesIgnorees { get; init; }
    public IReadOnlyList<CahierTexteValidationIssueDto> Issues { get; init; } = [];
    public bool EstValide => Issues.All(x => x.Severity != CahierTexteValidationSeverity.Erreur);
}
