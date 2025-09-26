using System;
using System.Drawing;
using System.Threading.Tasks;

namespace OfdrwNet.Reader.Model
{
    public abstract class ContentObject : RenderObject
    {
        public System.Drawing.Drawing2D.Matrix? CTM { get; set; }
        public bool IsCacheValid { get; set; }
        public string? ResourceId { get; set; }

        public abstract Task<bool> RenderAsync(Graphics graphics, RenderContext context);

        public virtual void Dispose()
        {
            CTM?.Dispose();
        }
    }

    public class TextObject : ContentObject
    {
        public string Text { get; set; } = string.Empty;
        public float FontSize { get; set; } = 12.0f;
        public FontInfo? Font { get; set; }
        public ColorInfo? Color { get; set; }
        public TextLayout? Layout { get; set; }

        public override async Task<bool> RenderAsync(Graphics graphics, RenderContext context)
        {
            await Task.CompletedTask;
            return true;
        }
    }

    public class ImageObject : ContentObject
    {
        public byte[]? ImageData { get; set; }

        public override async Task<bool> RenderAsync(Graphics graphics, RenderContext context)
        {
            await Task.CompletedTask;
            return true;
        }
    }

    public class ContentVectorObject : ContentObject
    {
        public string? PathData { get; set; }

        public override async Task<bool> RenderAsync(Graphics graphics, RenderContext context)
        {
            await Task.CompletedTask;
            return true;
        }
    }

    /// <summary>
    /// 字体信息类
    /// </summary>
    public class FontInfo
    {
        public string? Name { get; set; }
        public float Size { get; set; }
        public FontStyle Style { get; set; } = FontStyle.Regular;
    }

    /// <summary>
    /// 颜色信息类
    /// </summary>
    public class ColorInfo
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; } = 255;

        public Color ToColor() => Color.FromArgb(A, R, G, B);
        public Color ToSystemColor() => ToColor();  // 别名方法
    }

    /// <summary>
    /// 文本布局信息
    /// </summary>
    public class TextLayout
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float LineHeight { get; set; } = 1.0f;
        public StringAlignment Alignment { get; set; } = StringAlignment.Near;
        public float LetterSpacing { get; set; } = 0.0f;
        public float WordSpacing { get; set; } = 0.0f;
        public StringFormat? StringFormat { get; set; }
        public float BaselineOffset { get; set; } = 0f;
    }
}
