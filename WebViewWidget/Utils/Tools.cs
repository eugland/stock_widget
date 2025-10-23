using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WebViewWidget.Utils;

public class Tools
{
    public static Image EmojiToImage(string emoji, int sizePx=20)
    {
        using var bmp = new Bitmap(sizePx, sizePx);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        using var font = new Font("Segoe UI Emoji", sizePx - 2, FontStyle.Regular, GraphicsUnit.Pixel);
        var textSize = TextRenderer.MeasureText(emoji, font);
        // center the glyph in the square canvas
        var x = (sizePx - textSize.Width) / 2f;
        var y = (sizePx - textSize.Height) / 2f;
        g.DrawString(emoji, font, Brushes.Black, x, y);

        return (Image)bmp.Clone(); // return a real Image, not disposed
    }

    public static Image LoadEmbeddedImage(string relativePath)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream($"WebViewWidget.Assets.{relativePath}");
        return Image.FromStream(stream);
    }
}
