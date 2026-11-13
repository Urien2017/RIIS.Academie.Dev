namespace RIIS.Academic.Application.Abstractions.Services;

public interface IDataLoadingState
{
    bool IsLoading { get; }

    int ProgressPercentage { get; }

    string Message { get; }

    event EventHandler? Changed;

    IDisposable Begin(string message = "Chargement des données...");
}
