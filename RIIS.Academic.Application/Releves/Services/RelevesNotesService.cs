using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Abstractions.Services;
using RIIS.Academic.Application.Releves.Dtos;
using RIIS.Academic.Domain;
using QRCoder;

namespace RIIS.Academic.Application.Releves.Services;

public class RelevesNotesService(
    IRepository<DossierAcademique> dossiersAcademiques,
    IRepository<DossierEtudiant> dossiersEtudiants,
    IRepository<Etudiant> etudiants,
    IRepository<AnneeAcademique> anneesAcademiques,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<CycleFormation> cyclesFormation,
    IRepository<Filiere> filieres,
    IRepository<Specialite> specialites,
    IRepository<NiveauEtude> niveauxEtude,
    IRepository<MaquettePedagogique> maquettesPedagogiques,
    IRepository<AffectationMaquette> affectationsMaquettes,
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
        long? parcoursAcademiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var dossierItems = await dossiersAcademiques.ListAsync(cancellationToken);
        var dossierEtudiantItems = await dossiersEtudiants.ListAsync(cancellationToken);
        var etudiantItems = await etudiants.ListAsync(cancellationToken);
        var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var niveauItems = await niveauxEtude.ListAsync(cancellationToken);
        var noteItems = await notesEvaluations.ListAsync(cancellationToken);
        var dossierIdsAvecNotes = noteItems
            .Select(x => x.DossierAcademiqueId)
            .ToHashSet();

        dossierItems = dossierItems
            .Where(x => dossierIdsAvecNotes.Contains(x.Id))
            .ToList();

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

        if (parcoursAcademiqueId is not null)
        {
            dossierItems = dossierItems
                .Where(x => x.ParcoursAcademiqueId == parcoursAcademiqueId)
                .ToList();
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
                    Matricule = dossierEtudiantItems.FirstOrDefault(d =>
                        d.EtudiantId == x.EtudiantId
                        && d.ParcoursAcademiqueId == x.ParcoursAcademiqueId)?.Matricule ?? string.Empty,
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
        var dossier = await dossiersAcademiques.GetByIdAsync(
            dossierAcademiqueId,
            cancellationToken);

        if (dossier is null)
        {
            return null;
        }

        var etudiantItems = await etudiants.ListAsync(cancellationToken);
        var dossierEtudiantItems = await dossiersEtudiants.ListAsync(cancellationToken);
        var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var cycleItems = await cyclesFormation.ListAsync(cancellationToken);
        var filiereItems = await filieres.ListAsync(cancellationToken);
        var specialiteItems = await specialites.ListAsync(cancellationToken);
        var niveauItems = await niveauxEtude.ListAsync(cancellationToken);
        var maquetteItems = await maquettesPedagogiques.ListAsync(cancellationToken);
        var affectationItems = await affectationsMaquettes.ListAsync(cancellationToken);
        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);
        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var maquetteEcItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var evaluationItems = await evaluationsAcademiques.ListAsync(cancellationToken);
        var noteItems = await notesEvaluations.ListAsync(cancellationToken);
        var notesDossier = noteItems
            .Where(x => x.DossierAcademiqueId == dossier.Id)
            .ToList();

        if (notesDossier.Count == 0)
        {
            return null;
        }

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

        var maquette = ResolveMaquette(parcours, maquetteItems, affectationItems);
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
            Matricule = dossierEtudiantItems.FirstOrDefault(x =>
                x.EtudiantId == dossier.EtudiantId
                && x.ParcoursAcademiqueId == dossier.ParcoursAcademiqueId)?.Matricule ?? string.Empty,
            DateNaissance = etudiant.DateNaissance,
            LieuNaissance = etudiant.LieuNaissance,
            FiliereLibelle = filiere?.Libelle ?? string.Empty,
            SpecialiteLibelle = specialite?.Libelle,
            NiveauLibelle = niveau?.Libelle ?? string.Empty,
            AnneeAcademiqueLibelle = annee.Libelle
        };

        foreach (var numeroSemestre in numerosSemestres)
        {
            var semestres = semestreItems
                .Where(x => x.MaquettePedagogiqueId == maquette.Id && x.NumeroSemestre == numeroSemestre)
                .OrderBy(x => x.OrdreAffichage)
                .ThenBy(x => x.Id)
                .ToList();

            if (semestres.Count == 0)
            {
                continue;
            }

            var semestreDto = BuildSemestreDto(
                dossier,
                annee,
                numeroSemestre,
                semestres,
                ueItems,
                ecItems,
                maquetteEcItems,
                evaluationItems,
                notesDossier);

            if (semestreDto.Lignes.Count > 0)
            {
                releve.Semestres.Add(semestreDto);
            }
        }

        if (releve.Semestres.Count == 0)
        {
            return null;
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

        releve.QrCodeContent = BuildQrCodeContent(releve);
        releve.QrCodeDataUrl = BuildQrCodeDataUrl(releve.QrCodeContent);

        return releve;
    }

    private static string BuildQrCodeContent(ReleveNoteAnnuelDto releve)
        => string.Join(
            Environment.NewLine,
            "RIIS Academic",
            "RELEVE DE NOTES ANNUEL",
            $"Dossier: {releve.DossierAcademiqueId}",
            $"Matricule: {releve.Matricule}",
            $"Annee: {releve.AnneeAcademiqueLibelle}",
            $"Decision: {releve.Decision}");

    private static string BuildQrCodeDataUrl(string content)
    {
        using var generator = new QRCodeGenerator();
        using var qrCodeData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        var png = qrCode.GetGraphic(8);

        return $"data:image/png;base64,{Convert.ToBase64String(png)}";
    }

    private ReleveNoteSemestreDto BuildSemestreDto(
        DossierAcademique dossier,
        AnneeAcademique annee,
        byte numeroSemestre,
        IReadOnlyCollection<SemestrePedagogique> semestres,
        IReadOnlyCollection<UniteEnseignement> ueItems,
        IReadOnlyCollection<ElementConstitutif> ecItems,
        IReadOnlyCollection<MaquetteElementConstitutif> maquetteEcItems,
        IReadOnlyCollection<EvaluationAcademique> evaluationItems,
        IReadOnlyCollection<NoteEvaluation> noteItems)
    {
        var semestreIds = semestres.Select(x => x.Id).ToHashSet();
        var maquetteEcsSemestre = maquetteEcItems
            .Where(x => semestreIds.Contains(x.SemestrePedagogiqueId))
            .OrderBy(x => x.SemestrePedagogiqueId)
            .ThenBy(x => x.OrdreAffichage)
            .ToList();

        var semestreDto = new ReleveNoteSemestreDto
        {
            Numero = numeroSemestre,
            Libelle = $"SEMESTRE {numeroSemestre}",
            CreditsAttendus = maquetteEcsSemestre.Sum(x => x.Credits)
        };

        foreach (var maquetteEc in maquetteEcsSemestre)
        {
            var ec = ecItems.FirstOrDefault(e => e.Id == maquetteEc.ElementConstitutifId);
            var semestre = semestres.FirstOrDefault(x => x.Id == maquetteEc.SemestrePedagogiqueId);
            var ue = ueItems.FirstOrDefault(u => u.Id == ec?.UniteEnseignementId);

            if (ec is null || ue is null || semestre is null)
            {
                continue;
            }

            if (!HasNoteForMaquetteElement(dossier.Id, annee.Id, maquetteEc.Id, evaluationItems, noteItems))
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
                Session = moyenneSr is not null ? "SR" : $"SN{numeroSemestre}",
                NoteSur20 = moyenneFinale,
                Decision = moyenneFinale is null ? string.Empty : moyenneFinale >= 10m ? "V" : "NV",
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

    private static bool HasNoteForMaquetteElement(
        long dossierAcademiqueId,
        long anneeAcademiqueId,
        long maquetteElementConstitutifId,
        IReadOnlyCollection<EvaluationAcademique> evaluations,
        IReadOnlyCollection<NoteEvaluation> notes)
    {
        var evaluationIds = evaluations
            .Where(x => x.AnneeAcademiqueId == anneeAcademiqueId
                && x.MaquetteElementConstitutifId == maquetteElementConstitutifId)
            .Select(x => x.Id)
            .ToHashSet();

        return notes.Any(x => x.DossierAcademiqueId == dossierAcademiqueId
            && evaluationIds.Contains(x.EvaluationAcademiqueId));
    }

    private static MaquettePedagogique? ResolveMaquette(
        ParcoursAcademique parcours,
        IReadOnlyCollection<MaquettePedagogique> maquettes,
        IReadOnlyCollection<AffectationMaquette> affectations)
    {
        var affectation = affectations
            .Where(x => x.ParcoursAcademiqueId == parcours.Id)
            .OrderByDescending(x => x.EstActive)
            .ThenByDescending(x => x.EstPrincipale)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();

        if (affectation is not null)
        {
            var maquetteAffectee = maquettes.FirstOrDefault(x => x.Id == affectation.MaquettePedagogiqueId);
            if (maquetteAffectee is not null)
            {
                return maquetteAffectee;
            }
        }

        // Compatibilité avec les anciennes bases où la liaison était portée
        // directement par MaquettesPedagogiques.ParcoursAcademiqueId.
        return maquettes
            .Where(x => x.ParcoursAcademiqueId == parcours.Id)
            .OrderByDescending(x => x.Statut == StatutMaquettePedagogique.Active)
            .ThenByDescending(x => x.Version)
            .FirstOrDefault();
    }

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
