using KioskWin.Core;
using Xunit;

namespace KioskWin.Tests;

public class WebViewAutoFitScriptTests
{
    [Fact]
    public void Script_hides_scrollbars_and_scales_body_to_viewport()
    {
        var script = WebViewAutoFitScript.Create();

        Assert.Contains("overflow = 'hidden'", script);
        Assert.Contains("Math.min(window.innerWidth / contentWidth, window.innerHeight / contentHeight, 1)", script);
        Assert.Contains("document.body.style.transform = `scale(${scale})`", script);
        Assert.Contains("document.body.style.transformOrigin = 'top left'", script);
    }
}
