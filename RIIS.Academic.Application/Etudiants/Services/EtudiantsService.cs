using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Etudiants.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Etudiants.Services;

public class EtudiantsService(
    IRepository<Etudiant> etudiants,
    IRepository<DossierEtudiant> dossiersEtudiants) : IEtudiantsService
{
    public async Task<List<EtudiantDto>> GetEtudiantsAsync(
        string? recherche = null,
        CancellationToken cancellationToken = default)
    {
        var items = await etudiants.ListAsync(cancellationToken);
        var normalizedSearch = recherche?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            items = items
                .Where(x =>
                    Contains(x.Nom, normalizedSearch)
                    || Contains(x.Prenoms, normalizedSearch)
                    || Contains($"{x.Nom} {x.Prenoms}", normalizedSearch))
                .ToList();
        }

        return items
            .OrderBy(x => x.Nom)
            .ThenBy(x => x.Prenoms)
            .Select(ToDto)
            .ToList();
    }

    public async Task<EtudiantDto?> GetEtudiantAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await etudiants.GetByIdAsync(id, cancellationToken);

        return entity is null ? null : ToDto(entity);
    }

    public EtudiantDto CreateDefaultEtudiant()
        => new()
        {
            DateNaissance = new DateOnly(2000, 1, 1),
            LieuNaissance = "Non renseigné",
            Sexe = Sexe.NonRenseigne,
            AptitudeMedicale = AptitudeMedicale.NonRenseignee,
            Nationalite = "Camerounaise"
        };

    public async Task SaveEtudiantAsync(EtudiantDto dto, CancellationToken cancellationToken = default)
    {
        dto.Nom = RequireText(dto.Nom, "Le nom de l'étudiant est obligatoire.").ToUpperInvariant();
        dto.Prenoms = RequireText(dto.Prenoms, "Les prénoms de l'étudiant sont obligatoires.");
        dto.LieuNaissance = RequireText(dto.LieuNaissance, "Le lieu de naissance est obligatoire.");
        dto.Nationalite = RequireText(dto.Nationalite, "La nationalité est obligatoire.");

        if (dto.Id == 0)
        {
            await etudiants.AddAsync(new Etudiant
            {
                Nom = dto.Nom,
                Prenoms = dto.Prenoms,
                DateNaissance = dto.DateNaissance,
                LieuNaissance = dto.LieuNaissance,
                Sexe = dto.Sexe,
                AptitudeMedicale = dto.AptitudeMedicale,
                Nationalite = dto.Nationalite,
                RegionOrigine = NormalizeNullable(dto.RegionOrigine),
                TelephonePrincipal = NormalizeNullable(dto.TelephonePrincipal),
                TelephoneSecondaire = NormalizeNullable(dto.TelephoneSecondaire),
                TelephoneParent1 = NormalizeNullable(dto.TelephoneParent1),
                TelephoneParent2 = NormalizeNullable(dto.TelephoneParent2),
                Email = NormalizeNullable(dto.Email),
                NomPere = NormalizeNullable(dto.NomPere),
                NomMere = NormalizeNullable(dto.NomMere),
                LieuResidence = NormalizeNullable(dto.LieuResidence),
                PhotoUrl = NormalizeNullable(dto.PhotoUrl)
            }, cancellationToken);
        }
        else
        {
            var entity = await etudiants.GetByIdAsync(dto.Id, cancellationToken);
            if (entity is null) return;

            entity.Nom = dto.Nom;
            entity.Prenoms = dto.Prenoms;
            entity.DateNaissance = dto.DateNaissance;
            entity.LieuNaissance = dto.LieuNaissance;
            entity.Sexe = dto.Sexe;
            entity.AptitudeMedicale = dto.AptitudeMedicale;
            entity.Nationalite = dto.Nationalite;
            entity.RegionOrigine = NormalizeNullable(dto.RegionOrigine);
            entity.TelephonePrincipal = NormalizeNullable(dto.TelephonePrincipal);
            entity.TelephoneSecondaire = NormalizeNullable(dto.TelephoneSecondaire);
            entity.TelephoneParent1 = NormalizeNullable(dto.TelephoneParent1);
            entity.TelephoneParent2 = NormalizeNullable(dto.TelephoneParent2);
            entity.Email = NormalizeNullable(dto.Email);
            entity.NomPere = NormalizeNullable(dto.NomPere);
            entity.NomMere = NormalizeNullable(dto.NomMere);
            entity.LieuResidence = NormalizeNullable(dto.LieuResidence);
            entity.PhotoUrl = NormalizeNullable(dto.PhotoUrl);
        }

        await etudiants.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteEtudiantAsync(long id, CancellationToken cancellationToken = default)
    {
        var hasDossier = (await dossiersEtudiants.ListAsync(cancellationToken))
            .Any(x => x.EtudiantId == id);
        if (hasDossier)
        {
            throw new InvalidOperationException(
                "Cet étudiant possède déjà un dossier et ne peut pas être supprimé.");
        }

        await etudiants.DeleteByIdAsync(id, cancellationToken);
        await etudiants.SaveChangesAsync(cancellationToken);
    }

    private static EtudiantDto ToDto(Etudiant entity)
        => new()
        {
            Id = entity.Id,
            Nom = entity.Nom,
            Prenoms = entity.Prenoms,
            DateNaissance = entity.DateNaissance,
            LieuNaissance = entity.LieuNaissance,
            Sexe = entity.Sexe,
            AptitudeMedicale = entity.AptitudeMedicale,
            Nationalite = entity.Nationalite,
            RegionOrigine = entity.RegionOrigine,
            TelephonePrincipal = entity.TelephonePrincipal,
            TelephoneSecondaire = entity.TelephoneSecondaire,
            TelephoneParent1 = entity.TelephoneParent1,
            TelephoneParent2 = entity.TelephoneParent2,
            Email = entity.Email,
            NomPere = entity.NomPere,
            NomMere = entity.NomMere,
            LieuResidence = entity.LieuResidence,
            PhotoUrl = entity.PhotoUrl,
            AdmissionInfo = entity.AdmissionInfo is null ? null : AdmissionInfoDto.FromEntity(entity.AdmissionInfo),
            Documents = entity.DocumentsPersonnels
                .OrderByDescending(x => x.DateDepotUtc)
                .Select(DocumentPersonnelDto.FromEntity)
                .ToList()
        };

    private static bool Contains(string? source, string value)
        => source?.Contains(value, StringComparison.OrdinalIgnoreCase) == true;

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
