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
            ("UserDataFolder", "Data"),
            ("AutoFitToWindow", "false")
        ));

        Assert.Equal("https://example.com/", cfg.Url);
        Assert.Equal("Ctrl+Alt+P", cfg.AdminKeyCombination);
        Assert.Equal("ABCD", cfg.AdminPasswordHash);
        Assert.Equal("1234", cfg.PasswordSalt);
        Assert.Equal(30, cfg.RetryIntervalSeconds);
        Assert.False(cfg.TopMost);
        Assert.True(cfg.ShowInTaskbar);
        Assert.Equal("Data", cfg.UserDataFolder);
        Assert.False(cfg.AutoFitToWindow);
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
        Assert.True(cfg.AutoFitToWindow);
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
