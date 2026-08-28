using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Abstractions.Services;
using RIIS.Academic.Application.Releves.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Releves.Services;

public class RelevesNotesService(
    IRepository<DossierAcademique> dossiersAcademiques,
    IRepository<Etudiant> etudiants,
    IRepository<AnneeAcademique> anneesAcademiques,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<CycleFormation> cyclesFormation,
    IRepository<Filiere> filieres,
    IRepository<Specialite> specialites,
    IRepository<NiveauEtude> niveauxEtude,
    IRepository<MaquettePedagogique> maquettesPedagogiques,
    IRepository<SemestrePedagogique> semestresPedagogiques,
    IRepository<UniteEnseignement> unitesEnseignement,
    IRepository<ElementConstitutif> elementsConstitutifs,
    IRepository<MaquetteElementConstitutif> maquetteElementsConstitutifs,
    IRepository<EvaluationAcademique> evaluationsAcademiques,
    IRepository<NoteEvaluation> notesEvaluations,
    ICalculNotesService calculNotesService) : IRelevesNotesService
{
    public async Task<List<ReleveNoteEtudiantDisponibleDto>> GetEtudiantsDisponiblesAsync(
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        long? niveauEtudeId = null,
        long? classePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var dossierItems = await dossiersAcademiques.ListAsync(cancellationToken);
        var etudiantItems = await etudiants.ListAsync(cancellationToken);
        var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var niveauItems = await niveauxEtude.ListAsync(cancellationToken);

        if (anneeAcademiqueId is not null)
        {
            var parcoursIds = parcoursItems.Where(x => x.AnneeAcademiqueId == anneeAcademiqueId).Select(x => x.Id).ToHashSet();
            dossierItems = dossierItems.Where(x => parcoursIds.Contains(x.ParcoursAcademiqueId)).ToList();
        }

        if (cycleFormationId is not null)
        {
            var parcoursIds = parcoursItems.Where(x => x.CycleFormationId == cycleFormationId).Select(x => x.Id).ToHashSet();
            dossierItems = dossierItems.Where(x => parcoursIds.Contains(x.ParcoursAcademiqueId)).ToList();
        }

        if (niveauEtudeId is not null)
        {
            var parcoursIds = parcoursItems.Where(x => x.NiveauEtudeId == niveauEtudeId).Select(x => x.Id).ToHashSet();
            dossierItems = dossierItems.Where(x => parcoursIds.Contains(x.ParcoursAcademiqueId)).ToList();
        }

        if (classePedagogiqueId is not null)
        {
            dossierItems = dossierItems.Where(x => x.ClassePedagogiqueId == classePedagogiqueId).ToList();
        }

        return dossierItems
            .Select(x =>
            {
                var etudiant = etudiantItems.FirstOrDefault(e => e.Id == x.EtudiantId);
                var parcours = parcoursItems.FirstOrDefault(o => o.Id == x.ParcoursAcademiqueId);
                var annee = parcours is null ? null : anneeItems.FirstOrDefault(a => a.Id == parcours.AnneeAcademiqueId);
                var niveau = parcours is null ? null : niveauItems.FirstOrDefault(n => n.Id == parcours.NiveauEtudeId);

                return new ReleveNoteEtudiantDisponibleDto
                {
                    DossierAcademiqueId = x.Id,
                    Matricule = etudiant?.Matricule ?? string.Empty,
                    EtudiantNomComplet = etudiant is null ? string.Empty : $"{etudiant.Nom} {etudiant.Prenoms}",
                    ParcoursLibelle = parcours is null ? string.Empty : FormatCodeLibelle(parcours.Code, parcours.Libelle),
                    NiveauLibelle = niveau?.Libelle ?? string.Empty,
                    AnneeAcademiqueLibelle = annee?.Libelle ?? string.Empty
                };
            })
            .OrderBy(x => x.EtudiantNomComplet)
            .ToList();
    }

    public async Task<ReleveNoteAnnuelDto?> GenererReleveAnnuelAsync(
        long dossierAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        var dossierItems = await dossiersAcademiques.ListAsync(cancellationToken);
        var dossier = dossierItems.FirstOrDefault(x => x.Id == dossierAcademiqueId);

        if (dossier is null)
        {
            return null;
        }

        var etudiantItems = await etudiants.ListAsync(cancellationToken);
        var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var cycleItems = await cyclesFormation.ListAsync(cancellationToken);
        var filiereItems = await filieres.ListAsync(cancellationToken);
        var specialiteItems = await specialites.ListAsync(cancellationToken);
        var niveauItems = await niveauxEtude.ListAsync(cancellationToken);
        var maquetteItems = await maquettesPedagogiques.ListAsync(cancellationToken);
        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);
        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var maquetteEcItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var evaluationItems = await evaluationsAcademiques.ListAsync(cancellationToken);
        var noteItems = await notesEvaluations.ListAsync(cancellationToken);

        var etudiant = etudiantItems.FirstOrDefault(x => x.Id == dossier.EtudiantId)
            ?? throw new InvalidOperationException("L'etudiant du dossier academique est introuvable.");
        var parcours = parcoursItems.FirstOrDefault(x => x.Id == dossier.ParcoursAcademiqueId)
            ?? throw new InvalidOperationException("Le parcours du dossier academique est introuvable.");
        var annee = anneeItems.FirstOrDefault(x => x.Id == parcours.AnneeAcademiqueId)
            ?? throw new InvalidOperationException("L'annee academique du dossier est introuvable.");
        var cycle = cycleItems.FirstOrDefault(x => x.Id == parcours.CycleFormationId);
        var filiere = filiereItems.FirstOrDefault(x => x.Id == parcours.FiliereId);
        var specialite = specialiteItems.FirstOrDefault(x => x.Id == parcours.SpecialiteId);
        var niveau = niveauItems.FirstOrDefault(x => x.Id == parcours.NiveauEtudeId);

        var maquette = ResolveMaquette(parcours, maquetteItems);
        if (maquette is null)
        {
            throw new InvalidOperationException("Aucune maquette pedagogique n'est rattachee au parcours de ce dossier.");
        }

        var numerosSemestres = GetNumerosSemestresPourNiveau(niveau?.Numero ?? 1);

        var releve = new ReleveNoteAnnuelDto
        {
            DossierAcademiqueId = dossier.Id,
            CycleFormationCode = cycle?.Code ?? string.Empty,
            CycleFormationLibelle = cycle?.Libelle ?? string.Empty,
            EtudiantNomComplet = $"{etudiant.Nom} {etudiant.Prenoms}",
            Matricule = etudiant.Matricule ?? string.Empty,
            DateNaissance = etudiant.DateNaissance,
            LieuNaissance = etudiant.LieuNaissance,
            FiliereLibelle = filiere?.Libelle ?? string.Empty,
            SpecialiteLibelle = specialite?.Libelle,
            NiveauLibelle = niveau?.Libelle ?? string.Empty,
            AnneeAcademiqueLibelle = annee.Libelle
        };

        foreach (var numeroSemestre in numerosSemestres)
        {
            var semestre = semestreItems
                .Where(x => x.MaquettePedagogiqueId == maquette.Id && x.NumeroSemestre == numeroSemestre)
                .OrderBy(x => x.Id)
                .FirstOrDefault();

            if (semestre is null)
            {
                continue;
            }

            releve.Semestres.Add(BuildSemestreDto(
                dossier,
                annee,
                semestre,
                ueItems,
                ecItems,
                maquetteEcItems,
                evaluationItems,
                noteItems));
        }

        releve.CreditsCapitalises = releve.Semestres.Sum(x => x.CreditsCapitalises);
        releve.CreditsRequis = releve.Semestres.Sum(x => x.CreditsAttendus);
        releve.MoyenneAnnuelle = CalculerMoyenneAnnuelle(releve.Semestres);
        releve.Mention = GetMention(releve.MoyenneAnnuelle);
        releve.Decision = releve.MoyenneAnnuelle >= 10m && releve.CreditsCapitalises >= releve.CreditsRequis
            ? "ADMIS"
            : "ECHEC";

        releve.Resume = releve.Semestres
            .Select(x => new ReleveNoteResumeDto
            {
                Libelle = $"SEMESTRE {x.Numero}",
                Moyenne = x.Moyenne,
                CreditsCapitalises = x.CreditsCapitalises
            })
            .ToList();

        releve.Resume.Add(new ReleveNoteResumeDto
        {
            Libelle = "ANNUELLE",
            Moyenne = releve.MoyenneAnnuelle,
            CreditsCapitalises = releve.CreditsCapitalises
        });

        return releve;
    }

    private ReleveNoteSemestreDto BuildSemestreDto(
        DossierAcademique dossier,
        AnneeAcademique annee,
        SemestrePedagogique semestre,
        IReadOnlyCollection<UniteEnseignement> ueItems,
        IReadOnlyCollection<ElementConstitutif> ecItems,
        IReadOnlyCollection<MaquetteElementConstitutif> maquetteEcItems,
        IReadOnlyCollection<EvaluationAcademique> evaluationItems,
        IReadOnlyCollection<NoteEvaluation> noteItems)
    {
        var semestreDto = new ReleveNoteSemestreDto
        {
            Numero = semestre.NumeroSemestre,
            Libelle = $"SEMESTRE {semestre.NumeroSemestre}",
            CreditsAttendus = semestre.CreditsUE
        };

        foreach (var maquetteEc in maquetteEcItems.Where(mec => mec.SemestrePedagogiqueId == semestre.Id))
        {
            var ec = ecItems.FirstOrDefault(e => e.Id == maquetteEc.ElementConstitutifId);
            var ue = ec is null ? null : ueItems.FirstOrDefault(u => u.Id == ec.UniteEnseignementId);

            if (ec is null || ue is null)
            {
                continue;
            }

            var moyenneCcon = CalculerMoyenneType(dossier.Id, annee.Id, maquetteEc.Id, TypeEvaluation.ControleContinu, evaluationItems, noteItems);
            var moyenneCc = CalculerMoyenneType(dossier.Id, annee.Id, maquetteEc.Id, TypeEvaluation.ControleConnaissance, evaluationItems, noteItems);
            var moyenneSn = CalculerMoyenneType(dossier.Id, annee.Id, maquetteEc.Id, TypeEvaluation.SessionNormale, evaluationItems, noteItems);
            var moyenneSr = CalculerMoyenneType(dossier.Id, annee.Id, maquetteEc.Id, TypeEvaluation.SessionRattrapage, evaluationItems, noteItems);
            var moyenneSessionRetenue = moyenneSr ?? moyenneSn;
            var moyenneFinale = calculNotesService.CalculerMoyenneElementConstitutif(moyenneCcon, moyenneCc, moyenneSessionRetenue);
            var credits = calculNotesService.CalculerCreditsAcquis(maquetteEc, moyenneFinale);

            semestreDto.Lignes.Add(new ReleveNoteLigneDto
            {
                UniteEnseignementLibelle = ue.Libelle,
                ElementConstitutifCode = ec.Code,
                ElementConstitutifLibelle = ec.Libelle,
                Session = moyenneSr is not null ? "SR" : $"SN{semestre.NumeroSemestre}",
                NoteSur20 = moyenneFinale,
                Decision = moyenneFinale >= 10m ? "V" : "NV",
                CreditsCapitalises = credits,
                Grade = GetGrade(moyenneFinale)
            });
        }

        var notes = semestreDto.Lignes.Where(x => x.NoteSur20.HasValue).Select(x => x.NoteSur20!.Value).ToList();

        semestreDto.TotalNotes = Truncate2(notes.Sum());
        semestreDto.Moyenne = notes.Count == 0 ? null : Truncate2(notes.Average());
        semestreDto.CreditsCapitalises = semestreDto.Lignes.Sum(x => x.CreditsCapitalises);
        semestreDto.Grade = GetGrade(semestreDto.Moyenne);

        return semestreDto;
    }

    private static MaquettePedagogique? ResolveMaquette(
        ParcoursAcademique parcours,
        IReadOnlyCollection<MaquettePedagogique> maquettes)
        => maquettes
            .Where(x => x.ParcoursAcademiqueId == parcours.Id)
            .OrderByDescending(x => x.Statut == StatutMaquettePedagogique.Active)
            .ThenByDescending(x => x.Version)
            .FirstOrDefault();

    private static decimal? CalculerMoyenneType(
        long dossierAcademiqueId,
        long anneeAcademiqueId,
        long maquetteElementConstitutifId,
        TypeEvaluation type,
        IReadOnlyCollection<EvaluationAcademique> evaluations,
        IReadOnlyCollection<NoteEvaluation> notes)
    {
        var evaluationIds = evaluations
            .Where(x => x.AnneeAcademiqueId == anneeAcademiqueId
                && x.MaquetteElementConstitutifId == maquetteElementConstitutifId
                && x.Type == type)
            .Select(x => x.Id)
            .ToHashSet();

        var valeurs = notes
            .Where(x => evaluationIds.Contains(x.EvaluationAcademiqueId)
                && x.DossierAcademiqueId == dossierAcademiqueId
                && x.StatutPresence == StatutPresenceEvaluation.Present
                && x.Valeur.HasValue)
            .Select(x => x.Valeur!.Value)
            .ToList();

        return valeurs.Count == 0 ? null : Truncate2(valeurs.Average());
    }

    private static byte[] GetNumerosSemestresPourNiveau(byte niveauNumero)
        => niveauNumero switch
        {
            1 => [1, 2],
            2 => [3, 4],
            3 => [5, 6],
            _ => [(byte)((niveauNumero * 2) - 1), (byte)(niveauNumero * 2)]
        };

    private static decimal? CalculerMoyenneAnnuelle(IReadOnlyCollection<ReleveNoteSemestreDto> semestres)
    {
        var moyennes = semestres.Where(x => x.Moyenne.HasValue).Select(x => x.Moyenne!.Value).ToList();

        return moyennes.Count == 0 ? null : Truncate2(moyennes.Average());
    }

    private static decimal Truncate2(decimal value)
        => Math.Truncate(value * 100m) / 100m;

    private static string GetGrade(decimal? note)
    {
        if (note is null)
        {
            return string.Empty;
        }

        return note.Value switch
        {
            >= 18m => "A+",
            >= 16m => "A",
            >= 14m => "B+",
            >= 12m => "B",
            >= 10m => "C",
            >= 8m => "D",
            _ => "F"
        };
    }

    private static string GetMention(decimal? moyenne)
    {
        if (moyenne is null)
        {
            return "NON RENSEIGNE";
        }

        return moyenne.Value switch
        {
            >= 16m => "TRES BIEN",
            >= 14m => "BIEN",
            >= 12m => "ASSEZ BIEN",
            >= 10m => "PASSABLE",
            _ => "INSUFFISANT"
        };
    }

    private static string FormatCodeLibelle(string code, string libelle)
        => string.IsNullOrWhiteSpace(code) ? libelle : $"{code} - {libelle}";
}
