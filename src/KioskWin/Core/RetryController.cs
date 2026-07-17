namespace KioskWin.Core;

public interface IRetryScheduler
{
    bool IsRunning { get; }
    void Start(TimeSpan interval, Action onTick);
    void Stop();
}

public enum RetryState
{
    Idle,
    Retrying,
}

public sealed class RetryController
{
    private readonly TimeSpan _interval;
    private readonly IRetryScheduler _scheduler;

    public RetryController(TimeSpan interval, IRetryScheduler scheduler)
    {
        _interval = interval;
        _scheduler = scheduler;
    }

    public RetryState State { get; private set; } = RetryState.Idle;
    public event Action? ShouldRetry;

    public void ReportFailure()
    {
        if (State == RetryState.Retrying) return;
        State = RetryState.Retrying;
        _scheduler.Start(_interval, () => ShouldRetry?.Invoke());
    }

    public void ReportSuccess()
    {
        _scheduler.Stop();
        State = RetryState.Idle;
    }
}
