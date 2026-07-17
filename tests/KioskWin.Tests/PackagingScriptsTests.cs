using System.Xml.Linq;
using Xunit;

namespace KioskWin.Tests;

public class PackagingScriptsTests
{
    [Fact]
    public void Startup_shortcut_scripts_are_copied_to_publish_output()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "KioskWin", "KioskWin.csproj");
        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var document = XDocument.Load(projectPath);

        AssertScriptIsCopied(document, projectDirectory, "install-shortcut.ps1");
        AssertScriptIsCopied(document, projectDirectory, "uninstall-shortcut.ps1");
    }

    private static void AssertScriptIsCopied(XDocument document, string projectDirectory, string scriptName)
    {
        var scriptPath = Path.Combine(projectDirectory, scriptName);
        Assert.True(File.Exists(scriptPath), $"Missing packaging script: {scriptPath}");

        var item = document
            .Descendants("None")
            .FirstOrDefault(e => string.Equals((string?)e.Attribute("Update"), scriptName, StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(item);
        Assert.Equal("PreserveNewest", (string?)item!.Element("CopyToOutputDirectory"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "KioskWin.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
