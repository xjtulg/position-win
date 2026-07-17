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
