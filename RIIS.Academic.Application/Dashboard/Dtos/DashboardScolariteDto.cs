namespace RIIS.Academic.Application.Dashboard.Dtos;

public class DashboardScolariteFilterDto
{
    public long? AnneeAcademiqueId { get; set; }
    public long? CycleFormationId { get; set; }
    public long? ParcoursAcademiqueId { get; set; }
}

public class DashboardScolariteDto
{
    public DateTime DateGenerationUtc { get; set; } = DateTime.UtcNow;
    public List<DashboardScolariteOptionDto> Annees { get; set; } = [];
    public List<DashboardScolariteOptionDto> Cycles { get; set; } = [];
    public List<DashboardScolariteOptionDto> ParcoursDisponibles { get; set; } = [];
    public DashboardScolariteKpiDto Kpi { get; set; } = new();
    public List<DashboardScolariteEvolutionDto> EvolutionEncaissements { get; set; } = [];
    public List<DashboardScolariteModeDto> RepartitionModesPaiement { get; set; } = [];
    public List<DashboardScolariteStatutDto> RepartitionStatuts { get; set; } = [];
    public List<DashboardScolariteParcoursDto> Parcours { get; set; } = [];
    public List<DashboardScolaritePaiementRecentDto> PaiementsRecents { get; set; } = [];
    public List<DashboardScolariteRetardPaiementGroupeDto> RetardsPaiement { get; set; } = [];
}

public class DashboardScolariteOptionDto
{
    public long Id { get; set; }
    public string Libelle { get; set; } = string.Empty;
}

public class DashboardScolariteKpiDto
{
    public int NombreDossiers { get; set; }
    public decimal TotalAttendu { get; set; }
    public decimal TotalEncaisse { get; set; }
    public decimal TotalAffecte { get; set; }
    public decimal TotalNonAffecte { get; set; }
    public decimal TotalRestant { get; set; }
    public decimal TauxRecouvrement { get; set; }
    public int NombreDossiersSoldes { get; set; }
    public int NombreDossiersEnCours { get; set; }
}

public class DashboardScolariteEvolutionDto
{
    public string Periode { get; set; } = string.Empty;
    public int Ordre { get; set; }
    public decimal Encaisse { get; set; }
    public decimal Affecte { get; set; }
}

public class DashboardScolariteModeDto
{
    public string Libelle { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public int Nombre { get; set; }
}

public class DashboardScolariteStatutDto
{
    public string Libelle { get; set; } = string.Empty;
    public int Nombre { get; set; }
}

public class DashboardScolariteParcoursDto
{
    public long ParcoursAcademiqueId { get; set; }
    public string Parcours { get; set; } = string.Empty;
    public int NombreDossiers { get; set; }
    public decimal TotalAttendu { get; set; }
    public decimal TotalEncaisse { get; set; }
    public decimal TotalRestant { get; set; }
    public decimal TauxRecouvrement { get; set; }
}

public class DashboardScolaritePaiementRecentDto
{
    public DateOnly DatePaiement { get; set; }
    public string Etudiant { get; set; } = string.Empty;
    public string Parcours { get; set; } = string.Empty;
    public string ModePaiement { get; set; } = string.Empty;
    public decimal Montant { get; set; }
}

public class DashboardScolariteRetardPaiementGroupeDto
{
    public long TypeElementScolariteId { get; set; }
    public string TypeElementScolariteLibelle { get; set; } = string.Empty;
    public int NombreEtudiants => Etudiants.Count;
    public List<DashboardScolariteRetardPaiementEtudiantDto> Etudiants { get; set; } = [];
}

public class DashboardScolariteRetardPaiementEtudiantDto
{
    public long DossierFinancierId { get; set; }
    public string Matricule { get; set; } = string.Empty;
    public string Etudiant { get; set; } = string.Empty;
    public string Parcours { get; set; } = string.Empty;
    public DateOnly DateNotification { get; set; }
    public decimal MontantAttendu { get; set; }
    public decimal MontantPayeAvantNotification { get; set; }
    public decimal MontantRestant { get; set; }
}
