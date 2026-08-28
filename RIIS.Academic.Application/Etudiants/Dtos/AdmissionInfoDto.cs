using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Etudiants.Dtos;

public class AdmissionInfoDto
{
    public long Id { get; set; }
    public long EtudiantId { get; set; }
    public string? SerieBaccalaureat { get; set; }
    public short? AnneeObtentionBaccalaureat { get; set; }
    public string? MentionBaccalaureat { get; set; }
    public string? DiplomeEntree { get; set; }
    public string? SpecialiteDiplomeEntree { get; set; }
    public string? NumeroEquivalence { get; set; }
    public string? DiplomeEquivalence { get; set; }

    public static AdmissionInfoDto FromEntity(AdmissionInfo entity)
        => new()
        {
            Id = entity.Id,
            EtudiantId = entity.EtudiantId,
            SerieBaccalaureat = entity.SerieBaccalaureat,
            AnneeObtentionBaccalaureat = entity.AnneeObtentionBaccalaureat,
            MentionBaccalaureat = entity.MentionBaccalaureat,
            DiplomeEntree = entity.DiplomeEntree,
            SpecialiteDiplomeEntree = entity.SpecialiteDiplomeEntree,
            NumeroEquivalence = entity.NumeroEquivalence,
            DiplomeEquivalence = entity.DiplomeEquivalence
        };
}

public class DocumentPersonnelDto
{
    public long Id { get; set; }
    public long EtudiantId { get; set; }
    public string NomDocument { get; set; } = string.Empty;
    public string UrlFichier { get; set; } = string.Empty;
    public DateTime DateDepotUtc { get; set; }
    public DateTime? DateVerificationUtc { get; set; }
    public string? VerifiePar { get; set; }
    public string? Observation { get; set; }

    public static DocumentPersonnelDto FromEntity(DocumentPersonnel entity)
        => new()
        {
            Id = entity.Id,
            EtudiantId = entity.EtudiantId,
            NomDocument = entity.NomDocument,
            UrlFichier = entity.UrlFichier,
            DateDepotUtc = entity.DateDepotUtc,
            DateVerificationUtc = entity.DateVerificationUtc,
            VerifiePar = entity.VerifiePar,
            Observation = entity.Observation
        };
}
