using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using PositionKiosk.Core;
using System.Drawing;
using System.Windows.Forms;
using PositionKiosk.Forms;

namespace PositionKiosk.Forms;

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
    private bool _showingAdminDialog;

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
            if (IsDisposed) return;
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
        // F5：失焦后抢回前台（但在显示管理对话框时不抢）
        if (TopMost && !_showingAdminDialog)
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
        _showingAdminDialog = true;
        try
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
        finally
        {
            _showingAdminDialog = false;
        }
    }

    private void ReloadConfigAndNavigate()
    {
        KioskConfig reloaded;
        try
        {
            reloaded = KioskConfig.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
        }
        catch (Exception ex)
        {
            _logger.Log($"[reload] failed to read config, keeping previous: {ex}");
            return;
        }
        _config = reloaded;
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
