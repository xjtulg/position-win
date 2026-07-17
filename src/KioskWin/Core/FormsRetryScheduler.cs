using System.Windows.Forms;

namespace KioskWin.Core;

public sealed class FormsRetryScheduler : IRetryScheduler
{
    private readonly System.Windows.Forms.Timer _timer = new() { Enabled = false };
    private Action? _onTick;

    public bool IsRunning => _timer.Enabled;

    public void Start(TimeSpan interval, Action onTick)
    {
        if (_timer.Enabled) return;
        _onTick = onTick;
        _timer.Interval = Math.Max((int)interval.TotalMilliseconds, 1);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        _onTick = null;
    }

    private void OnTick(object? sender, EventArgs e) => _onTick?.Invoke();
}
