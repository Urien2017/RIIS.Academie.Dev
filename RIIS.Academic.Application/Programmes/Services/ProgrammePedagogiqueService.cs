using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Common.Dtos;
using RIIS.Academic.Application.Programmes.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Programmes.Services;

public class ProgrammePedagogiqueService(
    IRepository<MaquettePedagogique> maquettes,
    IRepository<AffectationMaquette> affectationsMaquettes,
    IRepository<SemestrePedagogique> semestres,
    IRepository<UniteEnseignement> unitesEnseignement,
    IRepository<ElementConstitutif> elementsConstitutifs,
    IRepository<MaquetteElementConstitutif> maquetteElementsConstitutifs,
    IRepository<AnneeAcademique> anneesAcademiques,
    IRepository<CycleFormation> cyclesFormation,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<ClassePedagogique> classesPedagogiques,
    IRepository<NiveauEtude> niveauxEtude,
    IRepository<EvaluationAcademique> evaluationsAcademiques,
    IRepository<ResultatElementConstitutif> resultatsElementsConstitutifs) : IProgrammePedagogiqueService
{
    public async Task<List<MaquettePedagogiqueDto>> GetMaquettesAsync(CancellationToken cancellationToken = default)
    {
        var maquetteItems = await maquettes.ListAsync(cancellationToken);
        var ouvertureItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var affectationItems = await affectationsMaquettes.ListAsync(cancellationToken);
        var semestreItems = await semestres.ListAsync(cancellationToken);

        return maquetteItems
            .Select(x => ToDto(x, affectationItems, ouvertureItems, semestreItems))
            .OrderBy(x => x.ParcoursAcademiquesLibelle)
            .ThenBy(x => x.Libelle)
            .ThenBy(x => x.Version)
            .ToList();
    }

    public async Task<List<MaquettePedagogiqueHierarchyDto>> GetMaquettesHierarchyAsync(
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        long? niveauEtudeId = null,
        long? maquettePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var maquetteItems = await maquettes.ListAsync(cancellationToken);
        var affectationItems = await affectationsMaquettes.ListAsync(cancellationToken);
        var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
        var ouvertureItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var niveauItems = await niveauxEtude.ListAsync(cancellationToken);
        var classeItems = await classesPedagogiques.ListAsync(cancellationToken);
        var semestreItems = await semestres.ListAsync(cancellationToken);
        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var maquetteEcItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);

        maquetteItems = FilterMaquettes(
            maquetteItems,
            affectationItems,
            anneeItems,
            ouvertureItems,
            classeItems,
            semestreItems,
            niveauItems,
            anneeAcademiqueId,
            cycleFormationId,
            niveauEtudeId,
            maquettePedagogiqueId);

        return maquetteItems
            .OrderBy(x => x.Libelle)
            .ThenBy(x => x.Version)
            .Select(maquette =>
            {
                var affectations = affectationItems.Where(x => x.MaquettePedagogiqueId == maquette.Id).ToList();
                var parcoursAffectes = ouvertureItems.Where(x => affectations.Any(a => a.ParcoursAcademiqueId == x.Id)).ToList();

                return new MaquettePedagogiqueHierarchyDto
                {
                    Id = maquette.Id,
                    ParcoursAcademiquesLibelle = string.Join(", ", parcoursAffectes.Select(x => FormatCodeLibelle(x.Code, x.Libelle))),
                    NombreAffectations = parcoursAffectes.Count,
                    Code = maquette.Code,
                    Libelle = maquette.Libelle,
                    Version = maquette.Version,
                    Statut = maquette.Statut,
                    DateDebutValidite = maquette.DateDebutValidite,
                    DateFinValidite = maquette.DateFinValidite,
                    SourceDocument = maquette.SourceDocument,
                    Observation = maquette.Observation,
                    Semestres = semestreItems
                        .Where(semestre => semestre.MaquettePedagogiqueId == maquette.Id)
                        .Where(semestre => IsSemestreForNiveau(semestre, niveauEtudeId, niveauItems))
                        .OrderBy(semestre => semestre.OrdreAffichage)
                        .ThenBy(semestre => semestre.NumeroSemestre)
                        .Select(semestre =>
                        {
                            var maquetteEcsForSemestre = maquetteEcItems
                                .Where(mec => mec.SemestrePedagogiqueId == semestre.Id)
                                .ToList();

                            var ueGroups = maquetteEcsForSemestre
                                .GroupBy(mec => mec.ElementConstitutifId)
                                .Select(g => g.First())
                                .Select(mec =>
                                {
                                    var ec = ecItems.FirstOrDefault(e => e.Id == mec.ElementConstitutifId);
                                    var ue = ec is null ? null : ueItems.FirstOrDefault(u => u.Id == ec.UniteEnseignementId);
                                    return new { Ue = ue, Ec = ec, Mec = mec };
                                })
                                .Where(x => x.Ue is not null)
                                .GroupBy(x => x.Ue!.Id)
                                .ToList();

                            return new SemestrePedagogiqueHierarchyDto
                            {
                                Id = semestre.Id,
                                NumeroSemestre = semestre.NumeroSemestre,
                                Libelle = semestre.Libelle,
                                NiveauEtudeLibelle = GetNiveauForSemestre(semestre, niveauItems)?.Libelle ?? string.Empty,
                                CreditsUE = maquetteEcsForSemestre
                                    .Select(mec => ecItems.FirstOrDefault(ec => ec.Id == mec.ElementConstitutifId))
                                    .Where(ec => ec is not null)
                                    .Sum(ec => ec!.Credits),
                                VolumeHoraireUE = (short)maquetteEcsForSemestre
                                    .Select(mec => ecItems.FirstOrDefault(ec => ec.Id == mec.ElementConstitutifId))
                                    .Where(ec => ec is not null)
                                    .Sum(ec => ec!.VolumeHoraire),
                                OrdreAffichage = semestre.OrdreAffichage,
                                UnitesEnseignement = ueGroups
                                    .Select(group =>
                                    {
                                        var first = group.First();
                                        var ue = first.Ue!;

                                        var ueMaquetteEcs = maquetteEcItems
                                            .Where(mec => mec.SemestrePedagogiqueId == semestre.Id)
                                            .Where(mec =>
                                            {
                                                var mecEc = ecItems.FirstOrDefault(e => e.Id == mec.ElementConstitutifId);
                                                return mecEc is not null && mecEc.UniteEnseignementId == ue.Id;
                                            })
                                            .ToList();

                                        return new UniteEnseignementHierarchyDto
                                        {
                                            Id = ue.Id,
                                            SemestrePedagogiqueId = semestre.Id,
                                            SemestrePedagogiqueLibelle = $"{semestre.NumeroSemestre} - {semestre.Libelle}",
                                            Code = ue.Code,
                                            Libelle = ue.Libelle,
                                            Credits = ueMaquetteEcs.Sum(m => m.Credits),
                                            VolumeHoraire = (short)ueMaquetteEcs.Sum(m => m.VolumeHoraire),
                                            OrdreAffichage = ueMaquetteEcs.Min(m => m.OrdreAffichage),
                                            EstObligatoire = ueMaquetteEcs.All(m => m.EstObligatoire),
                                            ElementsConstitutifs = ueMaquetteEcs
                                                .OrderBy(m => m.OrdreAffichage)
                                                .Select(m =>
                                                {
                                                    var mecEc = ecItems.FirstOrDefault(e => e.Id == m.ElementConstitutifId);
                                                    return new ElementConstitutifHierarchyDto
                                                    {
                                                        Id = m.ElementConstitutifId,
                                                        MaquetteElementConstitutifId = m.Id,
                                                        Code = mecEc?.Code,
                                                        Libelle = mecEc?.Libelle ?? string.Empty,
                                                        Type = mecEc?.Type ?? TypeElementConstitutif.CoursMagistraux,
                                                        Credits = m.Credits,
                                                        Coefficient = m.Coefficient,
                                                        VolumeHoraire = m.VolumeHoraire,
                                                        OrdreAffichage = m.OrdreAffichage,
                                                        EstObligatoire = m.EstObligatoire,
                                                        Observation = m.Observation
                                                    };
                                                })
                                                .ToList()
                                        };
                                    })
                                    .ToList()
                            };
                        })
                        .ToList()
                };
            })
            .ToList();
    }

    public async Task<MaquettePedagogiqueDto?> GetMaquetteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await maquettes.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        var ouvertureItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var affectationItems = await affectationsMaquettes.ListAsync(cancellationToken);
        var semestreItems = await semestres.ListAsync(cancellationToken);

        return ToDto(entity, affectationItems, ouvertureItems, semestreItems);
    }

    public MaquettePedagogiqueDto CreateDefaultMaquette()
        => new()
        {
            Version = "V1",
            Statut = StatutMaquettePedagogique.Brouillon
        };

    public async Task<string> GenerateMaquetteCodeAsync(
        string? libelle,
        long excludeId = 0,
        CancellationToken cancellationToken = default)
    {
        var key = NormalizeLibelleKey(libelle);
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        var ignoredWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DE", "DES", "DU", "D", "ET", "EN", "A", "AU", "AUX", "LA", "LE", "LES",
            "PROGRAMME", "PROGRAMMES", "FORMATION", "FORMATIONS"
        };
        var words = key.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !ignoredWords.Contains(x))
            .ToList();

        var token = string.Concat(words
                .Take(4)
                .Select(x => x[..Math.Min(3, x.Length)]))
            .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(token)) token = "MAQUETTE";

        var baseCode = $"MAQ-{token}";
        var existingCodes = (await maquettes.ListAsync(cancellationToken))
            .Where(x => x.Id != excludeId)
            .Select(x => x.Code)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existingCodes.Contains(baseCode)) return baseCode;

        for (var suffix = 2; suffix <= 9999; suffix++)
        {
            var candidate = $"{baseCode}-{suffix}";
            if (!existingCodes.Contains(candidate)) return candidate;
        }

        throw new InvalidOperationException("Impossible de générer un code unique pour la maquette.");
    }

    public async Task SaveMaquetteAsync(MaquettePedagogiqueDto dto, CancellationToken cancellationToken = default)
    {
        dto.Code = NormalizeCode(dto.Code, "Le code de la maquette est obligatoire.");
        dto.Libelle = RequireText(dto.Libelle, "Le libellé de la maquette est obligatoire.");
        dto.Version = RequireText(dto.Version, "La version de la maquette est obligatoire.").ToUpperInvariant();
        dto.SourceDocument = NormalizeNullable(dto.SourceDocument);
        dto.Observation = NormalizeNullable(dto.Observation);

        if (dto.DateDebutValidite is not null
            && dto.DateFinValidite is not null
            && dto.DateFinValidite < dto.DateDebutValidite)
        {
            throw new InvalidOperationException("La date de fin de validité doit être supérieure ou égale à la date de début.");
        }

        var existing = await maquettes.ListAsync(cancellationToken);
        var duplicate = existing.Any(x =>
            x.Id != dto.Id
            && string.Equals(x.Code, dto.Code, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Version, dto.Version, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            throw new InvalidOperationException("Une maquette avec le même code et la même version existe déjà.");
        }

        if (dto.Id == 0)
        {
            await maquettes.AddAsync(new MaquettePedagogique
            {
                Code = dto.Code,
                Libelle = dto.Libelle,
                Version = dto.Version,
                Statut = dto.Statut,
                DateDebutValidite = dto.DateDebutValidite,
                DateFinValidite = dto.DateFinValidite,
                SourceDocument = dto.SourceDocument,
                Observation = dto.Observation
            }, cancellationToken);
        }
        else
        {
            var entity = await maquettes.GetByIdAsync(dto.Id, cancellationToken);
            if (entity is null) return;

            entity.Code = dto.Code;
            entity.Libelle = dto.Libelle;
            entity.Version = dto.Version;
            entity.Statut = dto.Statut;
            entity.DateDebutValidite = dto.DateDebutValidite;
            entity.DateFinValidite = dto.DateFinValidite;
            entity.SourceDocument = dto.SourceDocument;
            entity.Observation = dto.Observation;
        }

        await maquettes.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMaquetteAsync(long id, CancellationToken cancellationToken = default)
    {
        await maquettes.DeleteByIdAsync(id, cancellationToken);
        await maquettes.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AffectationMaquetteDto>> GetAffectationsMaquettesAsync(
        long? maquettePedagogiqueId = null,
        long? parcoursAcademiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var affectationItems = await affectationsMaquettes.ListAsync(cancellationToken);
        var maquetteItems = await maquettes.ListAsync(cancellationToken);
        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);

        return affectationItems
            .Where(x => maquettePedagogiqueId is null || x.MaquettePedagogiqueId == maquettePedagogiqueId)
            .Where(x => parcoursAcademiqueId is null || x.ParcoursAcademiqueId == parcoursAcademiqueId)
            .Select(x => new AffectationMaquetteDto
            {
                Id = x.Id,
                ParcoursAcademiqueId = x.ParcoursAcademiqueId,
                MaquettePedagogiqueId = x.MaquettePedagogiqueId,
                ParcoursAcademiqueLibelle = parcoursItems.FirstOrDefault(p => p.Id == x.ParcoursAcademiqueId) is { } parcours
                    ? FormatCodeLibelle(parcours.Code, parcours.Libelle)
                    : string.Empty,
                MaquettePedagogiqueLibelle = maquetteItems.FirstOrDefault(m => m.Id == x.MaquettePedagogiqueId) is { } maquette
                    ? $"{maquette.Code} - {maquette.Libelle} ({maquette.Version})"
                    : string.Empty,
                EstPrincipale = x.EstPrincipale,
                EstActive = x.EstActive,
                DateDebut = x.DateDebut,
                DateFin = x.DateFin,
                Observation = x.Observation
            })
            .OrderBy(x => x.ParcoursAcademiqueLibelle)
            .ThenByDescending(x => x.EstPrincipale)
            .ToList();
    }

    public async Task SaveAffectationMaquetteAsync(
        AffectationMaquetteDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.ParcoursAcademiqueId <= 0 || dto.MaquettePedagogiqueId <= 0)
        {
            throw new InvalidOperationException("Le parcours et la maquette sont obligatoires.");
        }

        if (dto.DateDebut is not null && dto.DateFin is not null && dto.DateFin < dto.DateDebut)
        {
            throw new InvalidOperationException("La date de fin doit être supérieure ou égale à la date de début.");
        }

        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);
        if (parcoursItems.All(x => x.Id != dto.ParcoursAcademiqueId))
        {
            throw new InvalidOperationException("Le parcours sélectionné est introuvable.");
        }

        var maquetteItems = await maquettes.ListAsync(cancellationToken);
        if (maquetteItems.All(x => x.Id != dto.MaquettePedagogiqueId))
        {
            throw new InvalidOperationException("La maquette sélectionnée est introuvable.");
        }

        var existingItems = await affectationsMaquettes.ListAsync(cancellationToken);
        var duplicate = existingItems.Any(x =>
            x.Id != dto.Id
            && x.ParcoursAcademiqueId == dto.ParcoursAcademiqueId
            && x.MaquettePedagogiqueId == dto.MaquettePedagogiqueId);

        if (duplicate)
        {
            throw new InvalidOperationException("Cette maquette est déjà affectée à ce parcours.");
        }

        var activePrimary = existingItems.FirstOrDefault(x =>
            x.Id != dto.Id
            && x.ParcoursAcademiqueId == dto.ParcoursAcademiqueId
            && x.EstActive
            && x.EstPrincipale);

        if (dto.EstActive && dto.EstPrincipale && activePrimary is not null)
        {
            activePrimary.EstPrincipale = false;
        }

        var maquette = maquetteItems.First(x => x.Id == dto.MaquettePedagogiqueId);
        if (dto.EstActive && dto.EstPrincipale)
        {
            maquette.ParcoursAcademiqueId = dto.ParcoursAcademiqueId;
        }
        else if (maquette.ParcoursAcademiqueId == dto.ParcoursAcademiqueId)
        {
            maquette.ParcoursAcademiqueId = null;
        }

        if (dto.Id == 0)
        {
            await affectationsMaquettes.AddAsync(new AffectationMaquette
            {
                ParcoursAcademiqueId = dto.ParcoursAcademiqueId,
                MaquettePedagogiqueId = dto.MaquettePedagogiqueId,
                EstPrincipale = dto.EstPrincipale,
                EstActive = dto.EstActive,
                DateDebut = dto.DateDebut,
                DateFin = dto.DateFin,
                Observation = string.IsNullOrWhiteSpace(dto.Observation) ? null : dto.Observation.Trim()
            }, cancellationToken);
        }
        else
        {
            var entity = await affectationsMaquettes.GetByIdAsync(dto.Id, cancellationToken)
                ?? throw new InvalidOperationException("L'affectation sélectionnée est introuvable.");

            entity.ParcoursAcademiqueId = dto.ParcoursAcademiqueId;
            entity.MaquettePedagogiqueId = dto.MaquettePedagogiqueId;
            entity.EstPrincipale = dto.EstPrincipale;
            entity.EstActive = dto.EstActive;
            entity.DateDebut = dto.DateDebut;
            entity.DateFin = dto.DateFin;
            entity.Observation = string.IsNullOrWhiteSpace(dto.Observation) ? null : dto.Observation.Trim();
        }

        await affectationsMaquettes.SaveChangesAsync(cancellationToken);
        await maquettes.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAffectationMaquetteAsync(long id, CancellationToken cancellationToken = default)
    {
        await affectationsMaquettes.DeleteByIdAsync(id, cancellationToken);
        await affectationsMaquettes.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<SemestrePedagogiqueDto>> GetSemestresAsync(
        long? maquettePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var semestreItems = await semestres.ListAsync(cancellationToken);

        if (maquettePedagogiqueId is not null)
        {
            semestreItems = semestreItems.Where(x => x.MaquettePedagogiqueId == maquettePedagogiqueId).ToList();
        }

        var maquetteItems = await maquettes.ListAsync(cancellationToken);
        var uniteItems = await unitesEnseignement.ListAsync(cancellationToken);
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var maquetteEcItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var niveauItems = await niveauxEtude.ListAsync(cancellationToken);

        return semestreItems
            .Select(x => ToSemestreDto(x, maquetteItems, uniteItems, ecItems, maquetteEcItems, niveauItems))
            .OrderBy(x => x.MaquettePedagogiqueLibelle)
            .ThenBy(x => x.OrdreAffichage)
            .ThenBy(x => x.NumeroSemestre)
            .ToList();
    }

    public async Task<SemestrePedagogiqueDto?> GetSemestreAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await semestres.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        var maquetteItems = await maquettes.ListAsync(cancellationToken);
        var uniteItems = await unitesEnseignement.ListAsync(cancellationToken);
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var maquetteEcItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var niveauItems = await niveauxEtude.ListAsync(cancellationToken);

        return ToSemestreDto(entity, maquetteItems, uniteItems, ecItems, maquetteEcItems, niveauItems);
    }

    public async Task<SemestrePedagogiqueDto> CreateDefaultSemestreAsync(
        long? maquettePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var semestreItems = await semestres.ListAsync(cancellationToken);
        var filtered = maquettePedagogiqueId is null
            ? semestreItems
            : semestreItems.Where(x => x.MaquettePedagogiqueId == maquettePedagogiqueId).ToList();
        var nextNumero = Enumerable.Range(1, 8)
            .Select(x => (byte)x)
            .FirstOrDefault(numero => filtered.All(x => x.NumeroSemestre != numero));

        return new SemestrePedagogiqueDto
        {
            MaquettePedagogiqueId = maquettePedagogiqueId ?? 0,
            NumeroSemestre = nextNumero,
            Libelle = nextNumero == 0 ? string.Empty : $"Semestre {nextNumero}",
            OrdreAffichage = nextNumero
        };
    }

    public async Task SaveSemestreAsync(SemestrePedagogiqueDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.MaquettePedagogiqueId <= 0)
        {
            throw new InvalidOperationException("La maquette du semestre est obligatoire.");
        }

        var selectedUniteIds = dto.UniteEnseignementIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (selectedUniteIds.Count == 0)
        {
            throw new InvalidOperationException("Sélectionnez au moins une UE pour le semestre.");
        }

        if (dto.NumeroSemestre is < 1 or > 8)
        {
            throw new InvalidOperationException("Le numéro du semestre doit être compris entre 1 et 8.");
        }

        dto.Libelle = RequireText(dto.Libelle, "Le libellé du semestre est obligatoire.");
        dto.OrdreAffichage = dto.OrdreAffichage == 0 ? dto.NumeroSemestre : dto.OrdreAffichage;

        var niveauNumero = (byte)(((dto.NumeroSemestre - 1) / 2) + 1);
        var niveauEtude = (await niveauxEtude.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Numero == niveauNumero)
            ?? throw new InvalidOperationException(
                $"Le niveau d'étude {niveauNumero} est introuvable pour le semestre {dto.NumeroSemestre}.");

        await EnsureMaquetteExistsAsync(dto.MaquettePedagogiqueId, cancellationToken);

        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);
        var selectedUes = ueItems
            .Where(x => selectedUniteIds.Contains(x.Id))
            .ToList();

        if (selectedUes.Count != selectedUniteIds.Count)
        {
            throw new InvalidOperationException("Une des UE sélectionnées est introuvable.");
        }

        if (selectedUes.Any(x => x.MaquettePedagogiqueId != dto.MaquettePedagogiqueId))
        {
            throw new InvalidOperationException("Toutes les UE sélectionnées doivent appartenir à la maquette du semestre.");
        }

        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var selectedEcItems = selectedUes
            .SelectMany(ue => ecItems.Where(ec => ec.UniteEnseignementId == ue.Id))
            .ToList();

        if (selectedEcItems.Count == 0)
        {
            throw new InvalidOperationException("Chaque UE sélectionnée doit contenir au moins un EC.");
        }

        var existing = await semestres.ListAsync(cancellationToken);
        var duplicate = existing.Any(x =>
            x.Id != dto.Id
            && x.MaquettePedagogiqueId == dto.MaquettePedagogiqueId
            && x.NumeroSemestre == dto.NumeroSemestre);

        if (duplicate)
        {
            throw new InvalidOperationException("Ce numéro de semestre existe déjà dans cette maquette.");
        }

        SemestrePedagogique entity;

        if (dto.Id == 0)
        {
            entity = new SemestrePedagogique
            {
                MaquettePedagogiqueId = dto.MaquettePedagogiqueId,
                NiveauEtudeId = niveauEtude.Id,
                NumeroSemestre = dto.NumeroSemestre,
                Libelle = dto.Libelle,
                OrdreAffichage = dto.OrdreAffichage
            };

            await semestres.AddAsync(entity, cancellationToken);
        }
        else
        {
            var existingEntity = await semestres.GetByIdAsync(dto.Id, cancellationToken);
            if (existingEntity is null) return;
            entity = existingEntity;

            entity.MaquettePedagogiqueId = dto.MaquettePedagogiqueId;
            entity.NiveauEtudeId = niveauEtude.Id;
            entity.NumeroSemestre = dto.NumeroSemestre;
            entity.Libelle = dto.Libelle;
            entity.OrdreAffichage = dto.OrdreAffichage;
        }

        await semestres.SaveChangesAsync(cancellationToken);

        var currentMecItems = (await maquetteElementsConstitutifs.ListAsync(cancellationToken))
            .Where(x => x.SemestrePedagogiqueId == entity.Id)
            .ToList();
        var selectedEcIds = selectedEcItems.Select(x => x.Id).ToHashSet();
        var obsoleteMecItems = currentMecItems
            .Where(x => !selectedEcIds.Contains(x.ElementConstitutifId))
            .ToList();

        if (obsoleteMecItems.Count > 0)
        {
            var obsoleteMecIds = obsoleteMecItems.Select(x => x.Id).ToHashSet();
            var hasEvaluations = (await evaluationsAcademiques.ListAsync(cancellationToken))
                .Any(x => obsoleteMecIds.Contains(x.MaquetteElementConstitutifId));
            var hasResults = (await resultatsElementsConstitutifs.ListAsync(cancellationToken))
                .Any(x => obsoleteMecIds.Contains(x.MaquetteElementConstitutifId));

            if (hasEvaluations || hasResults)
            {
                throw new InvalidOperationException("Impossible de retirer une UE déjà utilisée dans des évaluations ou des résultats.");
            }

            foreach (var obsoleteMec in obsoleteMecItems)
            {
                maquetteElementsConstitutifs.Delete(obsoleteMec);
            }
        }

        var currentMecByEcId = currentMecItems.ToDictionary(x => x.ElementConstitutifId);
        var ordre = 1;

        foreach (var ec in selectedEcItems.OrderBy(x => selectedUniteIds.IndexOf(x.UniteEnseignementId)).ThenBy(x => x.Code).ThenBy(x => x.Libelle))
        {
            if (currentMecByEcId.TryGetValue(ec.Id, out var maquetteElement))
            {
                maquetteElement.Credits = ec.Credits;
                maquetteElement.VolumeHoraire = ec.VolumeHoraire;
                maquetteElement.OrdreAffichage = (short)ordre++;
                continue;
            }

            await maquetteElementsConstitutifs.AddAsync(new MaquetteElementConstitutif
            {
                SemestrePedagogiqueId = entity.Id,
                ElementConstitutifId = ec.Id,
                Credits = ec.Credits,
                Coefficient = 1m,
                VolumeHoraire = ec.VolumeHoraire,
                OrdreAffichage = (short)ordre++,
                EstObligatoire = true
            }, cancellationToken);
        }

        await maquetteElementsConstitutifs.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSemestreAsync(long id, CancellationToken cancellationToken = default)
    {
        await semestres.DeleteByIdAsync(id, cancellationToken);
        await semestres.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<UniteEnseignementDto>> GetUnitesEnseignementAsync(
        long? semestrePedagogiqueId = null,
        long? maquettePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var maquetteItems = await maquettes.ListAsync(cancellationToken);

        if (semestrePedagogiqueId is not null)
        {
            var maquetteEcItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
            var ecIds = maquetteEcItems
                .Where(x => x.SemestrePedagogiqueId == semestrePedagogiqueId)
                .Select(x => x.ElementConstitutifId)
                .ToHashSet();
            var ueIds = ecItems
                .Where(x => ecIds.Contains(x.Id))
                .Select(x => x.UniteEnseignementId)
                .ToHashSet();
            ueItems = ueItems.Where(x => ueIds.Contains(x.Id)).ToList();
        }

        if (maquettePedagogiqueId is not null)
        {
            ueItems = ueItems
                .Where(x => x.MaquettePedagogiqueId == maquettePedagogiqueId)
                .ToList();
        }

        return ueItems
            .Select(x => ToUeDto(x, ecItems, maquetteItems))
            .OrderBy(x => x.Code)
            .ToList();
    }

    public async Task<List<UniteEnseignementDto>> GetUnitesEnseignementByHierarchyAsync(
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        long? parcoursAcademiqueId = null,
        long? semestrePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var semestreItems = await GetFilteredSemestresAsync(
            anneeAcademiqueId, cycleFormationId, parcoursAcademiqueId, cancellationToken);

        if (semestrePedagogiqueId is not null)
        {
            semestreItems = semestreItems.Where(x => x.Id == semestrePedagogiqueId).ToList();
        }

        var semestreIds = semestreItems.Select(x => x.Id).ToHashSet();
        var maquetteEcItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var ecIds = maquetteEcItems
            .Where(x => semestreIds.Contains(x.SemestrePedagogiqueId))
            .Select(x => x.ElementConstitutifId)
            .ToHashSet();
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var ueIds = ecItems
            .Where(x => ecIds.Contains(x.Id))
            .Select(x => x.UniteEnseignementId)
            .ToHashSet();
        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);
        ueItems = ueItems.Where(x => ueIds.Contains(x.Id)).ToList();

        return ueItems
            .Select(x => ToUeDto(x, ecItems, null))
            .OrderBy(x => x.Code)
            .ToList();
    }

    public async Task<List<UniteEnseignementHierarchyDto>> GetUnitesEnseignementHierarchyAsync(
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        long? parcoursAcademiqueId = null,
        long? semestrePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var semestreItems = await GetFilteredSemestresAsync(
            anneeAcademiqueId, cycleFormationId, parcoursAcademiqueId, cancellationToken);

        if (semestrePedagogiqueId is not null)
        {
            semestreItems = semestreItems.Where(x => x.Id == semestrePedagogiqueId).ToList();
        }

        var semestreIds = semestreItems.Select(x => x.Id).ToHashSet();
        var maquetteEcItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);

        var maquetteEcsFiltered = maquetteEcItems
            .Where(x => semestreIds.Contains(x.SemestrePedagogiqueId))
            .ToList();

        var result = maquetteEcsFiltered
            .GroupBy(mec => mec.SemestrePedagogiqueId)
            .SelectMany(semGroup =>
            {
                var semestre = semestreItems.FirstOrDefault(x => x.Id == semGroup.Key);
                if (semestre is null) return Enumerable.Empty<UniteEnseignementHierarchyDto>();

                return semGroup
                    .GroupBy(mec =>
                    {
                        var ec = ecItems.FirstOrDefault(e => e.Id == mec.ElementConstitutifId);
                        return ec?.UniteEnseignementId ?? 0;
                    })
                    .Where(ueGroup => ueGroup.Key > 0)
                    .Select(ueGroup =>
                    {
                        var ue = ueItems.FirstOrDefault(x => x.Id == ueGroup.Key);
                        if (ue is null) return null!;

                        return new UniteEnseignementHierarchyDto
                        {
                            Id = ue.Id,
                            SemestrePedagogiqueId = semestre.Id,
                            SemestrePedagogiqueLibelle = $"{semestre.NumeroSemestre} - {semestre.Libelle}",
                            Code = ue.Code,
                            Libelle = ue.Libelle,
                            Credits = ueGroup
                                .Select(mec => ecItems.FirstOrDefault(ec => ec.Id == mec.ElementConstitutifId))
                                .Where(ec => ec is not null)
                                .Sum(ec => ec!.Credits),
                            VolumeHoraire = (short)ueGroup
                                .Select(mec => ecItems.FirstOrDefault(ec => ec.Id == mec.ElementConstitutifId))
                                .Where(ec => ec is not null)
                                .Sum(ec => ec!.VolumeHoraire),
                            OrdreAffichage = ueGroup.Min(m => m.OrdreAffichage),
                            EstObligatoire = ueGroup.All(m => m.EstObligatoire),
                            ElementsConstitutifs = ueGroup
                                .OrderBy(m => m.OrdreAffichage)
                                .Select(m =>
                                {
                                    var mecEc = ecItems.FirstOrDefault(e => e.Id == m.ElementConstitutifId);
                                    return new ElementConstitutifHierarchyDto
                                    {
                                        Id = m.ElementConstitutifId,
                                        MaquetteElementConstitutifId = m.Id,
                                        Code = mecEc?.Code,
                                        Libelle = mecEc?.Libelle ?? string.Empty,
                                        Type = mecEc?.Type ?? TypeElementConstitutif.CoursMagistraux,
                                        Credits = mecEc?.Credits ?? 0m,
                                        Coefficient = m.Coefficient,
                                        VolumeHoraire = mecEc?.VolumeHoraire ?? 0,
                                        OrdreAffichage = m.OrdreAffichage,
                                        EstObligatoire = m.EstObligatoire,
                                        Observation = m.Observation
                                    };
                                })
                                .ToList()
                        };
                    })
                    .Where(x => x is not null);
            })
            .ToList();

        // Une UE peut exister dans le catalogue avant son rattachement a une maquette.
        // Elle reste visible dans la vue generale avec ses EC et ses totaux propres.
        if (anneeAcademiqueId is null
            && cycleFormationId is null
            && parcoursAcademiqueId is null
            && semestrePedagogiqueId is null)
        {
            var ueIdsAlreadyDisplayed = result.Select(x => x.Id).ToHashSet();

            result.AddRange(ueItems
                .Where(ue => !ueIdsAlreadyDisplayed.Contains(ue.Id))
                .Select(ue =>
                {
                    var ueEcs = ecItems.Where(ec => ec.UniteEnseignementId == ue.Id).ToList();

                    return new UniteEnseignementHierarchyDto
                    {
                        Id = ue.Id,
                        SemestrePedagogiqueLibelle = "Non rattachee",
                        Code = ue.Code,
                        Libelle = ue.Libelle,
                        Credits = ueEcs.Sum(ec => ec.Credits),
                        VolumeHoraire = (short)ueEcs.Sum(ec => ec.VolumeHoraire),
                        OrdreAffichage = 0,
                        EstObligatoire = true,
                        ElementsConstitutifs = ueEcs
                            .OrderBy(ec => ec.Code)
                            .ThenBy(ec => ec.Libelle)
                            .Select(ec => new ElementConstitutifHierarchyDto
                            {
                                Id = ec.Id,
                                Code = ec.Code,
                                Libelle = ec.Libelle,
                                Type = ec.Type,
                                Credits = ec.Credits,
                                VolumeHoraire = ec.VolumeHoraire
                            })
                            .ToList()
                    };
                }));
        }

        return result
            .OrderBy(x => x.SemestrePedagogiqueLibelle)
            .ThenBy(x => x.Code)
            .ToList();
    }

    public async Task<UniteEnseignementDto?> GetUniteEnseignementAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await unitesEnseignement.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);

        var maquetteItems = await maquettes.ListAsync(cancellationToken);
        return ToUeDto(entity, ecItems, maquetteItems);
    }

    public Task<UniteEnseignementDto> CreateDefaultUniteEnseignementAsync(
        long? maquettePedagogiqueId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new UniteEnseignementDto
        {
            Code = string.Empty,
            MaquettePedagogiqueId = maquettePedagogiqueId
        });

    public async Task<string> GenerateUniteEnseignementCodeAsync(
        string? libelle,
        long excludeId = 0,
        CancellationToken cancellationToken = default)
    {
        var key = NormalizeLibelleKey(libelle);
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var prefix = GenerateCatalogueCodePrefix(key);
        var existingCodes = (await unitesEnseignement.ListAsync(cancellationToken))
            .Where(x => x.Id != excludeId)
            .Select(x => x.Code)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var number = 1; number <= 999; number++)
        {
            var candidate = $"{prefix}{number:000}";
            if (!existingCodes.Contains(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Impossible de générer un code unique pour cette UE.");
    }

    public async Task SaveUniteEnseignementAsync(UniteEnseignementDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.MaquettePedagogiqueId is null or <= 0)
        {
            throw new InvalidOperationException("La maquette pédagogique de l'UE est obligatoire.");
        }

        await EnsureMaquetteExistsAsync(dto.MaquettePedagogiqueId.Value, cancellationToken);
        dto.Libelle = RequireText(dto.Libelle, "Le libellé de l'UE est obligatoire.");
        dto.Code = NormalizeNullable(dto.Code)?.ToUpperInvariant() ?? string.Empty;

        if (dto.Id == 0 && string.IsNullOrWhiteSpace(dto.Code))
        {
            dto.Code = await GenerateUniteEnseignementCodeAsync(dto.Libelle, cancellationToken: cancellationToken);
        }

        dto.Code = NormalizeCode(dto.Code, "Le code de l'UE est obligatoire.");

        var existing = await unitesEnseignement.ListAsync(cancellationToken);
        var duplicate = existing.Any(x =>
            x.Id != dto.Id
            && string.Equals(x.Code, dto.Code, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            throw new InvalidOperationException("Une UE avec le même code existe déjà.");
        }

        if (dto.Id == 0)
        {
            await unitesEnseignement.AddAsync(new UniteEnseignement
            {
                MaquettePedagogiqueId = dto.MaquettePedagogiqueId,
                Code = dto.Code,
                Libelle = dto.Libelle
            }, cancellationToken);
        }
        else
        {
            var entity = await unitesEnseignement.GetByIdAsync(dto.Id, cancellationToken);
            if (entity is null) return;

            entity.Code = dto.Code;
            entity.Libelle = dto.Libelle;
            entity.MaquettePedagogiqueId = dto.MaquettePedagogiqueId;
        }

        await unitesEnseignement.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteUniteEnseignementAsync(long id, CancellationToken cancellationToken = default)
    {
        await unitesEnseignement.DeleteByIdAsync(id, cancellationToken);
        await unitesEnseignement.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ElementConstitutifDto>> GetElementsConstitutifsAsync(
        long? uniteEnseignementId = null,
        CancellationToken cancellationToken = default)
    {
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);

        if (uniteEnseignementId is not null)
        {
            ecItems = ecItems.Where(x => x.UniteEnseignementId == uniteEnseignementId).ToList();
        }

        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);

        return ecItems
            .Select(x => ToEcDto(x, ueItems))
            .OrderBy(x => x.UniteEnseignementLibelle)
            .ThenBy(x => x.Libelle)
            .ToList();
    }

    public async Task<ElementConstitutifDto?> GetElementConstitutifAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await elementsConstitutifs.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);

        return ToEcDto(entity, ueItems);
    }

    public Task<ElementConstitutifDto> CreateDefaultElementConstitutifAsync(
        long? uniteEnseignementId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ElementConstitutifDto
        {
            UniteEnseignementId = uniteEnseignementId ?? 0
        });

    public async Task<string> GenerateElementConstitutifCodeAsync(
        long uniteEnseignementId,
        string? libelle,
        long excludeId = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(libelle))
        {
            return string.Empty;
        }

        var uniteEnseignement = await unitesEnseignement.GetByIdAsync(uniteEnseignementId, cancellationToken);
        var prefix = uniteEnseignement is null ? string.Empty : NormalizeCodeToken(uniteEnseignement.Code);
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return string.Empty;
        }

        var existingCodes = (await elementsConstitutifs.ListAsync(cancellationToken))
            .Where(x => x.Id != excludeId)
            .Select(x => x.Code)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var number = 1; number <= 999; number++)
        {
            var candidate = $"{prefix}-EC{number:00}";
            if (!existingCodes.Contains(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Impossible de générer un code unique pour cet EC.");
    }

    public async Task SaveElementConstitutifAsync(ElementConstitutifDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.UniteEnseignementId <= 0)
        {
            throw new InvalidOperationException("L'UE de l'EC est obligatoire.");
        }

        dto.Libelle = RequireText(dto.Libelle, "Le libellé de l'EC est obligatoire.");
        dto.Code = NormalizeNullable(dto.Code)?.ToUpperInvariant();
        if (dto.Credits < 0)
        {
            throw new InvalidOperationException("Les crédits de l'EC doivent être supérieurs ou égaux à 0.");
        }

        if (dto.VolumeHoraire < 0)
        {
            throw new InvalidOperationException("Le volume horaire de l'EC doit être supérieur ou égal à 0.");
        }

        await EnsureUniteEnseignementExistsAsync(dto.UniteEnseignementId, cancellationToken);

        if (dto.Id == 0 && string.IsNullOrWhiteSpace(dto.Code))
        {
            dto.Code = await GenerateElementConstitutifCodeAsync(
                dto.UniteEnseignementId,
                dto.Libelle,
                cancellationToken: cancellationToken);
        }

        var existing = await elementsConstitutifs.ListAsync(cancellationToken);

        if (dto.Code is not null)
        {
            var duplicateCode = existing.Any(x =>
                x.Id != dto.Id
                && string.Equals(x.Code, dto.Code, StringComparison.OrdinalIgnoreCase));

            if (duplicateCode)
            {
                throw new InvalidOperationException("Un EC avec le même code existe déjà.");
            }
        }

        if (dto.Id == 0)
        {
            await elementsConstitutifs.AddAsync(new ElementConstitutif
            {
                UniteEnseignementId = dto.UniteEnseignementId,
                Code = dto.Code,
                Libelle = dto.Libelle,
                Type = dto.Type,
                Credits = dto.Credits,
                VolumeHoraire = dto.VolumeHoraire
            }, cancellationToken);
        }
        else
        {
            var entity = await elementsConstitutifs.GetByIdAsync(dto.Id, cancellationToken);
            if (entity is null) return;

            entity.UniteEnseignementId = dto.UniteEnseignementId;
            entity.Code = dto.Code;
            entity.Libelle = dto.Libelle;
            entity.Type = dto.Type;
            entity.Credits = dto.Credits;
            entity.VolumeHoraire = dto.VolumeHoraire;
        }

        await elementsConstitutifs.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteElementConstitutifAsync(long id, CancellationToken cancellationToken = default)
    {
        await elementsConstitutifs.DeleteByIdAsync(id, cancellationToken);
        await elementsConstitutifs.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<LookupDto>> GetAnneesAcademiquesLookupAsync(CancellationToken cancellationToken = default)
    {
        var items = await anneesAcademiques.ListAsync(cancellationToken);

        return items
            .OrderByDescending(x => x.AnneeDebut)
            .Select(x => new LookupDto { Id = x.Id, Libelle = x.Libelle })
            .ToList();
    }

    public async Task<List<LookupDto>> GetCyclesFormationLookupAsync(CancellationToken cancellationToken = default)
    {
        var items = await cyclesFormation.ListAsync(cancellationToken);

        return items
            .Where(x => x.EstActif)
            .OrderBy(x => x.OrdreAffichage)
            .ThenBy(x => x.Libelle)
            .Select(x => new LookupDto { Id = x.Id, Libelle = FormatCodeLibelle(x.Code, x.Libelle) })
            .ToList();
    }

    public async Task<List<LookupDto>> GetParcoursAcademiquesLookupAsync(
        long? cycleFormationId = null,
        CancellationToken cancellationToken = default)
    {
        var items = await parcoursAcademiques.ListAsync(cancellationToken);

        if (cycleFormationId is not null)
        {
            items = items
                .Where(x => x.CycleFormationId == cycleFormationId)
                .ToList();
        }

        return items
            .Where(x => x.EstActive)
            .OrderBy(x => x.Libelle)
            .Select(x => new LookupDto { Id = x.Id, Libelle = FormatCodeLibelle(x.Code, x.Libelle) })
            .ToList();
    }

    public async Task<List<LookupDto>> GetMaquettesLookupAsync(
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        long? niveauEtudeId = null,
        CancellationToken cancellationToken = default)
    {
        var items = await maquettes.ListAsync(cancellationToken);
        var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
        var ouvertureItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var affectationItems = await affectationsMaquettes.ListAsync(cancellationToken);
        var classeItems = await classesPedagogiques.ListAsync(cancellationToken);
        var semestreItems = await semestres.ListAsync(cancellationToken);
        var niveauItems = await niveauxEtude.ListAsync(cancellationToken);

        items = FilterMaquettes(
            items,
            affectationItems,
            anneeItems,
            ouvertureItems,
            classeItems,
            semestreItems,
            niveauItems,
            anneeAcademiqueId,
            cycleFormationId,
            niveauEtudeId,
            null);

        return items
            .OrderBy(x => x.Libelle)
            .ThenBy(x => x.Version)
            .Select(x =>
            {
                var niveaux = semestreItems
                    .Where(semestre => semestre.MaquettePedagogiqueId == x.Id)
                    .Select(semestre => GetNiveauForSemestre(semestre, niveauItems)?.Libelle)
                    .Where(libelle => !string.IsNullOrWhiteSpace(libelle))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(libelle => libelle, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var niveauSuffix = niveaux.Count == 0
                    ? string.Empty
                    : $" - {string.Join(", ", niveaux)}";

                return new LookupDto
                {
                    Id = x.Id,
                    Libelle = $"{x.Code} - {x.Libelle} ({x.Version}){niveauSuffix}"
                };
            })
            .ToList();
    }

    public async Task<List<LookupDto>> GetNiveauxEtudeLookupAsync(CancellationToken cancellationToken = default)
    {
        var items = await niveauxEtude.ListAsync(cancellationToken);

        return items
            .Where(x => x.EstActif)
            .OrderBy(x => x.Numero)
            .Select(x => new LookupDto { Id = x.Id, Libelle = x.Libelle })
            .ToList();
    }

    public async Task<List<LookupDto>> GetSemestresLookupAsync(
        long? maquettePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var items = await semestres.ListAsync(cancellationToken);

        if (maquettePedagogiqueId is not null)
        {
            items = items.Where(x => x.MaquettePedagogiqueId == maquettePedagogiqueId).ToList();
        }

        return items
            .OrderBy(x => x.OrdreAffichage)
            .ThenBy(x => x.NumeroSemestre)
            .Select(x => new LookupDto { Id = x.Id, Libelle = $"{x.NumeroSemestre} - {x.Libelle}" })
            .ToList();
    }

    public async Task<List<LookupDto>> GetSemestresHierarchyLookupAsync(
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        long? parcoursAcademiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var items = await GetFilteredSemestresAsync(
            anneeAcademiqueId, cycleFormationId, parcoursAcademiqueId, cancellationToken);

        return items
            .OrderBy(x => x.OrdreAffichage)
            .ThenBy(x => x.NumeroSemestre)
            .Select(x => new LookupDto { Id = x.Id, Libelle = $"{x.NumeroSemestre} - {x.Libelle}" })
            .ToList();
    }

    public async Task<List<LookupDto>> GetUnitesEnseignementLookupAsync(
        long? semestrePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var items = await unitesEnseignement.ListAsync(cancellationToken);

        if (semestrePedagogiqueId is not null)
        {
            var maquetteEcItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
            var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
            var ecIds = maquetteEcItems
                .Where(x => x.SemestrePedagogiqueId == semestrePedagogiqueId)
                .Select(x => x.ElementConstitutifId)
                .ToHashSet();
            var ueIds = ecItems
                .Where(x => ecIds.Contains(x.Id))
                .Select(x => x.UniteEnseignementId)
                .ToHashSet();
            items = items.Where(x => ueIds.Contains(x.Id)).ToList();
        }

        return items
            .OrderBy(x => x.Code)
            .Select(x => new LookupDto { Id = x.Id, Libelle = FormatCodeLibelle(x.Code, x.Libelle) })
            .ToList();
    }

    // --- Private helpers ---

    private async Task<ParcoursAcademique> GetParcoursAcademiqueAsync(long id, CancellationToken cancellationToken)
    {
        var parcours = (await parcoursAcademiques.ListAsync(cancellationToken)).FirstOrDefault(x => x.Id == id);
        return parcours ?? throw new InvalidOperationException("Le parcours sélectionné est introuvable.");
    }

    private async Task<List<SemestrePedagogique>> GetFilteredSemestresAsync(
        long? anneeAcademiqueId,
        long? cycleFormationId,
        long? parcoursAcademiqueId,
        CancellationToken cancellationToken)
    {
        var semestreItems = await semestres.ListAsync(cancellationToken);
        var maquetteItems = await maquettes.ListAsync(cancellationToken);
        var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
        var ouvertureItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var affectationItems = await affectationsMaquettes.ListAsync(cancellationToken);
        var classeItems = await classesPedagogiques.ListAsync(cancellationToken);
        var niveauItems = await niveauxEtude.ListAsync(cancellationToken);

        maquetteItems = FilterMaquettes(
            maquetteItems, affectationItems, anneeItems, ouvertureItems, classeItems,
            semestreItems, niveauItems, anneeAcademiqueId, cycleFormationId, null, null);

        if (parcoursAcademiqueId is not null)
        {
            maquetteItems = maquetteItems
                .Where(x => affectationItems.Any(a => a.MaquettePedagogiqueId == x.Id && a.ParcoursAcademiqueId == parcoursAcademiqueId))
                .ToList();
        }

        var maquetteIds = maquetteItems.Select(x => x.Id).ToHashSet();

        return semestreItems
            .Where(x => maquetteIds.Contains(x.MaquettePedagogiqueId))
            .ToList();
    }

    private async Task EnsureMaquetteExistsAsync(long id, CancellationToken cancellationToken)
    {
        var exists = (await maquettes.ListAsync(cancellationToken)).Any(x => x.Id == id);
        if (!exists) throw new InvalidOperationException("La maquette sélectionnée est introuvable.");
    }

    private async Task EnsureUniteEnseignementExistsAsync(long id, CancellationToken cancellationToken)
    {
        var exists = (await unitesEnseignement.ListAsync(cancellationToken)).Any(x => x.Id == id);
        if (!exists) throw new InvalidOperationException("L'UE sélectionnée est introuvable.");
    }

    private static List<MaquettePedagogique> FilterMaquettes(
        List<MaquettePedagogique> maquetteItems,
        IReadOnlyCollection<AffectationMaquette> affectationItems,
        IReadOnlyCollection<AnneeAcademique> anneeItems,
        IReadOnlyCollection<ParcoursAcademique> ouvertureItems,
        IReadOnlyCollection<ClassePedagogique> classeItems,
        IReadOnlyCollection<SemestrePedagogique> semestreItems,
        IReadOnlyCollection<NiveauEtude> niveauItems,
        long? anneeAcademiqueId,
        long? cycleFormationId,
        long? niveauEtudeId,
        long? maquettePedagogiqueId)
    {
        IEnumerable<MaquettePedagogique> query = maquetteItems;

        if (anneeAcademiqueId is not null)
        {
            var annee = anneeItems.FirstOrDefault(x => x.Id == anneeAcademiqueId);
            var parcoursIdsPourAnnee = ouvertureItems
                .Where(x => x.AnneeAcademiqueId == anneeAcademiqueId)
                .Select(x => x.Id)
                .ToHashSet();

            var maquetteIdsPourAnnee = maquetteItems
                .Where(x => affectationItems.Any(a => a.MaquettePedagogiqueId == x.Id && parcoursIdsPourAnnee.Contains(a.ParcoursAcademiqueId)))
                .Select(x => x.Id)
                .ToHashSet();

            query = query.Where(x =>
                maquetteIdsPourAnnee.Contains(x.Id)
                || (annee is not null && IsMaquetteValidForAcademicYear(x, annee)));
        }

        if (cycleFormationId is not null)
        {
            var ouvertureIds = ouvertureItems
                .Where(x => x.CycleFormationId == cycleFormationId)
                .Select(x => x.Id)
                .ToHashSet();

            query = query.Where(x => affectationItems.Any(a => a.MaquettePedagogiqueId == x.Id && ouvertureIds.Contains(a.ParcoursAcademiqueId)));
        }

        if (niveauEtudeId is not null)
        {
            var niveau = niveauItems.FirstOrDefault(x => x.Id == niveauEtudeId.Value);

            if (niveau is null)
            {
                return [];
            }

            var maquetteIdsPourNiveau = semestreItems
                .Where(semestre => IsSemestreForNiveau(semestre, niveau))
                .Select(semestre => semestre.MaquettePedagogiqueId)
                .ToHashSet();

            query = query.Where(x => maquetteIdsPourNiveau.Contains(x.Id));
        }

        if (maquettePedagogiqueId is not null)
        {
            query = query.Where(x => x.Id == maquettePedagogiqueId);
        }

        return query.ToList();
    }

    private static bool IsSemestreForNiveau(
        SemestrePedagogique semestre,
        long? niveauEtudeId,
        IReadOnlyCollection<NiveauEtude> niveauItems)
    {
        if (niveauEtudeId is null)
        {
            return true;
        }

        var niveau = niveauItems.FirstOrDefault(x => x.Id == niveauEtudeId.Value);
        return niveau is not null && IsSemestreForNiveau(semestre, niveau);
    }

    private static bool IsSemestreForNiveau(
        SemestrePedagogique semestre,
        NiveauEtude niveau)
    {
        if (semestre.NumeroSemestre is < 1 or > 8)
        {
            return false;
        }

        var niveauCalcule = (byte)(((semestre.NumeroSemestre - 1) / 2) + 1);
        return niveauCalcule == niveau.Numero;
    }

    private static NiveauEtude? GetNiveauForSemestre(
        SemestrePedagogique semestre,
        IReadOnlyCollection<NiveauEtude> niveauItems)
    {
        if (semestre.NumeroSemestre is >= 1 and <= 8)
        {
            var niveauCalcule = (byte)(((semestre.NumeroSemestre - 1) / 2) + 1);
            return niveauItems.FirstOrDefault(x => x.Numero == niveauCalcule);
        }

        return semestre.NiveauEtudeId is null
            ? null
            : niveauItems.FirstOrDefault(x => x.Id == semestre.NiveauEtudeId.Value);
    }

    private static bool IsMaquetteValidForAcademicYear(MaquettePedagogique maquette, AnneeAcademique annee)
    {
        var academicStart = new DateOnly(annee.AnneeDebut, 9, 1);
        var academicEnd = new DateOnly(annee.AnneeFin, 8, 31);

        var maquetteStart = maquette.DateDebutValidite ?? DateOnly.MinValue;
        var maquetteEnd = maquette.DateFinValidite ?? DateOnly.MaxValue;

        return maquetteStart <= academicEnd && maquetteEnd >= academicStart;
    }

    private static MaquettePedagogiqueDto ToDto(
        MaquettePedagogique maquette,
        IReadOnlyCollection<AffectationMaquette> affectationItems,
        IReadOnlyCollection<ParcoursAcademique> ouvertures,
        IReadOnlyCollection<SemestrePedagogique> semestreItems)
    {
        var parcoursAffectes = ouvertures
            .Where(x => affectationItems.Any(a => a.MaquettePedagogiqueId == maquette.Id && a.ParcoursAcademiqueId == x.Id))
            .ToList();

        return new MaquettePedagogiqueDto
        {
            Id = maquette.Id,
            ParcoursAcademiquesLibelle = string.Join(", ", parcoursAffectes.Select(x => FormatCodeLibelle(x.Code, x.Libelle))),
            NombreAffectations = parcoursAffectes.Count,
            Code = maquette.Code,
            Libelle = maquette.Libelle,
            Version = maquette.Version,
            Statut = maquette.Statut,
            DateDebutValidite = maquette.DateDebutValidite,
            DateFinValidite = maquette.DateFinValidite,
            SourceDocument = maquette.SourceDocument,
            Observation = maquette.Observation,
            NombreSemestres = semestreItems.Count(x => x.MaquettePedagogiqueId == maquette.Id)
        };
    }

    private static SemestrePedagogiqueDto ToSemestreDto(
        SemestrePedagogique semestre,
        IReadOnlyCollection<MaquettePedagogique> maquetteItems,
        IReadOnlyCollection<UniteEnseignement> uniteItems,
        IReadOnlyCollection<ElementConstitutif> ecItems,
        IReadOnlyCollection<MaquetteElementConstitutif> maquetteEcItems,
        IReadOnlyCollection<NiveauEtude> niveauItems)
    {
        var maquette = maquetteItems.FirstOrDefault(x => x.Id == semestre.MaquettePedagogiqueId);
        var semestreMecItems = maquetteEcItems
            .Where(x => x.SemestrePedagogiqueId == semestre.Id)
            .ToList();
        var semestreEcItems = semestreMecItems
            .Select(mec => ecItems.FirstOrDefault(ec => ec.Id == mec.ElementConstitutifId))
            .Where(ec => ec is not null)
            .Select(ec => ec!)
            .ToList();
        var semestreUes = uniteItems
            .Where(ue => semestreEcItems.Any(ec => ec.UniteEnseignementId == ue.Id))
            .OrderBy(ue => ue.Code)
            .ToList();

        return new SemestrePedagogiqueDto
        {
            Id = semestre.Id,
            MaquettePedagogiqueId = semestre.MaquettePedagogiqueId,
            NiveauEtudeId = semestre.NiveauEtudeId,
            NiveauEtudeLibelle = semestre.NiveauEtudeId is null
                ? string.Empty
                : niveauItems.FirstOrDefault(x => x.Id == semestre.NiveauEtudeId.Value)?.Libelle ?? string.Empty,
            MaquettePedagogiqueLibelle = maquette is null ? string.Empty : $"{maquette.Code} - {maquette.Libelle} ({maquette.Version})",
            UniteEnseignementIds = semestreUes.Select(x => x.Id).ToList(),
            UniteEnseignementLibelle = string.Join(", ", semestreUes.Select(x => FormatCodeLibelle(x.Code, x.Libelle))),
            NumeroSemestre = semestre.NumeroSemestre,
            Libelle = semestre.Libelle,
            CreditsUE = semestreEcItems.Sum(x => x.Credits),
            VolumeHoraireUE = (short)semestreEcItems.Sum(x => x.VolumeHoraire),
            OrdreAffichage = semestre.OrdreAffichage,
            NombreUnitesEnseignement = semestreUes.Count
        };
    }

    private static UniteEnseignementDto ToUeDto(
        UniteEnseignement ue,
        IReadOnlyCollection<ElementConstitutif> ecItems,
        IReadOnlyCollection<MaquettePedagogique>? maquetteItems)
    {
        var ueEcItems = ecItems
            .Where(x => x.UniteEnseignementId == ue.Id)
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Libelle)
            .ToList();

        return new UniteEnseignementDto
        {
            Id = ue.Id,
            MaquettePedagogiqueId = ue.MaquettePedagogiqueId,
            MaquettePedagogiqueLibelle = maquetteItems is null || ue.MaquettePedagogiqueId is null
                ? string.Empty
                : maquetteItems.FirstOrDefault(x => x.Id == ue.MaquettePedagogiqueId)?.Libelle ?? string.Empty,
            Code = ue.Code,
            Libelle = ue.Libelle,
            Credits = ueEcItems.Sum(x => x.Credits),
            VolumeHoraire = (short)ueEcItems.Sum(x => x.VolumeHoraire),
            NombreElementsConstitutifs = ueEcItems.Count,
            ElementsConstitutifs = ueEcItems.Select(ec => new ElementConstitutifDto
            {
                Id = ec.Id,
                UniteEnseignementId = ec.UniteEnseignementId,
                Code = ec.Code,
                Libelle = ec.Libelle,
                Type = ec.Type,
                Credits = ec.Credits,
                VolumeHoraire = ec.VolumeHoraire
            }).ToList()
        };
    }

    private static ElementConstitutifDto ToEcDto(
        ElementConstitutif ec,
        IReadOnlyCollection<UniteEnseignement> ueItems)
    {
        var ue = ueItems.FirstOrDefault(x => x.Id == ec.UniteEnseignementId);

        return new ElementConstitutifDto
        {
            Id = ec.Id,
            UniteEnseignementId = ec.UniteEnseignementId,
            UniteEnseignementLibelle = ue is null ? string.Empty : FormatCodeLibelle(ue.Code, ue.Libelle),
            Code = ec.Code,
            Libelle = ec.Libelle,
            Type = ec.Type,
            Credits = ec.Credits,
            VolumeHoraire = ec.VolumeHoraire
        };
    }

    private static string FormatCodeLibelle(string code, string libelle)
        => string.IsNullOrWhiteSpace(code) ? libelle : $"{code} - {libelle}";

    private static string GenerateCatalogueCodePrefix(string normalizedLibelle)
    {
        var ignoredWords = new HashSet<string> { "DE", "DES", "DU", "D", "ET", "EN", "A", "AU", "AUX", "LA", "LE", "LES" };
        var words = normalizedLibelle.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => !ignoredWords.Contains(word))
            .ToList();

        var letters = string.Concat(words).Where(char.IsLetterOrDigit).ToArray();
        return new string(letters[..Math.Min(3, letters.Length)]).ToUpperInvariant();
    }

    private static string NormalizeCodeToken(string value)
        => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static string NormalizeLibelleKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().Normalize(System.Text.NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder(normalized.Length);
        var previousWasSpace = true;

        foreach (var character in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
                previousWasSpace = false;
            }
            else if (!previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        return builder.ToString().Trim();
    }

    private static string NormalizeCode(string? value, string errorMessage)
        => RequireText(value, errorMessage).ToUpperInvariant();

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string RequireText(string? value, string errorMessage)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return normalized;
    }
}
