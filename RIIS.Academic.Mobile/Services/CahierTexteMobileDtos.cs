namespace RIIS.Academic.Mobile.Services;

public sealed class MobileCaptureUploadResult
{
    public string StorageKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}

public sealed class CahierTexteLookups
{
    public List<MobileLookupItem> AnneesAcademiques { get; set; } = [];
    public List<MobileClassLookupItem> Classes { get; set; } = [];
}

public sealed class MobileLookupItem
{
    public long Id { get; set; }
    public string Libelle { get; set; } = string.Empty;
}

public sealed class MobileClassLookupItem
{
    public long AnneeAcademiqueId { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public long ClassePedagogiqueId { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public string CycleLibelle { get; set; } = string.Empty;
    public string NiveauLibelle { get; set; } = string.Empty;
    public string FiliereLibelle { get; set; } = string.Empty;
    public string SpecialiteLibelle { get; set; } = string.Empty;
    public override string ToString() => Libelle;
}

public sealed class CahierTexteImportRequest
{
    public long AnneeAcademiqueId { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public long ClassePedagogiqueId { get; set; }
    public string? EnseignantNom { get; set; }
    public List<CahierTextePageImport> Pages { get; set; } = [];
}

public sealed class CahierTextePageImport
{
    public int NumeroPage { get; set; }
    public string FichierOriginalUri { get; set; } = string.Empty;
    public string? EmpreinteSha256 { get; set; }
    public string? TexteOcr { get; set; }
    public decimal? ConfianceOcr { get; set; }
    public List<SeanceCoursImport> Seances { get; set; } = [];
}

public sealed class SeanceCoursImport
{
    public int OrdreExtraction { get; set; }
    public DateOnly? DateCours { get; set; }
    public string Nature { get; set; } = "Cours";
    public string? ElementConstitutifCode { get; set; }
    public string? TitreLecon { get; set; }
    public string? Paragraphe { get; set; }
    public decimal? ConfianceExtraction { get; set; }
}

public sealed class CahierTexteImportResult
{
    public long? CahierTexteId { get; set; }
    public int PagesImportees { get; set; }
    public int PagesIgnorees { get; set; }
    public List<CahierTexteValidationIssue> Issues { get; set; } = [];
    public CahierTexteValidationReport? Validation { get; set; }
    public bool EstValide => Issues.All(x => !string.Equals(x.Severity, "Erreur", StringComparison.OrdinalIgnoreCase));
}

public sealed class CahierTexteValidationReport
{
    public long? CahierTexteId { get; set; }
    public List<CahierTexteValidationIssue> Issues { get; set; } = [];
    public List<SeanceCoursValidation> Seances { get; set; } = [];
    public int Erreurs => Issues.Count(x => string.Equals(x.Severity, "Erreur", StringComparison.OrdinalIgnoreCase))
        + Seances.SelectMany(x => x.Issues).Count(x => string.Equals(x.Severity, "Erreur", StringComparison.OrdinalIgnoreCase));
    public bool EstValide => Erreurs == 0 && Seances.Count > 0;
}

public sealed class CahierTexteValidationIssue
{
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? NumeroPage { get; set; }
    public int? OrdreExtraction { get; set; }
}

public sealed class SeanceCoursValidation
{
    public int NumeroPage { get; set; }
    public int OrdreExtraction { get; set; }
    public bool EstValide { get; set; }
    public List<CahierTexteValidationIssue> Issues { get; set; } = [];
}

public sealed class CahierTexteSeancesSaveResult
{
    public int SeancesEnregistrees { get; set; }
    public int SeancesIgnorees { get; set; }
    public List<CahierTexteValidationIssue> Issues { get; set; } = [];
    public bool EstValide => Issues.All(x => !string.Equals(x.Severity, "Erreur", StringComparison.OrdinalIgnoreCase));
}
