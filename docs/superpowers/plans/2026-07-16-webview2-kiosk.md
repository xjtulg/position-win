# KioskWin 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个 WinForms + WebView2 的全屏锁定程序，加载可配置远程网页，防止普通用户关闭或切到后台。

**Architecture:** 单 WinExe 项目（`KioskWin`，net8.0-windows）+ xUnit 测试项目。纯逻辑放 `Core/`（配置、密码哈希、重试状态机、日志、组合键解析），TDD 覆盖；Win32/WebView2/窗口行为放 `Forms/`，用手动 QA 清单覆盖。`Program.cs` 负责入口、单实例、异常兜底、`--hash-password` CLI。自包含单文件发布，拷贝即用。

**Tech Stack:** C# / .NET 8 (net8.0-windows), WinForms, Microsoft.Web.WebView2 (Evergreen), Microsoft.Extensions.Configuration, xUnit.

## Global Constraints

- **目标框架**：`net8.0-windows`。构建/发布/测试均在 Windows 上运行（WSL 无 dotnet；源码在 `/mnt/d/code/position-win` 即 Windows `D:` 盘，编辑在 WSL，编译在 Windows）。
- **NuGet 版本**：`Microsoft.Web.WebView2` 1.0.2903.40；`Microsoft.Extensions.Configuration.Json` 8.0.1；`Microsoft.Extensions.Configuration.Binder` 8.0.2；测试用 `Microsoft.NET.Test.Sdk` 17.11.1、`xunit` 2.9.2、`xunit.runner.visualstudio` 2.8.2、`Microsoft.Extensions.Configuration.Memory` 8.0.1。如某个版本恢复失败，改用同主版本的最新修订号。
- **命名空间**：主项目 `KioskWin`、`KioskWin.Core`、`KioskWin.Forms`；测试 `KioskWin.Tests`。
- **UI 全部代码构造**：不使用 `.Designer.cs`（计划中所有控件在构造函数里 new 出来），便于在无 WinForms 设计器的环境下完整呈现代码。
- **配置文件**：`appsettings.json` 与 exe 同目录，`CopyToOutputDirectory=PreserveNewest`。**禁止存明文密码**——存 `AdminPasswordHash`(SHA-256 hex) + 随机 `PasswordSalt`。
- **单实例互斥锁名**：`Global\KioskWin_SingleInstance`。
- **日志目录**：`%LocalAppData%\KioskWin\logs\`，按天 `yyyy-MM-dd.log`。
- **每完成一个任务即 commit**（frequent commits）。仓库已初始化于项目根目录。
- **YAGNI**：只实现规格第 2 节的需求；中等/高强度锁定（规格第 12 节）不做。

---

## 文件结构（本计划创建/修改的文件）

| 文件 | 职责 | 创建任务 |
|---|---|---|
| `KioskWin.sln` | 解决方案 | Task 1 |
| `src/KioskWin/KioskWin.csproj` | WinForms 项目 + 依赖 | Task 1 |
| `src/KioskWin/Program.cs` | 入口（占位→最终） | Task 1 / Task 10 |
| `src/KioskWin/appsettings.json` | 配置 | Task 1 |
| `src/KioskWin/Core/FileLogger.cs` | 按天文件日志 | Task 2 |
| `src/KioskWin/Core/PasswordHasher.cs` | SHA-256(salt+pwd) 哈希/校验 | Task 3 |
| `src/KioskWin/Core/KeyCombinationParser.cs` | "Ctrl+Shift+Alt+Q"→Keys | Task 4 |
| `src/KioskWin/Core/KioskConfig.cs` | 配置 POCO + 加载/校验/默认 | Task 5 |
| `src/KioskWin/Core/RetryController.cs` | 重试状态机 + IRetryScheduler | Task 6 |
| `src/KioskWin/Core/FormsRetryScheduler.cs` | 生产用 Forms.Timer 调度器 | Task 6 |
| `src/KioskWin/Forms/RetryOverlay.cs` | 复用全屏遮罩控件 | Task 7 |
| `src/KioskWin/Forms/AdminDialog.cs` | 密码框 + 工具按钮对话框 | Task 8 |
| `src/KioskWin/Forms/MainForm.cs` | 无边框全屏 + WebView2 + 锁定 + 重试 + 管理员入口 | Task 9 |
| `src/KioskWin/install-shortcut.ps1` | 开机自启快捷方式 | Task 11 |
| `tests/KioskWin.Tests/KioskWin.Tests.csproj` | 测试项目 | Task 1 |
| `tests/KioskWin.Tests/*.cs` | Core 单元测试 | Task 2-6 |

---

## Task 1: 项目脚手架与构建验证

**Files:**
- Create: `KioskWin.sln`
- Create: `src/KioskWin/KioskWin.csproj`
- Create: `src/KioskWin/Program.cs`（占位）
- Create: `src/KioskWin/appsettings.json`
- Create: `tests/KioskWin.Tests/KioskWin.Tests.csproj`
- Create: `tests/KioskWin.Tests/SmokeTests.cs`

**Interfaces:**
- Produces: 可构建的解决方案；`KioskWin` 命名空间；后续任务在此基础上加文件。

- [ ] **Step 1: 创建解决方案**

在项目根目录（`D:\code\position-win`，WSL 路径 `/mnt/d/code/position-win`）运行：

```bash
dotnet new sln -n KioskWin
```

预期：生成 `KioskWin.sln`。

- [ ] **Step 2: 写主项目 csproj**

创建 `src/KioskWin/KioskWin.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>KioskWin</RootNamespace>
    <AssemblyName>KioskWin</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2903.40" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.1" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="8.0.2" />
  </ItemGroup>

  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

- [ ] **Step 3: 写占位 Program.cs**

创建 `src/KioskWin/Program.cs`（Task 10 会替换为最终版本）：

```csharp
namespace KioskWin;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new Form());
    }
}
```

- [ ] **Step 4: 写 appsettings.json**

创建 `src/KioskWin/appsettings.json`（密码哈希先留空，部署时用 `--hash-password` 填充）：

```json
{
  "Url": "https://position.lmding.cn/public/wjbl/xcj/dist/index.html#/",
  "AdminKeyCombination": "Ctrl+Shift+Alt+Q",
  "AdminPasswordHash": "",
  "PasswordSalt": "",
  "RetryIntervalSeconds": 10,
  "TopMost": true,
  "ShowInTaskbar": false,
  "UserDataFolder": "WebView2Data"
}
```

- [ ] **Step 5: 写测试项目 csproj**

创建 `tests/KioskWin.Tests/KioskWin.Tests.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Memory" Version="8.0.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\KioskWin\KioskWin.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 6: 写一个冒烟测试**

创建 `tests/KioskWin.Tests/SmokeTests.cs`：

```csharp
using Xunit;

namespace KioskWin.Tests;

public class SmokeTests
{
    [Fact]
    public void TestRunnerWorks()
    {
        // 仅验证测试运行器与项目引用链路正常；Task 2 起被真正的 Core 测试取代
        Assert.Equal(1, 1);
    }
}
```

- [ ] **Step 7: 把两个项目加入解决方案**

```bash
dotnet sln add src/KioskWin/KioskWin.csproj
dotnet sln add tests/KioskWin.Tests/KioskWin.Tests.csproj
```

- [ ] **Step 8: 还原 + 构建 + 测试**

```bash
dotnet restore
dotnet build KioskWin.sln -c Debug
dotnet test
```

预期：还原成功；构建 0 错误；测试通过 1 个。

- [ ] **Step 9: 提交**

```bash
git add -A
git commit -m "chore: scaffold KioskWin solution and test project"
```

---

## Task 2: FileLogger（Core，TDD）

**Files:**
- Create: `src/KioskWin/Core/FileLogger.cs`
- Create: `tests/KioskWin.Tests/FileLoggerTests.cs`

**Interfaces:**
- Produces: `KioskWin.Core.FileLogger`
  - `FileLogger()` — 默认写到 `%LocalAppData%\KioskWin\logs\`
  - `FileLogger(string logDirectory)` — 用于测试注入目录
  - `void Log(string message)` — 追加一行（带时间戳）到 `yyyy-MM-dd.log`

- [ ] **Step 1: 写失败测试**

创建 `tests/KioskWin.Tests/FileLoggerTests.cs`：

```csharp
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
```

- [ ] **Step 2: 删除冒烟测试并运行测试确认失败**

Task 1 的冒烟测试使命已完成，删除它：

```bash
rm tests/KioskWin.Tests/SmokeTests.cs
dotnet test
```

预期：编译失败 / 测试失败，提示 `FileLogger` 未定义。

- [ ] **Step 3: 实现 FileLogger**

创建 `src/KioskWin/Core/FileLogger.cs`：

```csharp
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
```

- [ ] **Step 4: 运行测试确认通过**

```bash
dotnet test --filter "FullyQualifiedName~FileLoggerTests"
```

预期：2 个测试通过。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(core): add FileLogger with daily file rotation"
```

---

## Task 3: PasswordHasher（Core，TDD）

**Files:**
- Create: `src/KioskWin/Core/PasswordHasher.cs`
- Create: `tests/KioskWin.Tests/PasswordHasherTests.cs`

**Interfaces:**
- Produces: `KioskWin.Core.PasswordHasher`
  - `record PasswordHashResult(string Hash, string Salt)`
  - `static PasswordHashResult Generate(string password)` — 随机 16 字节 salt，返回 hex
  - `static bool Verify(string password, string expectedHashHex, string saltHex)` — 常数时间比较
- 算法：`SHA-256(UTF8(password) + salt)`，hex 编码。

- [ ] **Step 1: 写失败测试**

创建 `tests/KioskWin.Tests/PasswordHasherTests.cs`：

```csharp
using KioskWin.Core;
using Xunit;

namespace KioskWin.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_accepts_correct_password()
    {
        var result = PasswordHasher.Generate("s3cret!");
        Assert.True(PasswordHasher.Verify("s3cret!", result.Hash, result.Salt));
    }

    [Fact]
    public void Verify_rejects_wrong_password()
    {
        var result = PasswordHasher.Generate("s3cret!");
        Assert.False(PasswordHasher.Verify("wrong", result.Hash, result.Salt));
    }

    [Fact]
    public void Generate_produces_different_salt_each_call()
    {
        var a = PasswordHasher.Generate("same");
        var b = PasswordHasher.Generate("same");
        Assert.NotEqual(a.Salt, b.Salt);
        Assert.NotEqual(a.Hash, b.Hash);
    }

    [Fact]
    public void Verify_rejects_empty_hash_or_salt()
    {
        Assert.False(PasswordHasher.Verify("x", "", "salt"));
        Assert.False(PasswordHasher.Verify("x", "hash", ""));
    }

    [Fact]
    public void Verify_rejects_non_hex_input_without_throwing()
    {
        Assert.False(PasswordHasher.Verify("x", "not-hex!@#", "zzzz"));
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

```bash
dotnet test --filter "FullyQualifiedName~PasswordHasherTests"
```

预期：失败，`PasswordHasher` 未定义。

- [ ] **Step 3: 实现 PasswordHasher**

创建 `src/KioskWin/Core/PasswordHasher.cs`：

```csharp
using System.Security.Cryptography;
using System.Text;

namespace KioskWin.Core;

public sealed record PasswordHashResult(string Hash, string Salt);

public static class PasswordHasher
{
    private const int SaltBytes = 16;

    public static PasswordHashResult Generate(string password)
    {
        Span<byte> salt = stackalloc byte[SaltBytes];
        RandomNumberGenerator.Fill(salt);
        var saltArray = salt.ToArray();
        return new PasswordHashResult(ComputeHash(password, saltArray), Convert.ToHexString(saltArray));
    }

    public static bool Verify(string password, string expectedHashHex, string saltHex)
    {
        if (string.IsNullOrEmpty(expectedHashHex) || string.IsNullOrEmpty(saltHex))
            return false;

        byte[] salt;
        try { salt = Convert.FromHexString(saltHex); }
        catch { return false; }

        var actualHashHex = ComputeHash(password, salt);

        if (actualHashHex.Length != expectedHashHex.Length)
            return false;

        var actual = Convert.FromHexString(actualHashHex);
        var expected = Convert.FromHexString(expectedHashHex);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static string ComputeHash(string password, byte[] salt)
    {
        var pw = Encoding.UTF8.GetBytes(password);
        var buf = new byte[pw.Length + salt.Length];
        Buffer.BlockCopy(pw, 0, buf, 0, pw.Length);
        Buffer.BlockCopy(salt, 0, buf, pw.Length, salt.Length);
        return Convert.ToHexString(SHA256.HashData(buf));
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

```bash
dotnet test --filter "FullyQualifiedName~PasswordHasherTests"
```

预期：5 个测试通过。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(core): add PasswordHasher (SHA-256 + salt, constant-time compare)"
```

---

## Task 4: KeyCombinationParser（Core，TDD）

**Files:**
- Create: `src/KioskWin/Core/KeyCombinationParser.cs`
- Create: `tests/KioskWin.Tests/KeyCombinationParserTests.cs`

**Interfaces:**
- Produces: `KioskWin.Core.KeyCombinationParser`
  - `static System.Windows.Forms.Keys Parse(string text)` — 解析 `"Ctrl+Shift+Alt+Q"` 为按位或的 `Keys`
- 消费：`System.Windows.Forms.Keys`（主项目已是 WinForms）。

- [ ] **Step 1: 写失败测试**

创建 `tests/KioskWin.Tests/KeyCombinationParserTests.cs`：

```csharp
using System.Windows.Forms;
using KioskWin.Core;
using Xunit;

namespace KioskWin.Tests;

public class KeyCombinationParserTests
{
    [Fact]
    public void Parses_full_modifier_combo_with_letter()
    {
        var keys = KeyCombinationParser.Parse("Ctrl+Shift+Alt+Q");
        Assert.Equal(Keys.Control | Keys.Shift | Keys.Alt | Keys.Q, keys);
    }

    [Fact]
    public void Parses_ctrl_only_with_digit()
    {
        var keys = KeyCombinationParser.Parse("Ctrl+1");
        Assert.Equal(Keys.Control | Keys.D1, keys);
    }

    [Fact]
    public void Parses_single_letter()
    {
        Assert.Equal(Keys.Q, KeyCombinationParser.Parse("Q"));
    }

    [Fact]
    public void Empty_returns_none()
    {
        Assert.Equal(Keys.None, KeyCombinationParser.Parse(""));
        Assert.Equal(Keys.None, KeyCombinationParser.Parse("   "));
    }

    [Fact]
    public void Unknown_token_is_ignored()
    {
        var keys = KeyCombinationParser.Parse("Ctrl+Banana+Q");
        Assert.Equal(Keys.Control | Keys.Q, keys);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

```bash
dotnet test --filter "FullyQualifiedName~KeyCombinationParserTests"
```

预期：失败，`KeyCombinationParser` 未定义。

- [ ] **Step 3: 实现 KeyCombinationParser**

创建 `src/KioskWin/Core/KeyCombinationParser.cs`：

```csharp
using System.Windows.Forms;

namespace KioskWin.Core;

public static class KeyCombinationParser
{
    public static Keys Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Keys.None;

        Keys result = Keys.None;
        foreach (var raw in text.Split('+'))
        {
            var t = raw.Trim();
            if (t.Length == 0) continue;
            result |= MapToken(t);
        }
        return result;
    }

    private static Keys MapToken(string t) => t.ToLowerInvariant() switch
    {
        "ctrl" or "control" => Keys.Control,
        "shift" => Keys.Shift,
        "alt" or "menu" => Keys.Alt,
        var d when d.Length == 1 && char.IsLetterOrDigit(d[0]) => (Keys)char.ToUpper(d[0]),
        _ => Enum.TryParse(t, ignoreCase: true, out Keys k) ? k : Keys.None,
    };
}
```

- [ ] **Step 4: 运行测试确认通过**

```bash
dotnet test --filter "FullyQualifiedName~KeyCombinationParserTests"
```

预期：5 个测试通过。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(core): add KeyCombinationParser (string -> WinForms Keys)"
```

---

## Task 5: KioskConfig（Core，TDD）

**Files:**
- Create: `src/KioskWin/Core/KioskConfig.cs`
- Create: `tests/KioskWin.Tests/KioskConfigTests.cs`

**Interfaces:**
- Consumes: `Microsoft.Extensions.Configuration.IConfiguration`
- Produces: `KioskWin.Core.KioskConfig`
  - 可写属性：`Url`、`AdminKeyCombination`、`AdminPasswordHash`、`PasswordSalt`、`RetryIntervalSeconds`、`TopMost`、`ShowInTaskbar`、`UserDataFolder`
  - 计算属性：`bool IsUrlValid`、`TimeSpan RetryInterval`
  - `static KioskConfig Load(IConfiguration configuration)` — 绑定 + 默认值兜底（可测）
  - `static KioskConfig LoadFromFile(string jsonFilePath)` — 读 JSON 文件（程序用）

- [ ] **Step 1: 写失败测试**

创建 `tests/KioskWin.Tests/KioskConfigTests.cs`：

```csharp
using Microsoft.Extensions.Configuration;
using KioskWin.Core;
using Xunit;

namespace KioskWin.Tests;

public class KioskConfigTests
{
    private static IConfiguration ConfigFromMemory(params (string Key, string Value)[] pairs)
    {
        var dict = pairs.ToDictionary(p => p.Key, p => (string?)p.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public void Loads_all_fields()
    {
        var cfg = KioskConfig.Load(ConfigFromMemory(
            ("Url", "https://example.com/"),
            ("AdminKeyCombination", "Ctrl+Alt+P"),
            ("AdminPasswordHash", "ABCD"),
            ("PasswordSalt", "1234"),
            ("RetryIntervalSeconds", "30"),
            ("TopMost", "false"),
            ("ShowInTaskbar", "true"),
            ("UserDataFolder", "Data")
        ));

        Assert.Equal("https://example.com/", cfg.Url);
        Assert.Equal("Ctrl+Alt+P", cfg.AdminKeyCombination);
        Assert.Equal("ABCD", cfg.AdminPasswordHash);
        Assert.Equal("1234", cfg.PasswordSalt);
        Assert.Equal(30, cfg.RetryIntervalSeconds);
        Assert.False(cfg.TopMost);
        Assert.True(cfg.ShowInTaskbar);
        Assert.Equal("Data", cfg.UserDataFolder);
    }

    [Fact]
    public void IsUrlValid_true_for_https()
    {
        var cfg = KioskConfig.Load(ConfigFromMemory(("Url", "https://position.lmding.cn/x")));
        Assert.True(cfg.IsUrlValid);
    }

    [Fact]
    public void IsUrlValid_false_for_relative()
    {
        var cfg = KioskConfig.Load(ConfigFromMemory(("Url", "not a url")));
        Assert.False(cfg.IsUrlValid);
        // 非法 URL 保留原值，不替换
        Assert.Equal("not a url", cfg.Url);
    }

    [Fact]
    public void Defaults_when_fields_missing()
    {
        var cfg = KioskConfig.Load(ConfigFromMemory(("Url", "https://x")));
        Assert.Equal("Ctrl+Shift+Alt+Q", cfg.AdminKeyCombination);
        Assert.Equal("WebView2Data", cfg.UserDataFolder);
        Assert.Equal(10, cfg.RetryIntervalSeconds);
        Assert.True(cfg.TopMost);
        Assert.False(cfg.ShowInTaskbar);
    }

    [Theory]
    [InlineData(0, 10)]      // 非法 -> 默认 10
    [InlineData(-5, 10)]     // 非法 -> 默认 10
    [InlineData(3, 3)]
    [InlineData(99999, 600)] // 上限
    public void RetryInterval_clamped(int input, int expectedSeconds)
    {
        var cfg = KioskConfig.Load(ConfigFromMemory(
            ("Url", "https://x"),
            ("RetryIntervalSeconds", input.ToString())));
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), cfg.RetryInterval);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

```bash
dotnet test --filter "FullyQualifiedName~KioskConfigTests"
```

预期：失败，`KioskConfig` 未定义。

- [ ] **Step 3: 实现 KioskConfig**

创建 `src/KioskWin/Core/KioskConfig.cs`：

```csharp
using Microsoft.Extensions.Configuration;

namespace KioskWin.Core;

public sealed class KioskConfig
{
    public string Url { get; set; } = "";
    public string AdminKeyCombination { get; set; } = "Ctrl+Shift+Alt+Q";
    public string AdminPasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public int RetryIntervalSeconds { get; set; } = 10;
    public bool TopMost { get; set; } = true;
    public bool ShowInTaskbar { get; set; } = false;
    public string UserDataFolder { get; set; } = "WebView2Data";

    public bool IsUrlValid =>
        Uri.TryCreate(Url, UriKind.Absolute, out var u)
        && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);

    public TimeSpan RetryInterval => TimeSpan.FromSeconds(Math.Clamp(RetryIntervalSeconds, 1, 600));

    public static KioskConfig Load(IConfiguration configuration)
    {
        var cfg = new KioskConfig();
        configuration.Bind(cfg);

        // 兜底默认值：Bind 会用 JSON 值覆盖上面的默认值；这里修正非法/空值
        if (string.IsNullOrWhiteSpace(cfg.AdminKeyCombination))
            cfg.AdminKeyCombination = "Ctrl+Shift+Alt+Q";
        if (string.IsNullOrWhiteSpace(cfg.UserDataFolder))
            cfg.UserDataFolder = "WebView2Data";
        if (cfg.RetryIntervalSeconds <= 0)
            cfg.RetryIntervalSeconds = 10;
        cfg.RetryIntervalSeconds = Math.Clamp(cfg.RetryIntervalSeconds, 1, 600);

        return cfg;
    }

    public static KioskConfig LoadFromFile(string jsonFilePath)
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile(jsonFilePath, optional: true, reloadOnChange: false);
        return Load(builder.Build());
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

```bash
dotnet test --filter "FullyQualifiedName~KioskConfigTests"
```

预期：全部通过。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(core): add KioskConfig with validation and safe defaults"
```

---

## Task 6: RetryController + IRetryScheduler（Core，TDD）

**Files:**
- Create: `src/KioskWin/Core/RetryController.cs`
- Create: `src/KioskWin/Core/FormsRetryScheduler.cs`
- Create: `tests/KioskWin.Tests/RetryControllerTests.cs`

**Interfaces:**
- Produces:
  - `interface IRetryScheduler { bool IsRunning { get; } void Start(TimeSpan interval, Action onTick); void Stop(); }`
  - `enum RetryState { Idle, Retrying }`
  - `sealed class RetryController`：ctor `(TimeSpan interval, IRetryScheduler scheduler)`；`RetryState State { get; }`；`event Action? ShouldRetry`；`void ReportFailure()`；`void ReportSuccess()`
  - `sealed class FormsRetryScheduler : IRetryScheduler`（基于 `System.Windows.Forms.Timer`，UI 线程触发）
- 行为：`ReportFailure` 从 Idle 进入 Retrying 并启动调度器；每次 tick 触发 `ShouldRetry`；`ReportSuccess` 停调度器并回 Idle；重复 `ReportFailure` 不会重复启动。

> 说明：规格散文提到 `Idle/Waiting/Retrying` 三态，但 `Waiting` 不携带任何可测的独立行为，故按 YAGNI 合并为 `Idle/Retrying` 两态；状态机行为由 Task 6 的测试完整覆盖。

- [ ] **Step 1: 写失败测试**

创建 `tests/KioskWin.Tests/RetryControllerTests.cs`：

```csharp
using KioskWin.Core;
using Xunit;

namespace KioskWin.Tests;

public class RetryControllerTests
{
    private sealed class FakeScheduler : IRetryScheduler
    {
        public bool IsRunning { get; private set; }
        public TimeSpan LastInterval { get; private set; }
        private Action? _onTick;

        public void Start(TimeSpan interval, Action onTick)
        {
            if (IsRunning) return;
            IsRunning = true;
            LastInterval = interval;
            _onTick = onTick;
        }

        public void Stop()
        {
            IsRunning = false;
            _onTick = null;
        }

        public void FireTick() => _onTick?.Invoke();
    }

    [Fact]
    public void ReportFailure_starts_retrying()
    {
        var sched = new FakeScheduler();
        var ctrl = new RetryController(TimeSpan.FromSeconds(15), sched);

        ctrl.ReportFailure();

        Assert.Equal(RetryState.Retrying, ctrl.State);
        Assert.True(sched.IsRunning);
        Assert.Equal(TimeSpan.FromSeconds(15), sched.LastInterval);
    }

    [Fact]
    public void Tick_raises_ShouldRetry()
    {
        var sched = new FakeScheduler();
        var ctrl = new RetryController(TimeSpan.FromSeconds(1), sched);
        var fired = 0;
        ctrl.ShouldRetry += () => fired++;

        ctrl.ReportFailure();
        sched.FireTick();
        sched.FireTick();

        Assert.Equal(2, fired);
    }

    [Fact]
    public void ReportSuccess_stops_and_returns_to_idle()
    {
        var sched = new FakeScheduler();
        var ctrl = new RetryController(TimeSpan.FromSeconds(1), sched);

        ctrl.ReportFailure();
        ctrl.ReportSuccess();

        Assert.Equal(RetryState.Idle, ctrl.State);
        Assert.False(sched.IsRunning);
    }

    [Fact]
    public void Double_failure_does_not_restart_or_change_interval()
    {
        var sched = new FakeScheduler();
        var ctrl = new RetryController(TimeSpan.FromSeconds(7), sched);

        ctrl.ReportFailure();
        ctrl.ReportFailure(); // 再次失败应被忽略

        Assert.Equal(RetryState.Retrying, ctrl.State);
        Assert.Equal(TimeSpan.FromSeconds(7), sched.LastInterval);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

```bash
dotnet test --filter "FullyQualifiedName~RetryControllerTests"
```

预期：失败，`RetryController` / `IRetryScheduler` 未定义。

- [ ] **Step 3: 实现 RetryController + IRetryScheduler**

创建 `src/KioskWin/Core/RetryController.cs`：

```csharp
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
```

- [ ] **Step 4: 实现生产调度器 FormsRetryScheduler**

创建 `src/KioskWin/Core/FormsRetryScheduler.cs`：

```csharp
using System.Windows.Forms;

namespace KioskWin.Core;

public sealed class FormsRetryScheduler : IRetryScheduler
{
    private readonly Timer _timer = new() { Enabled = false };
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
```

- [ ] **Step 5: 运行全部测试确认通过**

```bash
dotnet test
```

预期：所有 Core 测试通过（FileLogger / PasswordHasher / KeyCombinationParser / KioskConfig / RetryController）。

- [ ] **Step 6: 提交**

```bash
git add -A
git commit -m "feat(core): add RetryController state machine and FormsRetryScheduler"
```

---

## Task 7: RetryOverlay（Forms，UI）

**Files:**
- Create: `src/KioskWin/Forms/RetryOverlay.cs`

**Interfaces:**
- Produces: `KioskWin.Forms.RetryOverlay`（继承 `UserControl`，`Dock = Fill`）
  - `void ShowRetry()` / `void ShowConfigError()` / `void ShowRuntimeMissing()` / `void ShowGenericError(string? detail = null)`
- 消费：`System.Windows.Forms`、`System.Drawing`。

> 说明：规格第 6 节指出 `RetryOverlay` 是**唯一**全屏遮罩组件，通过不同方法切换文案。本控件用代码构造（无 `.Designer.cs`）。

- [ ] **Step 1: 实现 RetryOverlay**

创建 `src/KioskWin/Forms/RetryOverlay.cs`：

```csharp
using System.Drawing;
using System.Windows.Forms;

namespace KioskWin.Forms;

public sealed class RetryOverlay : UserControl
{
    private readonly Label _label;

    public RetryOverlay()
    {
        BackColor = Color.FromArgb(30, 30, 30);
        Dock = DockStyle.Fill;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
        };

        _label = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
        };

        table.Controls.Add(_label, 0, 0);
        Controls.Add(table);
    }

    private void SetMessage(string title, string detail)
        => _label.Text = string.IsNullOrEmpty(detail) ? title : $"{title}\n\n{detail}";

    public void ShowRetry() => SetMessage("正在重试…", "网络连接失败，正在自动重试");
    public void ShowConfigError() => SetMessage("配置错误", "URL 配置非法，请联系管理员");
    public void ShowRuntimeMissing() => SetMessage("缺少 WebView2 运行时", "未检测到 WebView2 运行时，请联系管理员安装");
    public void ShowGenericError(string? detail = null) => SetMessage("程序出错", detail ?? "发生未知错误，请查看日志");
}
```

- [ ] **Step 2: 构建确认编译通过**

```bash
dotnet build src/KioskWin/KioskWin.csproj
```

预期：0 错误。

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "feat(forms): add reusable RetryOverlay control"
```

---

## Task 8: AdminDialog（Forms，UI）

**Files:**
- Create: `src/KioskWin/Forms/AdminDialog.cs`

**Interfaces:**
- Consumes: `KioskWin.Core.KioskConfig`、`KioskWin.Core.PasswordHasher`
- Produces:
  - `enum KioskWin.Forms.AdminAction { None, Exit, Reload, DevTools, Unlock }`
  - `sealed class AdminDialog : Form`：ctor `(KioskConfig config, Action<string> logFailure)`；属性 `AdminAction Result`
- 行为：密码框输入 → 点"验证"按钮 → `PasswordHasher.Verify` 比对 `config.AdminPasswordHash`/`config.PasswordSalt`；通过则启用 4 个工具按钮（退出/重载/DevTools/解除锁定）；失败则提示 + 清空 + 写日志。

- [ ] **Step 1: 实现 AdminDialog**

创建 `src/KioskWin/Forms/AdminDialog.cs`：

```csharp
using System.Drawing;
using System.Windows.Forms;
using KioskWin.Core;

namespace KioskWin.Forms;

public enum AdminAction
{
    None,
    Exit,
    Reload,
    DevTools,
    Unlock,
}

public sealed class AdminDialog : Form
{
    private readonly KioskConfig _config;
    private readonly Action<string> _logFailure;

    private readonly TextBox _passwordBox;
    private readonly Button _verifyButton;
    private readonly Button _exitButton;
    private readonly Button _reloadButton;
    private readonly Button _devToolsButton;
    private readonly Button _unlockButton;

    public AdminAction Result { get; private set; } = AdminAction.None;

    public AdminDialog(KioskConfig config, Action<string> logFailure)
    {
        _config = config;
        _logFailure = logFailure;

        Text = "KioskWin 管理员";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(360, 280);

        var lbl = new Label
        {
            Text = "管理员密码：",
            Location = new Point(20, 25),
            AutoSize = true,
        };

        _passwordBox = new TextBox
        {
            Location = new Point(20, 50),
            Size = new Size(320, 25),
            PasswordChar = '*',
        };
        _passwordBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { VerifyPassword(); e.SuppressKeyPress = true; }
        };

        _verifyButton = new Button { Text = "验证", Location = new Point(250, 85), Size = new Size(90, 30) };
        _verifyButton.Click += (_, _) => VerifyPassword();

        _exitButton = MakeActionButton("退出程序", 20, AdminAction.Exit);
        _reloadButton = MakeActionButton("重载页面", 110, AdminAction.Reload);
        _devToolsButton = MakeActionButton("打开 DevTools", 200, AdminAction.DevTools);
        _unlockButton = MakeActionButton("解除锁定", 20, AdminAction.Unlock);

        var unlockLabel = new Label
        {
            Text = "（解除锁定：临时变为可移动的普通窗口，便于维护）",
            Location = new Point(20, 195),
            Size = new Size(320, 60),
            ForeColor = Color.Gray,
        };

        Controls.AddRange(new Control[]
        {
            lbl, _passwordBox, _verifyButton,
            _exitButton, _reloadButton, _devToolsButton, _unlockButton, unlockLabel,
        });

        SetActionButtonsEnabled(false);
        AcceptButton = _verifyButton;
    }

    private Button MakeActionButton(string text, int x, AdminAction action)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, 150),
            Size = new Size(110, 35),
            Enabled = false,
        };
        btn.Click += (_, _) =>
        {
            Result = action;
            DialogResult = DialogResult.OK;
            Close();
        };
        return btn;
    }

    private void SetActionButtonsEnabled(bool enabled)
    {
        _exitButton.Enabled = enabled;
        _reloadButton.Enabled = enabled;
        _devToolsButton.Enabled = enabled;
        _unlockButton.Enabled = enabled;
    }

    private void VerifyPassword()
    {
        if (PasswordHasher.Verify(_passwordBox.Text, _config.AdminPasswordHash, _config.PasswordSalt))
        {
            SetActionButtonsEnabled(true);
            _passwordBox.Enabled = false;
            _verifyButton.Enabled = false;
            _ = MessageBox.Show(this, "验证通过，请选择操作。", "KioskWin",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            _logFailure("admin auth failed");
            MessageBox.Show(this, "密码错误", "KioskWin",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _passwordBox.Clear();
        }
    }
}
```

- [ ] **Step 2: 构建确认编译通过**

```bash
dotnet build src/KioskWin/KioskWin.csproj
```

预期：0 错误。

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "feat(forms): add AdminDialog with password gate and tool actions"
```

---

## Task 9: MainForm（Forms，集成）

**Files:**
- Create: `src/KioskWin/Forms/MainForm.cs`

**Interfaces:**
- Consumes: `KioskConfig`、`FileLogger`、`KeyCombinationParser`、`RetryController`、`FormsRetryScheduler`、`RetryOverlay`、`AdminDialog`/`AdminAction`、`Microsoft.Web.WebView2.WinForms.WebView2`、`CoreWebView2Environment`。
- Produces: `KioskWin.Forms.MainForm`，ctor `(KioskConfig config, FileLogger logger)`。`Program.cs`（Task 10）会 `Application.Run(new MainForm(config, logger))`。

> 覆盖需求 F1、F3、F4、F5、F7、F8 的窗口侧。本任务无单元测试（Win32/WebView2 行为），用手动 QA 清单（Task 11）覆盖。

- [ ] **Step 1: 实现 MainForm**

创建 `src/KioskWin/Forms/MainForm.cs`：

```csharp
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using KioskWin.Core;
using System.Drawing;
using System.Windows.Forms;
using KioskWin.Forms;

namespace KioskWin.Forms;

public sealed class MainForm : Form
{
    private KioskConfig _config;
    private readonly FileLogger _logger;
    private Keys _adminCombo;

    private readonly WebView2 _webView;
    private readonly RetryOverlay _overlay;
    private readonly RetryController _retry;
    private readonly FormsRetryScheduler _scheduler;

    private bool _authorizedClose;
    private bool _devToolsEnabled;

    public MainForm(KioskConfig config, FileLogger logger)
    {
        _config = config;
        _logger = logger;
        _adminCombo = KeyCombinationParser.Parse(config.AdminKeyCombination);

        // F3 无边框全屏 + 无最小化/关闭按钮
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;

        // F5 防切到后台
        TopMost = config.TopMost;
        ShowInTaskbar = config.ShowInTaskbar;
        KeyPreview = true;
        BackColor = Color.Black;

        _webView = new WebView2 { Dock = DockStyle.Fill };
        _overlay = new RetryOverlay { Visible = false };

        Controls.Add(_webView);
        Controls.Add(_overlay);
        _overlay.BringToFront();

        _scheduler = new FormsRetryScheduler();
        _retry = new RetryController(config.RetryInterval, _scheduler);
        _retry.ShouldRetry += OnRetry;

        Load += OnLoad;
        FormClosing += OnFormClosing;   // F4
        Deactivate += OnDeactivate;     // F5
    }

    // ---- WebView2 初始化与导航 ----

    private async void OnLoad(object? sender, EventArgs e)
    {
        try
        {
            var userDataFolder = Path.Combine(AppContext.BaseDirectory, _config.UserDataFolder);
            var env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder);
            await _webView.EnsureCoreWebView2Async(env);

            ConfigureWebView();

            _webView.CoreWebView2.ProcessFailed += OnProcessFailed;
            _webView.NavigationCompleted += OnNavigationCompleted;

            _logger.Log($"[startup] navigating to {_config.Url}");
            NavigateOrShowConfigError();
        }
        catch (Exception ex)
        {
            // 未检测到 WebView2 运行时等情况
            _logger.Log($"[webview2 init] {ex}");
            _overlay.ShowRuntimeMissing();
            _overlay.Visible = true;
            _overlay.BringToFront();
        }
    }

    private void ConfigureWebView()
    {
        var s = _webView.CoreWebView2!.Settings;
        s.AreDevToolsEnabled = false;
        s.AreBrowserAcceleratorKeysEnabled = false;
        s.IsStatusBarEnabled = false;
        s.IsZoomControlEnabled = false;
        s.IsBuiltInErrorPageEnabled = false;
        s.IsGeneralAutofillEnabled = false;
    }

    private void NavigateOrShowConfigError()
    {
        if (_webView.CoreWebView2 == null) return;

        if (_config.IsUrlValid)
        {
            _webView.CoreWebView2.Navigate(_config.Url);
        }
        else
        {
            _logger.Log($"[config] invalid Url: '{_config.Url}'");
            _overlay.ShowConfigError();
            _overlay.Visible = true;
            _overlay.BringToFront();
        }
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            _retry.ReportSuccess();
            _overlay.Visible = false;
            _logger.Log("[navigation] success");
        }
        else
        {
            _logger.Log("[navigation] failed -> retry");
            _overlay.ShowRetry();
            _overlay.Visible = true;
            _overlay.BringToFront();
            _retry.ReportFailure();
        }
    }

    private void OnRetry()
    {
        if (IsDisposed) return;
        if (_config.IsUrlValid && _webView.CoreWebView2 != null)
        {
            BeginInvoke(() => _webView.CoreWebView2.Navigate(_config.Url));
        }
    }

    private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        _logger.Log($"[webview2] process failed: {e.ProcessFailedKind}");
        _overlay.ShowGenericError("网页渲染进程崩溃，正在重试…");
        _overlay.Visible = true;
        _overlay.BringToFront();
        _retry.ReportFailure();
        _ = ReinitAsync();
    }

    private async Task ReinitAsync()
    {
        try
        {
            await Task.Delay(2000);
            if (_webView.CoreWebView2 == null)
                await _webView.EnsureCoreWebView2Async();
            NavigateOrShowConfigError();
        }
        catch (Exception ex)
        {
            _logger.Log($"[reinit] {ex}");
        }
    }

    // ---- 锁定行为 ----

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        // F4：拦截用户触发的关闭（Alt+F4 / 关闭按钮），除非管理员授权
        if (!_authorizedClose && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            _logger.Log("[close] intercepted user close");
        }
    }

    private void OnDeactivate(object? sender, EventArgs e)
    {
        // F5：失焦后抢回前台
        if (TopMost)
            BeginInvoke(() => Activate());
    }

    // ---- 管理员逃生通道（F8）----

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_adminCombo != Keys.None && keyData == _adminCombo)
        {
            ShowAdminDialog();
            return true; // 吞掉按键
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ShowAdminDialog()
    {
        using var dlg = new AdminDialog(_config, message => _logger.Log(message));
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        switch (dlg.Result)
        {
            case AdminAction.Exit:
                _logger.Log("[admin] exit requested");
                _authorizedClose = true;
                Application.Exit();
                break;
            case AdminAction.Reload:
                _logger.Log("[admin] reload requested");
                ReloadConfigAndNavigate();
                break;
            case AdminAction.DevTools:
                _logger.Log("[admin] toggle devtools");
                ToggleDevTools();
                break;
            case AdminAction.Unlock:
                _logger.Log("[admin] unlock mode");
                EnterUnlockedMode();
                break;
        }
    }

    private void ReloadConfigAndNavigate()
    {
        _config = KioskConfig.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
        _adminCombo = KeyCombinationParser.Parse(_config.AdminKeyCombination);
        _logger.Log($"[reload] url={_config.Url}");
        NavigateOrShowConfigError();
    }

    private void ToggleDevTools()
    {
        if (_webView.CoreWebView2 == null) return;
        _devToolsEnabled = !_devToolsEnabled;
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = _devToolsEnabled;
        if (_devToolsEnabled)
            _webView.CoreWebView2.OpenDevToolsWindow();
    }

    private void EnterUnlockedMode()
    {
        TopMost = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        WindowState = FormWindowState.Normal;
        Bounds = new Rectangle(50, 50, 1024, 768);
    }
}
```

- [ ] **Step 2: 构建 + 跑测试（确保未破坏 Core 测试）**

```bash
dotnet build KioskWin.sln
dotnet test
```

预期：构建 0 错误；Core 测试仍全部通过（MainForm 无单测）。

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "feat(forms): add MainForm (fullscreen, lockdown, retry, admin entry)"
```

---

## Task 10: Program.cs 最终入口（Forms）

**Files:**
- Modify: `src/KioskWin/Program.cs`（替换 Task 1 的占位）

**Interfaces:**
- Consumes: `KioskConfig.LoadFromFile`、`FileLogger`、`Forms.MainForm`。
- Produces: 完整程序入口，覆盖 F8 的 `--hash-password` CLI、F9 单实例、N3 异常兜底。

> 覆盖需求 F2（配置）、F8（生成哈希 CLI）、F9（单实例）、N3（不静默崩溃）。

- [ ] **Step 1: 替换 Program.cs 为最终版本**

将 `src/KioskWin/Program.cs` 全文替换为：

```csharp
using System.Diagnostics;
using System.Runtime.InteropServices;
using KioskWin.Core;
using KioskWin.Forms;

namespace KioskWin;

internal static class Program
{
    private const string MutexName = "Global\\KioskWin_SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        // F8：CLI 模式生成密码哈希（不启动 UI）
        if (TryHandleHashPassword(args))
            return;

        ApplicationConfiguration.Initialize();

        // F9：单实例——已有实例则尝试置前并退出
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            BringExistingToFront();
            return;
        }

        // N3：全局异常兜底，避免静默崩溃
        Application.ThreadException += (_, e) => LogException(e.Exception, "UI thread exception");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogException(e.ExceptionObject as Exception, "AppDomain unhandled");

        try
        {
            var logger = new FileLogger();
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var config = KioskConfig.LoadFromFile(configPath);
            logger.Log($"[startup] url={config.Url} topMost={config.TopMost}");
            Application.Run(new MainForm(config, logger));
        }
        catch (Exception ex)
        {
            LogException(ex, "startup");
            MessageBox.Show(
                "程序启动失败，请查看日志：\n" + ex.Message,
                "KioskWin",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static bool TryHandleHashPassword(string[] args)
    {
        if (args.Length == 2 &&
            string.Equals(args[0], "--hash-password", StringComparison.OrdinalIgnoreCase))
        {
            var result = PasswordHasher.Generate(args[1]);
            Console.WriteLine("把下面两行填入 appsettings.json：");
            Console.WriteLine("  \"AdminPasswordHash\": \"" + result.Hash + "\",");
            Console.WriteLine("  \"PasswordSalt\": \"" + result.Salt + "\"");
            return true;
        }
        return false;
    }

    private static void BringExistingToFront()
    {
        try
        {
            var current = Process.GetCurrentProcess();
            foreach (var p in Process.GetProcessesByName(current.ProcessName))
            {
                if (p.Id == current.Id || p.MainWindowHandle == IntPtr.Zero) continue;
                SetForegroundWindow(p.MainWindowHandle);
                ShowWindow(p.MainWindowHandle, SW_RESTORE);
            }
        }
        catch
        {
            // best-effort，置前失败不影响单实例退出逻辑
        }
    }

    private static void LogException(Exception? ex, string source)
    {
        try
        {
            new FileLogger().Log($"[{source}] {ex}");
        }
        catch
        {
            // 日志本身失败不要再抛，避免二次异常
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;
}
```

- [ ] **Step 2: 构建**

```bash
dotnet build KioskWin.sln
```

预期：0 错误。

- [ ] **Step 3: 手动验证 CLI 模式**

```bash
dotnet run --project src/KioskWin/KioskWin.csproj -- --hash-password mysecret
```

预期：打印两行 `AdminPasswordHash` / `PasswordSalt`，程序不弹窗、立即退出。

- [ ] **Step 4: 提交**

```bash
git add -A
git commit -m "feat: wire Program entry (hash-password CLI, single-instance, exception guards)"
```

---

## Task 11: 开机自启脚本 + 发布 + 手动 QA

**Files:**
- Create: `src/KioskWin/install-shortcut.ps1`
- (运行) 发布产物 + 手动 QA 清单

**Interfaces:**
- Produces: `install-shortcut.ps1`（在用户「启动」文件夹创建快捷方式）；发布命令；手动 QA 通过。

> 覆盖需求 F6（开机自启）、N1（自包含单文件）。这是部署与验收任务。

- [ ] **Step 1: 写 install-shortcut.ps1**

创建 `src/KioskWin/install-shortcut.ps1`：

```powershell
# 在当前用户的"启动"文件夹创建 KioskWin 开机自启快捷方式
# 用法：在发布产物目录（含 KioskWin.exe）下运行 powershell -ExecutionPolicy Bypass -File .\install-shortcut.ps1
$ErrorActionPreference = 'Stop'

$exe = Join-Path $PSScriptRoot 'KioskWin.exe'
if (-not (Test-Path $exe)) {
    throw "未找到 $exe，请在发布产物目录运行此脚本。"
}

$startup = [Environment]::GetFolderPath('Startup')
$shortcutPath = Join-Path $startup 'KioskWin.lnk'

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $PSScriptRoot
$shortcut.WindowStyle = 3   # 最大化
$shortcut.Description = 'KioskWin'
$shortcut.Save()

Write-Host "已创建开机自启快捷方式：$shortcutPath"
```

> 注意：发布后该脚本应与 `KioskWin.exe` 同目录。`dotnet publish` 默认不会把 `.ps1` 复制到输出。下一步用发布参数复制它。

- [ ] **Step 2: 让 csproj 把脚本复制到发布输出**

在 `src/KioskWin/KioskWin.csproj` 的 `</Project>` 之前插入：

```xml
  <ItemGroup>
    <None Update="install-shortcut.ps1">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
```

- [ ] **Step 3: 发布为自包含单文件**

```bash
dotnet publish src/KioskWin/KioskWin.csproj `
  -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -o publish
```

（Linux/macOS shell 去掉反引号换行，写成一行的 `dotnet publish ... -o publish`。）

预期：`publish/` 下生成 `KioskWin.exe`（单文件）+ `appsettings.json` + `install-shortcut.ps1`。

> **验证 WebView2 单文件兼容**：把 `publish\` 拷到一台干净的 Win10/11 机器（已装 Edge/WebView2 运行时）双击 `KioskWin.exe`，确认能正常初始化 WebView2（出现网页或重试遮罩，而非崩溃）。若单文件模式下 WebView2 无法初始化（`WebView2Loader.dll` 解压问题），退路：去掉 `-p:PublishSingleFile=true` 重新发布（仍 `--self-contained`，多文件目录部署），并相应把 `install-shortcut.ps1` 指向 `KioskWin.exe`。

- [ ] **Step 4: 配置管理员密码（关键，否则无法维护退出）**

在发布目录运行：

```bash
./KioskWin.exe --hash-password 你的密码
```

把输出的两行 `AdminPasswordHash` / `PasswordSalt` 填入 `appsettings.json`，保存。

- [ ] **Step 5: 在真实工控机或 Win10/11 虚拟机过手动 QA 清单**

在 `publish\` 目录双击 `KioskWin.exe`（或运行 `install-shortcut.ps1` 后重启），逐项核对：

- [ ] 全屏覆盖主屏，无标题栏与最小化/关闭按钮
- [ ] 按 Alt+F4 无效，窗口不关闭
- [ ] 任务栏无条目
- [ ] 窗口始终置顶；Alt+Tab 切换后本窗口仍可见在最前
- [ ] 拔网线/断网 → 出现"正在重试…"遮罩；恢复网络 → 自动回到网页
- [ ] 按 Ctrl+Shift+Alt+Q 弹出密码框
- [ ] 错误密码不通过、提示并清空；正确密码后出现 4 个工具按钮
- [ ] 点"退出程序"可正常退出（之后 Alt+F4 仍被拦，符合预期）
- [ ] 点"重载页面"会热重读 `appsettings.json`（改 URL 后生效）
- [ ] 点"打开 DevTools"弹出 DevTools 窗口
- [ ] 点"解除锁定"窗口变为可移动普通窗口
- [ ] 开机后自动启动（运行 `install-shortcut.ps1` 后重启验证）
- [ ] 程序运行时再次双击 exe，只激活已有窗口（不新开）

- [ ] **Step 6: 提交**

```bash
git add -A
git commit -m "feat: add install-shortcut.ps1 and self-contained publish config"
```

---

## 完成标志

- [ ] 所有任务 commit 完成；`dotnet test` 全绿（Core 单测）。
- [ ] `dotnet publish` 产物可独立运行（无需预装 .NET）。
- [ ] 手动 QA 清单全部勾选。
- [ ] `appsettings.json` 已填入真实密码哈希（非空）。
