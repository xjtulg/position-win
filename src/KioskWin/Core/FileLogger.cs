namespace KioskWin.Core;

public sealed class FileLogger
{
    private readonly string _logDirectory;
    private readonly object _lock = new();

    public FileLogger()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KioskWin", "logs"))
    {
    }

    public FileLogger(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public void Log(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}";
        var path = Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
        lock (_lock)
        {
            File.AppendAllText(path, line);
        }
    }
}
