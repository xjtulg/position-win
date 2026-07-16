namespace PositionKiosk.Core;

public static class WebViewAutoFitScript
{
    public static string Create() =>
        """
        (() => {
          if (!document.documentElement || !document.body) return;

          document.documentElement.style.overflow = '';
          document.body.style.overflow = '';
          document.body.style.transform = '';
          document.body.style.transformOrigin = '';
          document.body.style.width = '';
          document.body.style.height = '';

          const contentWidth = Math.max(
            document.documentElement.scrollWidth,
            document.documentElement.offsetWidth,
            document.body.scrollWidth,
            document.body.offsetWidth,
            1);
          const contentHeight = Math.max(
            document.documentElement.scrollHeight,
            document.documentElement.offsetHeight,
            document.body.scrollHeight,
            document.body.offsetHeight,
            1);

          const scale = Math.min(window.innerWidth / contentWidth, window.innerHeight / contentHeight, 1);
          document.documentElement.style.overflow = 'hidden';
          document.body.style.overflow = 'hidden';
          document.body.style.transformOrigin = 'top left';
          document.body.style.width = `${100 / scale}vw`;
          document.body.style.height = `${100 / scale}vh`;
          document.body.style.transform = `scale(${scale})`;
        })();
        """;
}
