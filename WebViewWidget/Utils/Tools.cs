using System.Reflection;

namespace WebViewWidget.Utils;

public static class Tools {
    public static Image LoadEmbeddedImage(string relativePath) {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream($"WebViewWidget.Assets.{relativePath}");
        return stream != null ? Image.FromStream(stream) : new Bitmap(1, 1);
    }
}