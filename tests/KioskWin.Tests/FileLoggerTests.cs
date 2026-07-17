using KioskWin.Core;
using Xunit;

namespace KioskWin.Tests;

public class FileLoggerTests
{
    [Fact]
    public void Log_creates_dated_file_and_writes_message()
    {
        var dir = Path.Combine(Path.GetTempPath(), "KioskWinTest_" + Guid.NewGuid().ToString("N"));
        var logger = new FileLogger(dir);

        logger.Log("hello world");

        var file = Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd}.log");
        Assert.True(File.Exists(file));
        var content = File.ReadAllText(file);
        Assert.Contains("hello world", content);
    }

    [Fact]
    public void Log_appends_multiple_lines_to_same_file()
    {
        var dir = Path.Combine(Path.GetTempPath(), "KioskWinTest_" + Guid.NewGuid().ToString("N"));
        var logger = new FileLogger(dir);

        logger.Log("line one");
        logger.Log("line two");

        var file = Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd}.log");
        var lines = File.ReadAllLines(file);
        Assert.Equal(2, lines.Length);
        Assert.Contains("line one", lines[0]);
        Assert.Contains("line two", lines[1]);
    }
}
