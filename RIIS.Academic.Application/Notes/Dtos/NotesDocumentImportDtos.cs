using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Notes.Dtos;

public sealed class NotesDocumentPreviewDto
{
    public string FileName { get; set; } = string.Empty;
    public string SourceFormat { get; set; } = string.Empty;
    public string? AnneeAcademiqueLibelleExtrait { get; set; }
    public TypeEvaluation? TypeEvaluationExtrait { get; set; }
    public string? CycleLibelleExtrait { get; set; }
    public string? FiliereLibelleExtrait { get; set; }
    public string? ClasseLibelleExtrait { get; set; }
    public byte? NumeroSemestreExtrait { get; set; }
    public string? SemestreLibelleExtrait { get; set; }
    public string? UniteEnseignementExtrait { get; set; }
    public string? ElementConstitutifExtrait { get; set; }
    public List<NotesDocumentRowDto> Lignes { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public bool IsValid => Errors.Count == 0 && Lignes.Count > 0;
}

public sealed class NotesDocumentRowDto
{
    public int Numero { get; set; }
    public string Matricule { get; set; } = string.Empty;
    public string NomComplet { get; set; } = string.Empty;
    public decimal? Valeur { get; set; }
    public StatutPresenceEvaluation StatutPresence { get; set; } = StatutPresenceEvaluation.Present;
    public string? ValeurBrute { get; set; }
    public string? Erreur { get; set; }
}

public sealed class NotesDocumentImportContextDto
{
    public long AnneeAcademiqueId { get; set; }
    public long ClassePedagogiqueId { get; set; }
    public long SemestrePedagogiqueId { get; set; }
    public long? UniteEnseignementId { get; set; }
    public long? ElementConstitutifId { get; set; }
    public long? EvaluationAcademiqueId { get; set; }
    public TypeEvaluation? TypeEvaluation { get; set; }
}

public sealed class NotesEvaluationContextDto
{
    public long ParcoursAcademiqueId { get; set; }
    public long MaquetteElementConstitutifId { get; set; }
}

public sealed class SaisieNotesEvaluationContextDto
{
    public long EvaluationAcademiqueId { get; set; }
    public long AnneeAcademiqueId { get; set; }
    public long ClassePedagogiqueId { get; set; }
    public long SemestrePedagogiqueId { get; set; }
    public long UniteEnseignementId { get; set; }
    public long ElementConstitutifId { get; set; }
}

public sealed class NotesDocumentImportResultDto
{
    public bool IsValid => Errors.Count == 0;
    public long EvaluationAcademiqueId { get; set; }
    public int NotesTraitees { get; set; }
    public int NotesIgnorees { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
