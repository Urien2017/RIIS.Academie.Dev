namespace RIIS.Academic.Application.Etudiants.Services;

public interface IMatriculeGenerator
{
    Task<string> GenerateMatriculeAsync(CancellationToken cancellationToken = default);
}
