
using RIIS.Academic.Application.Abstractions.Services;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Notes.Services;

public class CalculNotesService : ICalculNotesService
{
    public decimal? CalculerMoyenneElementConstitutif(decimal? moyenneCcon, decimal? moyenneCc, decimal? moyenneSnOuSr)
    {
        // Une évaluation peut ne pas être prévue pour un EC (par exemple CC
        // absent). Les pondérations des évaluations réellement disponibles
        // sont renormalisées afin que leur somme représente 100 %.
        var composantes = new[]
        {
            (Valeur: moyenneCcon, Ponderation: 0.20m),
            (Valeur: moyenneCc, Ponderation: 0.10m),
            (Valeur: moyenneSnOuSr, Ponderation: 0.70m)
        };

        var disponibles = composantes
            .Where(x => x.Valeur.HasValue)
            .ToList();

        if (disponibles.Count == 0)
        {
            return null;
        }

        var ponderationTotale = disponibles.Sum(x => x.Ponderation);
        var moyenne = disponibles.Sum(x => x.Valeur!.Value * x.Ponderation) / ponderationTotale;

        return Math.Round(moyenne, 2);
    }

    public bool EstEligibleRattrapage(decimal? moyenneElementConstitutif)
        => moyenneElementConstitutif.HasValue && moyenneElementConstitutif.Value < 10m;

    public decimal CalculerCreditsAcquis(MaquetteElementConstitutif maquetteElementConstitutif, decimal? moyenneRetenue)
        => moyenneRetenue.HasValue && moyenneRetenue.Value >= 10m ? maquetteElementConstitutif.Credits : 0m;
}
