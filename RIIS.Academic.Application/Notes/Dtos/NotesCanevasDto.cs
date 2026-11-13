using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Notes.Dtos;

public sealed class NotesCanevasDto
{
    public long AnneeAcademiqueId { get; set; }
    public string AnneeAcademiqueLibelle { get; set; } = string.Empty;
    public long ParcoursAcademiqueId { get; set; }
    public string ParcoursAcademiqueCode { get; set; } = string.Empty;
    public string ParcoursAcademiqueLibelle { get; set; } = string.Empty;
    public long ClassePedagogiqueId { get; set; }
    public string ClassePedagogiqueLibelle { get; set; } = string.Empty;
    public long MaquettePedagogiqueId { get; set; }
    public string MaquettePedagogiqueCode { get; set; } = string.Empty;
    public string MaquettePedagogiqueLibelle { get; set; } = string.Empty;
    public string MaquettePedagogiqueVersion { get; set; } = string.Empty;
    public long SemestrePedagogiqueId { get; set; }
    public byte NumeroSemestre { get; set; }
    public string SemestrePedagogiqueLibelle { get; set; } = string.Empty;
    public List<NotesCanevasEvaluationDto> Evaluations { get; set; } = [];
    public List<NotesCanevasEtudiantDto> Etudiants { get; set; } = [];
    public List<NotesCanevasNoteDto> Notes { get; set; } = [];
}

public sealed class NotesCanevasEvaluationDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public TypeEvaluation Type { get; set; }
    public byte Numero { get; set; }
    public decimal Bareme { get; set; }
    public decimal PonderationPourcentage { get; set; }
    public long MaquetteElementConstitutifId { get; set; }
    public long SemestrePedagogiqueId { get; set; }
    public byte NumeroSemestre { get; set; }
    public string SemestrePedagogiqueLibelle { get; set; } = string.Empty;
    public string ElementConstitutifCode { get; set; } = string.Empty;
    public string ElementConstitutifLibelle { get; set; } = string.Empty;
    public long UniteEnseignementId { get; set; }
    public string UniteEnseignementCode { get; set; } = string.Empty;
    public string UniteEnseignementLibelle { get; set; } = string.Empty;
}

public sealed class NotesCanevasEtudiantDto
{
    public long DossierAcademiqueId { get; set; }
    public long EtudiantId { get; set; }
    public string Matricule { get; set; } = string.Empty;
    public string NomComplet { get; set; } = string.Empty;
}

public sealed class NotesCanevasNoteDto
{
    public long DossierAcademiqueId { get; set; }
    public long EvaluationAcademiqueId { get; set; }
    public decimal? Valeur { get; set; }
    public StatutPresenceEvaluation StatutPresence { get; set; } = StatutPresenceEvaluation.Present;
    public string? Observation { get; set; }
}

public sealed class NotesCanevasExcelExportDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public byte[] Content { get; set; } = [];
}

public sealed class NotesCanevasImportResultDto
{
    public bool IsValid => Errors.Count == 0;
    public int NotesTraitees { get; set; }
    public int NotesIgnorees { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
