using RIIS.Academic.Application.Abstractions.Services;

namespace RIIS.Academic.Application.Common.Loading;

public sealed class DataLoadingState : IDataLoadingState, IDisposable
{
    private readonly object sync = new();
    private CancellationTokenSource? hideCancellation;
    private Timer? progressTimer;
    private int activeOperations;
    private long stateVersion;
    private bool isDisposed;

    public bool IsLoading { get; private set; }

    public int ProgressPercentage { get; private set; }

    public string Message { get; private set; } = "Chargement des données...";

    public event EventHandler? Changed;

    public IDisposable Begin(string message = "Chargement des données...")
    {
        lock (sync)
        {
            ThrowIfDisposed();
            activeOperations++;
            hideCancellation?.Cancel();
            hideCancellation?.Dispose();
            hideCancellation = null;

            if (activeOperations == 1)
            {
                IsLoading = true;
                ProgressPercentage = 8;
                Message = message;
                progressTimer = new Timer(
                    AdvanceProgress,
                    null,
                    TimeSpan.FromMilliseconds(250),
                    TimeSpan.FromMilliseconds(250));
            }
        }

        RaiseChanged();
        return new LoadingLease(this);
    }

    private void AdvanceProgress(object? state)
    {
        lock (sync)
        {
            if (isDisposed || !IsLoading || activeOperations == 0)
            {
                return;
            }

            ProgressPercentage = Math.Min(ProgressPercentage + 7, 92);
        }

        RaiseChanged();
    }

    private void Complete()
    {
        long version;

        lock (sync)
        {
            if (isDisposed || activeOperations == 0)
            {
                return;
            }

            activeOperations--;
            if (activeOperations > 0)
            {
                return;
            }

            ProgressPercentage = 100;
            version = ++stateVersion;
            hideCancellation = new CancellationTokenSource();
        }

        RaiseChanged();
        _ = HideAfterCompletionAsync(version, hideCancellation.Token);
    }

    private async Task HideAfterCompletionAsync(long version, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(220, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        lock (sync)
        {
            if (isDisposed || activeOperations != 0 || version != stateVersion)
            {
                return;
            }

            IsLoading = false;
            ProgressPercentage = 0;
            Message = "Chargement des données...";
            progressTimer?.Dispose();
            progressTimer = null;
            hideCancellation?.Dispose();
            hideCancellation = null;
        }

        RaiseChanged();
    }

    private void RaiseChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            hideCancellation?.Cancel();
            hideCancellation?.Dispose();
            hideCancellation = null;
            progressTimer?.Dispose();
            progressTimer = null;
        }
    }

    private sealed class LoadingLease(DataLoadingState owner) : IDisposable
    {
        private DataLoadingState? owner = owner;

        public void Dispose()
        {
            Interlocked.Exchange(ref owner, null)?.Complete();
        }
    }
}
