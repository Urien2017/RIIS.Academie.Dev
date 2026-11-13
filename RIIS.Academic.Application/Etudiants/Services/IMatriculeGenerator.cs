namespace RIIS.Academic.Application.Etudiants.Services;

public interface IMatriculeGenerator
{
    Task<string> GenerateMatriculeAsync(long? anneeAcademiqueId = null, CancellationToken cancellationToken = default);
}
