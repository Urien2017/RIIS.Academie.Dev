using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Common.Dtos;
using RIIS.Academic.Application.Evaluations.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Evaluations.Services;

public class EvaluationsService(
    IRepository<EvaluationAcademique> evaluations,
    IRepository<AnneeAcademique> anneesAcademiques,
    IRepository<ElementConstitutif> elementsConstitutifs,
    IRepository<UniteEnseignement> unitesEnseignement,
    IRepository<SemestrePedagogique> semestresPedagogiques,
    IRepository<MaquettePedagogique> maquettesPedagogiques,
    IRepository<MaquetteElementConstitutif> maquetteElementsConstitutifs,
    IRepository<AffectationMaquette> affectationsMaquettes,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<CycleFormation> cyclesFormation,
    IRepository<NoteEvaluation> notesEvaluations) : IEvaluationsService
{
    public async Task<List<EvaluationAcademiqueDto>> GetEvaluationsAsync(
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        long? semestrePedagogiqueId = null,
        long? uniteEnseignementId = null,
        long? elementConstitutifId = null,
        TypeEvaluation? type = null,
        long? parcoursAcademiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var evaluationItems = await evaluations.ListAsync(cancellationToken);
        var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);
        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);
        var maquetteItems = await maquettesPedagogiques.ListAsync(cancellationToken);
        var maquetteEcItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var affectationItems = await affectationsMaquettes.ListAsync(cancellationToken);
        var ouvertureItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var cycleItems = await cyclesFormation.ListAsync(cancellationToken);
        var noteItems = await notesEvaluations.ListAsync(cancellationToken);

        if (parcoursAcademiqueId is not null)
        {
            var maquetteIds = ResolveMaquetteIds(
                parcoursAcademiqueId.Value,
                maquetteItems,
                affectationItems);
            maquetteItems = maquetteItems.Where(x => maquetteIds.Contains(x.Id)).ToList();
            evaluationItems = evaluationItems
                .Where(x => maquetteEcItems.Any(mec =>
                    mec.Id == x.MaquetteElementConstitutifId
                    && maquetteIds.Contains(semestreItems.FirstOrDefault(s => s.Id == mec.SemestrePedagogiqueId)?.MaquettePedagogiqueId ?? 0)))
                .ToList();
        }

        if (anneeAcademiqueId is not null)
        {
            evaluationItems = evaluationItems.Where(x => x.AnneeAcademiqueId == anneeAcademiqueId).ToList();
        }

        if (cycleFormationId is not null)
        {
            var ouvertureIds = ouvertureItems
                .Where(x => x.CycleFormationId == cycleFormationId)
                .Select(x => x.Id)
                .ToHashSet();

            maquetteItems = maquetteItems
                .Where(x => x.ParcoursAcademiqueId is not null && ouvertureIds.Contains(x.ParcoursAcademiqueId.Value))
                .ToList();
        }

        if (anneeAcademiqueId is not null || cycleFormationId is not null)
        {
            var maquetteIds = maquetteItems.Select(x => x.Id).ToHashSet();

            var semestreIds = semestreItems
                .Where(x => maquetteIds.Contains(x.MaquettePedagogiqueId))
                .Select(x => x.Id)
                .ToHashSet();

            var maquetteEcIds = maquetteEcItems
                .Where(x => semestreIds.Contains(x.SemestrePedagogiqueId))
                .Select(x => x.Id)
                .ToHashSet();

            evaluationItems = evaluationItems.Where(x => maquetteEcIds.Contains(x.MaquetteElementConstitutifId)).ToList();
        }

        if (semestrePedagogiqueId is not null)
        {
            var maquetteEcIds = maquetteEcItems
                .Where(x => x.SemestrePedagogiqueId == semestrePedagogiqueId)
                .Select(x => x.Id)
                .ToHashSet();

            evaluationItems = evaluationItems.Where(x => maquetteEcIds.Contains(x.MaquetteElementConstitutifId)).ToList();
        }

        if (uniteEnseignementId is not null)
        {
            var ecIds = ecItems
                .Where(x => x.UniteEnseignementId == uniteEnseignementId)
                .Select(x => x.Id)
                .ToHashSet();

            var maquetteEcIds = maquetteEcItems
                .Where(x => ecIds.Contains(x.ElementConstitutifId))
                .Select(x => x.Id)
                .ToHashSet();

            evaluationItems = evaluationItems.Where(x => maquetteEcIds.Contains(x.MaquetteElementConstitutifId)).ToList();
        }

        if (elementConstitutifId is not null)
        {
            var maquetteEcIds = maquetteEcItems
                .Where(x => x.ElementConstitutifId == elementConstitutifId)
                .Select(x => x.Id)
                .ToHashSet();

            evaluationItems = evaluationItems.Where(x => maquetteEcIds.Contains(x.MaquetteElementConstitutifId)).ToList();
        }

        if (type is not null)
        {
            evaluationItems = evaluationItems.Where(x => x.Type == type).ToList();
        }

        return evaluationItems
            .Select(x => ToDto(x, anneeItems, ecItems, ueItems, semestreItems, maquetteItems, maquetteEcItems, ouvertureItems, cycleItems, evaluationItems, affectationItems, noteItems))
            .OrderBy(x => x.AnneeAcademiqueLibelle)
            .ThenBy(x => x.CycleFormationLibelle)
            .ThenBy(x => x.SemestrePedagogiqueLibelle)
            .ThenBy(x => x.UniteEnseignementLibelle)
            .ThenBy(x => x.ElementConstitutifLibelle)
            .ThenBy(x => x.Type)
            .ThenBy(x => x.Numero)
            .ToList();
    }

    public async Task<EvaluationAcademiqueDto?> GetEvaluationAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await evaluations.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);
        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);
        var maquetteItems = await maquettesPedagogiques.ListAsync(cancellationToken);
        var maquetteEcItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var ouvertureItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var cycleItems = await cyclesFormation.ListAsync(cancellationToken);
        var evaluationItems = await evaluations.ListAsync(cancellationToken);

        var affectationItems = await affectationsMaquettes.ListAsync(cancellationToken);
        var noteItems = await notesEvaluations.ListAsync(cancellationToken);

        return ToDto(entity, anneeItems, ecItems, ueItems, semestreItems, maquetteItems, maquetteEcItems, ouvertureItems, cycleItems, evaluationItems, affectationItems, noteItems);
    }

    public async Task<EvaluationAcademiqueDto> CreateDefaultEvaluationAsync(
        long? parcoursAcademiqueId = null,
        long? maquetteElementConstitutifId = null,
        TypeEvaluation type = TypeEvaluation.ControleContinu,
        CancellationToken cancellationToken = default)
    {
        var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
        var evaluationItems = await evaluations.ListAsync(cancellationToken);
        var parcours = parcoursAcademiqueId is null
            ? null
            : (await parcoursAcademiques.ListAsync(cancellationToken))
                .FirstOrDefault(x => x.Id == parcoursAcademiqueId);
        var anneeAcademiqueId = parcours?.AnneeAcademiqueId;
        var filtered = evaluationItems
            .Where(x => x.AnneeAcademiqueId == anneeAcademiqueId
                && x.MaquetteElementConstitutifId == maquetteElementConstitutifId
                && x.Type == type)
            .ToList();
        var nextNumero = filtered.Count == 0 ? (byte)1 : (byte)(filtered.Max(x => x.Numero) + 1);
        var codePrefix = GetTypeCode(type);

        return new EvaluationAcademiqueDto
        {
            AnneeAcademiqueId = anneeAcademiqueId ?? anneeItems.FirstOrDefault(x => x.EstActive)?.Id ?? anneeItems.FirstOrDefault()?.Id ?? 0,
            ParcoursAcademiqueId = parcours?.Id ?? 0,
            ParcoursAcademiqueLibelle = parcours?.Libelle ?? string.Empty,
            MaquetteElementConstitutifId = maquetteElementConstitutifId ?? 0,
            Type = type,
            TypeLibelle = GetTypeLibelle(type),
            Numero = nextNumero,
            Code = $"{codePrefix}{nextNumero}",
            Libelle = $"{GetTypeLibelle(type)} {nextNumero}",
            Bareme = 20m,
            PonderationPourcentage = GetDefaultPonderation(type)
        };
    }

    public async Task SaveEvaluationAsync(EvaluationAcademiqueDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.ParcoursAcademiqueId <= 0)
        {
            throw new InvalidOperationException("Le parcours académique est obligatoire.");
        }

        if (dto.MaquetteElementConstitutifId <= 0)
        {
            throw new InvalidOperationException("L'EC est obligatoire.");
        }

        var parcours = (await parcoursAcademiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == dto.ParcoursAcademiqueId)
            ?? throw new InvalidOperationException("Le parcours académique sélectionné est introuvable.");

        var maquetteElement = await ResolveMaquetteElementForParcoursAsync(
            dto.ParcoursAcademiqueId,
            dto.MaquetteElementConstitutifId,
            cancellationToken);
        var ec = (await elementsConstitutifs.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == maquetteElement.ElementConstitutifId)
            ?? throw new InvalidOperationException("L'EC sélectionné est introuvable.");

        dto.AnneeAcademiqueId = parcours.AnneeAcademiqueId;

        if (dto.Bareme <= 0)
        {
            throw new InvalidOperationException("Le barème doit être supérieur à 0.");
        }

        if (dto.PonderationPourcentage < 0 || dto.PonderationPourcentage > 100)
        {
            throw new InvalidOperationException("La pondération doit être comprise entre 0 et 100.");
        }

        if (dto.Type == TypeEvaluation.SessionRattrapage && dto.EvaluationRemplaceeId is null)
        {
            throw new InvalidOperationException("La session normale remplacée est obligatoire pour une session de rattrapage.");
        }

        if (dto.Type == TypeEvaluation.SessionRattrapage && dto.EvaluationRemplaceeId is not null)
        {
            var evaluationRemplacee = await evaluations.GetByIdAsync(dto.EvaluationRemplaceeId.Value, cancellationToken);
            if (evaluationRemplacee is null || evaluationRemplacee.Type != TypeEvaluation.SessionNormale)
            {
                throw new InvalidOperationException("Une session de rattrapage ne peut remplacer qu'une session normale.");
            }

            if (evaluationRemplacee.AnneeAcademiqueId != dto.AnneeAcademiqueId || evaluationRemplacee.MaquetteElementConstitutifId != dto.MaquetteElementConstitutifId)
            {
                throw new InvalidOperationException("La session normale remplacée doit appartenir à la même année académique et au même EC.");
            }
        }

        if (dto.Type != TypeEvaluation.SessionRattrapage)
        {
            dto.EvaluationRemplaceeId = null;
        }

        var evaluationItems = await evaluations.ListAsync(cancellationToken);

        if (dto.Id == 0)
        {
            var sameType = evaluationItems
                .Where(x => x.AnneeAcademiqueId == dto.AnneeAcademiqueId
                    && x.MaquetteElementConstitutifId == dto.MaquetteElementConstitutifId
                    && x.Type == dto.Type)
                .ToList();
            dto.Numero = dto.Type is TypeEvaluation.SessionNormale or TypeEvaluation.SessionRattrapage
                ? (byte)1
                : sameType.Count == 0 ? (byte)1 : (byte)(sameType.Max(x => x.Numero) + 1);
        }

        dto.Code = NormalizeCode($"{GetTypeCode(dto.Type)}-{ec.Code ?? $"EC{ec.Id}"}{(dto.Numero > 1 ? $"-{dto.Numero}" : string.Empty)}");
        dto.Libelle = RequireText($"{GetTypeLibelle(dto.Type)} - {ec.Libelle}", "Le libellé est obligatoire.");

        if (dto.Type is TypeEvaluation.SessionNormale or TypeEvaluation.SessionRattrapage)
        {
            var duplicateSession = evaluationItems.Any(x =>
                x.Id != dto.Id
                && x.AnneeAcademiqueId == dto.AnneeAcademiqueId
                && x.MaquetteElementConstitutifId == dto.MaquetteElementConstitutifId
                && x.Type == dto.Type);

            if (duplicateSession)
            {
                throw new InvalidOperationException($"Une seule évaluation de type {GetTypeCode(dto.Type)} est autorisée pour cet EC et cette année académique.");
            }
        }

        var duplicate = evaluationItems.Any(x =>
            x.Id != dto.Id
            && x.AnneeAcademiqueId == dto.AnneeAcademiqueId
            && x.MaquetteElementConstitutifId == dto.MaquetteElementConstitutifId
            && x.Type == dto.Type
            && x.Numero == dto.Numero);

        if (duplicate)
        {
            throw new InvalidOperationException("Une évaluation du même type avec le même numéro existe déjà pour cet EC et cette année.");
        }

        var dateEvaluation = dto.DateEvaluation is null ? (DateOnly?)null : DateOnly.FromDateTime(dto.DateEvaluation.Value);

        if (dto.Id == 0)
        {
            await evaluations.AddAsync(new EvaluationAcademique
            {
                AnneeAcademiqueId = dto.AnneeAcademiqueId,
                MaquetteElementConstitutifId = dto.MaquetteElementConstitutifId,
                Type = dto.Type,
                Numero = dto.Numero,
                Code = NormalizeCode(dto.Code),
                Libelle = RequireText(dto.Libelle, "Le libellé est obligatoire."),
                Bareme = dto.Bareme,
                PonderationPourcentage = dto.PonderationPourcentage,
                DateEvaluation = dateEvaluation,
                EvaluationRemplaceeId = dto.EvaluationRemplaceeId,
                Observation = NormalizeNullable(dto.Observation)
            }, cancellationToken);
        }
        else
        {
            var entity = await evaluations.GetByIdAsync(dto.Id, cancellationToken)
                ?? throw new InvalidOperationException("L'évaluation est introuvable.");

            entity.AnneeAcademiqueId = dto.AnneeAcademiqueId;
            entity.MaquetteElementConstitutifId = dto.MaquetteElementConstitutifId;
            entity.Type = dto.Type;
            entity.Numero = dto.Numero;
            entity.Code = NormalizeCode(dto.Code);
            entity.Libelle = RequireText(dto.Libelle, "Le libellé est obligatoire.");
            entity.Bareme = dto.Bareme;
            entity.PonderationPourcentage = dto.PonderationPourcentage;
            entity.DateEvaluation = dateEvaluation;
            entity.EvaluationRemplaceeId = dto.EvaluationRemplaceeId;
            entity.Observation = NormalizeNullable(dto.Observation);
        }

        await evaluations.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteEvaluationAsync(long id, CancellationToken cancellationToken = default)
    {
        await evaluations.DeleteByIdAsync(id, cancellationToken);
        await evaluations.SaveChangesAsync(cancellationToken);
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
            .OrderBy(x => x.OrdreAffichage)
            .ThenBy(x => x.Libelle)
            .Select(x => new LookupDto { Id = x.Id, Libelle = FormatCodeLibelle(x.Code, x.Libelle) })
            .ToList();
    }

    public async Task<List<LookupDto>> GetParcoursAcademiquesLookupAsync(CancellationToken cancellationToken = default)
    {
        var items = await parcoursAcademiques.ListAsync(cancellationToken);

        return items
            .Where(x => x.EstActive)
            .OrderByDescending(x => x.AnneeAcademiqueId)
            .ThenBy(x => x.Libelle)
            .Select(x => new LookupDto
            {
                Id = x.Id,
                Libelle = string.IsNullOrWhiteSpace(x.Code) ? x.Libelle : $"{x.Code} - {x.Libelle}"
            })
            .ToList();
    }

    public async Task<List<LookupDto>> GetUnitesEnseignementForParcoursLookupAsync(
        long parcoursAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        var maquetteIds = ResolveMaquetteIds(
            parcoursAcademiqueId,
            await maquettesPedagogiques.ListAsync(cancellationToken),
            await affectationsMaquettes.ListAsync(cancellationToken));
        var semestreIds = (await semestresPedagogiques.ListAsync(cancellationToken))
            .Where(x => maquetteIds.Contains(x.MaquettePedagogiqueId))
            .Select(x => x.Id)
            .ToHashSet();
        var ecIds = (await maquetteElementsConstitutifs.ListAsync(cancellationToken))
            .Where(x => semestreIds.Contains(x.SemestrePedagogiqueId))
            .Select(x => x.ElementConstitutifId)
            .ToHashSet();
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);

        return (await unitesEnseignement.ListAsync(cancellationToken))
            .Where(x => ecItems.Any(ec => ec.UniteEnseignementId == x.Id && ecIds.Contains(ec.Id)))
            .OrderBy(x => x.Code)
            .Select(x => new LookupDto { Id = x.Id, Libelle = FormatCodeLibelle(x.Code, x.Libelle) })
            .ToList();
    }

    public async Task<List<LookupDto>> GetElementsConstitutifsForParcoursLookupAsync(
        long parcoursAcademiqueId,
        long? uniteEnseignementId = null,
        CancellationToken cancellationToken = default)
    {
        var maquetteIds = ResolveMaquetteIds(
            parcoursAcademiqueId,
            await maquettesPedagogiques.ListAsync(cancellationToken),
            await affectationsMaquettes.ListAsync(cancellationToken));
        var semestreIds = (await semestresPedagogiques.ListAsync(cancellationToken))
            .Where(x => maquetteIds.Contains(x.MaquettePedagogiqueId))
            .Select(x => x.Id)
            .ToHashSet();
        var mecItems = (await maquetteElementsConstitutifs.ListAsync(cancellationToken))
            .Where(x => semestreIds.Contains(x.SemestrePedagogiqueId))
            .OrderBy(x => x.OrdreAffichage)
            .ThenBy(x => x.Id)
            .ToList();
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);

        return mecItems
            .Select(mec => new { Mec = mec, Ec = ecItems.FirstOrDefault(ec => ec.Id == mec.ElementConstitutifId) })
            .Where(x => x.Ec is not null && (uniteEnseignementId is null || x.Ec!.UniteEnseignementId == uniteEnseignementId))
            .Select(x => new LookupDto
            {
                Id = x.Mec.Id,
                Libelle = FormatCodeLibelle(x.Ec!.Code, x.Ec.Libelle)
            })
            .ToList();
    }

    public async Task<List<LookupDto>> GetSemestresLookupAsync(
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        CancellationToken cancellationToken = default)
    {
        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);
        var maquetteItems = await maquettesPedagogiques.ListAsync(cancellationToken);
        var ouvertureItems = await parcoursAcademiques.ListAsync(cancellationToken);

        if (anneeAcademiqueId is not null)
        {
            var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
            var annee = anneeItems.FirstOrDefault(x => x.Id == anneeAcademiqueId);

            if (annee is not null)
            {
                maquetteItems = maquetteItems
                    .Where(x => x.Version == annee.Libelle)
                    .ToList();
            }
        }

        if (cycleFormationId is not null)
        {
            var ouvertureIds = ouvertureItems
                .Where(x => x.CycleFormationId == cycleFormationId)
                .Select(x => x.Id)
                .ToHashSet();

            maquetteItems = maquetteItems
                .Where(x => x.ParcoursAcademiqueId is not null && ouvertureIds.Contains(x.ParcoursAcademiqueId.Value))
                .ToList();
        }

        var maquetteIds = maquetteItems.Select(x => x.Id).ToHashSet();

        return semestreItems
            .Where(x => maquetteIds.Contains(x.MaquettePedagogiqueId))
            .OrderBy(x => x.NumeroSemestre)
            .ThenBy(x => x.Libelle)
            .Select(x => new LookupDto { Id = x.Id, Libelle = $"S{x.NumeroSemestre} - {x.Libelle}" })
            .ToList();
    }

    public async Task<List<LookupDto>> GetUnitesEnseignementLookupAsync(
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        long? semestrePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var items = await unitesEnseignement.ListAsync(cancellationToken);
        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);
        var maquetteItems = await maquettesPedagogiques.ListAsync(cancellationToken);
        var maquetteEcItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var ouvertureItems = await parcoursAcademiques.ListAsync(cancellationToken);

        if (anneeAcademiqueId is not null)
        {
            var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
            var annee = anneeItems.FirstOrDefault(x => x.Id == anneeAcademiqueId);

            if (annee is not null)
            {
                maquetteItems = maquetteItems
                    .Where(x => x.Version == annee.Libelle)
                    .ToList();
            }
        }

        if (cycleFormationId is not null)
        {
            var ouvertureIds = ouvertureItems
                .Where(x => x.CycleFormationId == cycleFormationId)
                .Select(x => x.Id)
                .ToHashSet();

            maquetteItems = maquetteItems
                .Where(x => x.ParcoursAcademiqueId is not null && ouvertureIds.Contains(x.ParcoursAcademiqueId.Value))
                .ToList();
        }

        if (anneeAcademiqueId is not null || cycleFormationId is not null)
        {
            var maquetteIds = maquetteItems.Select(x => x.Id).ToHashSet();

            var semestreIds = semestreItems
                .Where(x => maquetteIds.Contains(x.MaquettePedagogiqueId))
                .Select(x => x.Id)
                .ToHashSet();

            var ueIds = maquetteEcItems
                .Where(x => semestreIds.Contains(x.SemestrePedagogiqueId))
                .Select(x => x.ElementConstitutifId)
                .ToHashSet();

            var catalogUeIds = (await elementsConstitutifs.ListAsync(cancellationToken))
                .Where(x => ueIds.Contains(x.Id))
                .Select(x => x.UniteEnseignementId)
                .ToHashSet();

            items = items.Where(x => catalogUeIds.Contains(x.Id)).ToList();
        }

        if (semestrePedagogiqueId is not null)
        {
            var ecIds = maquetteEcItems
                .Where(x => x.SemestrePedagogiqueId == semestrePedagogiqueId)
                .Select(x => x.ElementConstitutifId)
                .ToHashSet();

            var catalogUeIds = (await elementsConstitutifs.ListAsync(cancellationToken))
                .Where(x => ecIds.Contains(x.Id))
                .Select(x => x.UniteEnseignementId)
                .ToHashSet();

            items = items.Where(x => catalogUeIds.Contains(x.Id)).ToList();
        }

        return items
            .OrderBy(x => x.Code)
            .Select(x => new LookupDto { Id = x.Id, Libelle = FormatCodeLibelle(x.Code, x.Libelle) })
            .ToList();
    }

    public async Task<List<LookupDto>> GetElementsConstitutifsLookupAsync(
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        long? semestrePedagogiqueId = null,
        long? uniteEnseignementId = null,
        CancellationToken cancellationToken = default)
    {
        var items = await elementsConstitutifs.ListAsync(cancellationToken);
        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);
        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);
        var maquetteItems = await maquettesPedagogiques.ListAsync(cancellationToken);
        var maquetteEcItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var ouvertureItems = await parcoursAcademiques.ListAsync(cancellationToken);

        if (anneeAcademiqueId is not null)
        {
            var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
            var annee = anneeItems.FirstOrDefault(x => x.Id == anneeAcademiqueId);

            if (annee is not null)
            {
                maquetteItems = maquetteItems
                    .Where(x => x.Version == annee.Libelle)
                    .ToList();
            }
        }

        if (cycleFormationId is not null)
        {
            var ouvertureIds = ouvertureItems
                .Where(x => x.CycleFormationId == cycleFormationId)
                .Select(x => x.Id)
                .ToHashSet();

            var maquetteIds = maquetteItems
                .Where(x => x.ParcoursAcademiqueId is not null && ouvertureIds.Contains(x.ParcoursAcademiqueId.Value))
                .Select(x => x.Id)
                .ToHashSet();

            var semestreIds = semestreItems
                .Where(x => maquetteIds.Contains(x.MaquettePedagogiqueId))
                .Select(x => x.Id)
                .ToHashSet();

            var ecIds = maquetteEcItems
                .Where(x => semestreIds.Contains(x.SemestrePedagogiqueId))
                .Select(x => x.ElementConstitutifId)
                .ToHashSet();

            items = items.Where(x => ecIds.Contains(x.Id)).ToList();
        }

        if (semestrePedagogiqueId is not null)
        {
            var ecIds = maquetteEcItems
                .Where(x => x.SemestrePedagogiqueId == semestrePedagogiqueId)
                .Select(x => x.ElementConstitutifId)
                .ToHashSet();

            items = items.Where(x => ecIds.Contains(x.Id)).ToList();
        }

        if (uniteEnseignementId is not null)
        {
            items = items.Where(x => x.UniteEnseignementId == uniteEnseignementId).ToList();
        }

        return items
            .OrderBy(x => x.Libelle)
            .Select(x => new LookupDto { Id = x.Id, Libelle = FormatCodeLibelle(x.Code, x.Libelle) })
            .ToList();
    }

    public async Task<List<LookupDto>> GetSessionsNormalesLookupAsync(
        long? anneeAcademiqueId = null,
        long? maquetteElementConstitutifId = null,
        CancellationToken cancellationToken = default)
    {
        var items = await evaluations.ListAsync(cancellationToken);

        items = items.Where(x => x.Type == TypeEvaluation.SessionNormale).ToList();

        if (anneeAcademiqueId is not null)
        {
            items = items.Where(x => x.AnneeAcademiqueId == anneeAcademiqueId).ToList();
        }

        if (maquetteElementConstitutifId is not null)
        {
            items = items.Where(x => x.MaquetteElementConstitutifId == maquetteElementConstitutifId).ToList();
        }

        return items
            .OrderBy(x => x.Numero)
            .Select(x => new LookupDto { Id = x.Id, Libelle = $"{x.Code} - {x.Libelle}" })
            .ToList();
    }

    private static EvaluationAcademiqueDto ToDto(
        EvaluationAcademique evaluation,
        IReadOnlyCollection<AnneeAcademique> anneeItems,
        IReadOnlyCollection<ElementConstitutif> ecItems,
        IReadOnlyCollection<UniteEnseignement> ueItems,
        IReadOnlyCollection<SemestrePedagogique> semestreItems,
        IReadOnlyCollection<MaquettePedagogique> maquetteItems,
        IReadOnlyCollection<MaquetteElementConstitutif> maquetteEcItems,
        IReadOnlyCollection<ParcoursAcademique> ouvertureItems,
        IReadOnlyCollection<CycleFormation> cycleItems,
        IReadOnlyCollection<EvaluationAcademique> evaluationItems,
        IReadOnlyCollection<AffectationMaquette> affectationItems,
        IReadOnlyCollection<NoteEvaluation> noteItems)
    {
        var annee = anneeItems.FirstOrDefault(x => x.Id == evaluation.AnneeAcademiqueId);
        var maquetteEc = maquetteEcItems.FirstOrDefault(x => x.Id == evaluation.MaquetteElementConstitutifId);
        var ec = maquetteEc is null ? null : ecItems.FirstOrDefault(x => x.Id == maquetteEc.ElementConstitutifId);
        var ue = ec is null ? null : ueItems.FirstOrDefault(x => x.Id == ec.UniteEnseignementId);
        var semestre = maquetteEc is null ? null : semestreItems.FirstOrDefault(x => x.Id == maquetteEc.SemestrePedagogiqueId);
        var maquette = semestre is null ? null : maquetteItems.FirstOrDefault(x => x.Id == semestre.MaquettePedagogiqueId);
        var parcoursIds = maquette is null
            ? []
            : affectationItems
                .Where(x => x.MaquettePedagogiqueId == maquette.Id)
                .OrderByDescending(x => x.EstActive)
                .ThenByDescending(x => x.EstPrincipale)
                .Select(x => x.ParcoursAcademiqueId)
                .ToList();
        if (maquette?.ParcoursAcademiqueId is not null)
        {
            parcoursIds.Add(maquette.ParcoursAcademiqueId.Value);
        }

        var ouverture = ouvertureItems.FirstOrDefault(x => parcoursIds.Contains(x.Id));
        var cycle = ouverture is null ? null : cycleItems.FirstOrDefault(x => x.Id == ouverture.CycleFormationId);
        var remplacee = evaluation.EvaluationRemplaceeId is null
            ? null
            : evaluationItems.FirstOrDefault(x => x.Id == evaluation.EvaluationRemplaceeId);

        return new EvaluationAcademiqueDto
        {
            Id = evaluation.Id,
            AnneeAcademiqueId = evaluation.AnneeAcademiqueId,
            AnneeAcademiqueLibelle = annee?.Libelle ?? string.Empty,
            ParcoursAcademiqueId = ouverture?.Id ?? 0,
            ParcoursAcademiqueLibelle = ouverture?.Libelle ?? string.Empty,
            MaquetteElementConstitutifId = evaluation.MaquetteElementConstitutifId,
            ElementConstitutifLibelle = ec is null ? string.Empty : FormatCodeLibelle(ec.Code, ec.Libelle),
            UniteEnseignementId = ue?.Id ?? 0,
            UniteEnseignementLibelle = ue is null ? string.Empty : FormatCodeLibelle(ue.Code, ue.Libelle),
            SemestrePedagogiqueId = semestre?.Id ?? 0,
            SemestrePedagogiqueLibelle = semestre is null ? string.Empty : $"S{semestre.NumeroSemestre} - {semestre.Libelle}",
            CycleFormationId = ouverture?.CycleFormationId ?? 0,
            CycleFormationLibelle = cycle is null ? string.Empty : FormatCodeLibelle(cycle.Code, cycle.Libelle),
            Type = evaluation.Type,
            TypeLibelle = GetTypeLibelle(evaluation.Type),
            Numero = evaluation.Numero,
            Code = evaluation.Code,
            Libelle = evaluation.Libelle,
            Bareme = evaluation.Bareme,
            PonderationPourcentage = evaluation.PonderationPourcentage,
            DateEvaluation = evaluation.DateEvaluation?.ToDateTime(TimeOnly.MinValue),
            EvaluationRemplaceeId = evaluation.EvaluationRemplaceeId,
            EvaluationRemplaceeLibelle = remplacee is null ? null : $"{remplacee.Code} - {remplacee.Libelle}",
            Observation = evaluation.Observation,
            NombreNotes = noteItems.Count(x => x.EvaluationAcademiqueId == evaluation.Id)
        };
    }

    public static decimal GetDefaultPonderation(TypeEvaluation type)
        => type switch
        {
            TypeEvaluation.ControleContinu => 20m,
            TypeEvaluation.ControleConnaissance => 10m,
            TypeEvaluation.SessionNormale => 70m,
            TypeEvaluation.SessionRattrapage => 70m,
            _ => 0m
        };

    public static string GetTypeLibelle(TypeEvaluation type)
        => type switch
        {
            TypeEvaluation.ControleContinu => "CCON - Contrôle continu",
            TypeEvaluation.ControleConnaissance => "CC - Contrôle de connaissance",
            TypeEvaluation.SessionNormale => "SN - Session normale",
            TypeEvaluation.SessionRattrapage => "SR - Session de rattrapage",
            _ => type.ToString()
        };

    public static string GetTypeCode(TypeEvaluation type)
        => type switch
        {
            TypeEvaluation.ControleContinu => "CCON",
            TypeEvaluation.ControleConnaissance => "CC",
            TypeEvaluation.SessionNormale => "SN",
            TypeEvaluation.SessionRattrapage => "SR",
            _ => "EV"
        };

    private static HashSet<long> ResolveMaquetteIds(
        long parcoursAcademiqueId,
        IReadOnlyCollection<MaquettePedagogique> maquettes,
        IReadOnlyCollection<AffectationMaquette> affectations)
    {
        var ids = affectations
            .Where(x => x.ParcoursAcademiqueId == parcoursAcademiqueId && x.EstActive)
            .OrderByDescending(x => x.EstPrincipale)
            .ThenByDescending(x => x.Id)
            .Select(x => x.MaquettePedagogiqueId)
            .Take(1)
            .ToHashSet();

        if (ids.Count == 0)
        {
            ids = affectations
                .Where(x => x.ParcoursAcademiqueId == parcoursAcademiqueId)
                .OrderByDescending(x => x.EstPrincipale)
                .ThenByDescending(x => x.Id)
                .Select(x => x.MaquettePedagogiqueId)
                .Take(1)
                .ToHashSet();
        }

        if (ids.Count == 0)
        {
            ids = maquettes
                .Where(x => x.ParcoursAcademiqueId == parcoursAcademiqueId)
                .OrderByDescending(x => x.Id)
                .Take(1)
                .Select(x => x.Id)
                .ToHashSet();
        }

        return ids;
    }

    private async Task<MaquetteElementConstitutif> ResolveMaquetteElementForParcoursAsync(
        long parcoursAcademiqueId,
        long maquetteElementConstitutifId,
        CancellationToken cancellationToken)
    {
        var maquetteIds = ResolveMaquetteIds(
            parcoursAcademiqueId,
            await maquettesPedagogiques.ListAsync(cancellationToken),
            await affectationsMaquettes.ListAsync(cancellationToken));
        var semestreIds = (await semestresPedagogiques.ListAsync(cancellationToken))
            .Where(x => maquetteIds.Contains(x.MaquettePedagogiqueId))
            .Select(x => x.Id)
            .ToHashSet();

        return (await maquetteElementsConstitutifs.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == maquetteElementConstitutifId && semestreIds.Contains(x.SemestrePedagogiqueId))
            ?? throw new InvalidOperationException("L'EC sélectionné n'appartient pas à la maquette du parcours académique.");
    }

    private static string FormatCodeLibelle(string? code, string libelle)
        => string.IsNullOrWhiteSpace(code) ? libelle : $"{code} - {libelle}";

    private static string NormalizeCode(string? value)
        => RequireText(value, "Le code est obligatoire.").ToUpperInvariant();

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
