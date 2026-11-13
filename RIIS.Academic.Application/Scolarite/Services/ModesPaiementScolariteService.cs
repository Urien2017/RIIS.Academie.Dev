using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Scolarite.Dtos;
using RIIS.Academic.Domain;
using System.Globalization;
using System.Text;

namespace RIIS.Academic.Application.Scolarite.Services;

public class ModesPaiementScolariteService(
    IRepository<ModePaiementScolarite> modesPaiementScolarite) : IModesPaiementScolariteService
{
    public async Task<List<ModePaiementScolariteDto>> GetModesPaiementScolariteAsync(
        bool inclureInactifs = false,
        CancellationToken cancellationToken = default)
    {
        var items = await modesPaiementScolarite.ListAsync(cancellationToken);

        return items
            .Where(x => inclureInactifs || x.EstActif)
            .OrderBy(x => x.OrdreAffichage)
            .ThenBy(x => x.Libelle)
            .Select(ToDto)
            .ToList();
    }

    public ModePaiementScolariteDto CreateDefaultModePaiementScolarite()
        => new()
        {
            EstActif = true
        };

    public string GenerateCodeFromLibelle(string? libelle)
    {
        var normalized = RemoveDiacritics(
                RequireText(libelle, "Le libelle du mode de paiement est obligatoire."))
            .ToUpperInvariant();
        var words = normalized
            .Split([' ', '-', '_', '\'', '/', '\\', '.', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(x => new string(x.Where(char.IsLetterOrDigit).ToArray()))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (words.Count == 0)
        {
            throw new InvalidOperationException(
                "Le libelle du mode de paiement doit contenir au moins une lettre ou un chiffre.");
        }

        return words.Count == 1
            ? words[0][..Math.Min(3, words[0].Length)]
            : string.Concat(words.Select(x => x[0]))[..Math.Min(8, words.Count)];
    }

    public async Task SaveModePaiementScolariteAsync(
        ModePaiementScolariteDto dto,
        CancellationToken cancellationToken = default)
    {
        dto.Code = RequireText(dto.Code, "Le code du mode de paiement est obligatoire.").ToUpperInvariant();
        dto.Libelle = RequireText(dto.Libelle, "Le libelle du mode de paiement est obligatoire.");

        var items = await modesPaiementScolarite.ListAsync(cancellationToken);
        if (items.Any(x => x.Id != dto.Id && string.Equals(x.Code, dto.Code, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Un mode de paiement existe deja avec ce code.");
        }

        if (dto.Id == 0)
        {
            await modesPaiementScolarite.AddAsync(new ModePaiementScolarite
            {
                Code = dto.Code,
                Libelle = dto.Libelle,
                OrdreAffichage = dto.OrdreAffichage,
                EstActif = dto.EstActif
            }, cancellationToken);
        }
        else
        {
            var entity = await modesPaiementScolarite.GetByIdAsync(dto.Id, cancellationToken);
            if (entity is null) return;

            entity.Code = dto.Code;
            entity.Libelle = dto.Libelle;
            entity.OrdreAffichage = dto.OrdreAffichage;
            entity.EstActif = dto.EstActif;
        }

        await modesPaiementScolarite.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteModePaiementScolariteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        await modesPaiementScolarite.DeleteByIdAsync(id, cancellationToken);
        await modesPaiementScolarite.SaveChangesAsync(cancellationToken);
    }

    private static ModePaiementScolariteDto ToDto(ModePaiementScolarite entity)
        => new()
        {
            Id = entity.Id,
            Code = entity.Code,
            Libelle = entity.Libelle,
            OrdreAffichage = entity.OrdreAffichage,
            EstActif = entity.EstActif
        };

    private static string RequireText(string? value, string errorMessage)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return normalized;
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
