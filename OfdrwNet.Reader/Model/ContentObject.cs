using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;

namespace OfdrwNet.Reader.Model
{
    /// <summary>
    /// 抽象的可渲染内容对象，提供通用的 CTM、资源及缓存标记。
    /// </summary>
    public abstract class ContentObject : RenderObject
    {
        /// <summary>
        /// 将在渲染阶段乘到 Graphics 上的当前 CTM（若已被内联融合，则可能为 null）。
        /// </summary>
        public System.Drawing.Drawing2D.Matrix? CTM { get; set; }
    /// <summary>
    /// 原始 XML 中解析出的 CTM（未做任何内部判定或修改）。
    /// 若 <see cref="CTM"/> 被置空以避免重复应用，这里仍保留原矩阵供日志 / 调试。
    /// </summary>
    public Matrix? OriginalCTM { get; set; }
    /// <summary>
    /// 标记该 CTM 仅用于将内部文本坐标(例如 pt) 转换为边界坐标系(mm)，
    /// 不应在渲染阶段再次乘到 Graphics 上，否则会造成双重平移 / 缩放。
    /// </summary>
    public bool CtmIsInternalGlyph { get; set; }
        /// <summary>
        /// 指示缓存是否有效（预留）。
        /// </summary>
        public bool IsCacheValid { get; set; }
        /// <summary>
        /// 资源引用 ID（例如字体或图像资源）。
        /// </summary>
        public string? ResourceId { get; set; }

    /// <summary>
    /// 异步渲染。
    /// </summary>
    public abstract Task<bool> RenderAsync(Graphics graphics, RenderContext context);

        /// <summary>
        /// 释放矩阵资源。
        /// </summary>
        public virtual void Dispose()
        {
            CTM?.Dispose();
            OriginalCTM?.Dispose();
        }
    }

    /// <summary>
    /// 文本对象（简化模型）。
    /// </summary>
    public class TextObject : ContentObject
    {
        /// <summary>纯文本内容。</summary>
        public string Text { get; set; } = string.Empty;
        /// <summary>字号（像素或 mm 取决于统一模式）。</summary>
        public float FontSize { get; set; } = 12.0f;
        /// <summary>字体信息。</summary>
        public FontInfo? Font { get; set; }
        /// <summary>颜色。</summary>
        public ColorInfo? Color { get; set; }
        /// <summary>布局信息。</summary>
        public TextLayout? Layout { get; set; }

        public override async Task<bool> RenderAsync(Graphics graphics, RenderContext context)
        {
            await Task.CompletedTask;
            return true;
        }
    }

    /// <summary>
    /// 图像对象（占位实现）。
    /// </summary>
    public class ImageObject : ContentObject
    {
        /// <summary>图像数据（尚未实现加载）。</summary>
        public byte[]? ImageData { get; set; }

        public override async Task<bool> RenderAsync(Graphics graphics, RenderContext context)
        {
            await Task.CompletedTask;
            return true;
        }
    }

    /// <summary>
    /// 矢量对象（占位）。
    /// </summary>
    public class ContentVectorObject : ContentObject
    {
        /// <summary>路径数据。</summary>
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
        /// <summary>字体名称。</summary>
        public string? Name { get; set; }
        /// <summary>字号（与统一模式相关，可能是 mm 或像素）。</summary>
        public float Size { get; set; }
        /// <summary>字体样式。</summary>
        public FontStyle Style { get; set; } = FontStyle.Regular;
    }

    /// <summary>
    /// 颜色信息类
    /// </summary>
    public class ColorInfo
    {
        /// <summary>红色分量。</summary>
        public byte R { get; set; }
        /// <summary>绿色分量。</summary>
        public byte G { get; set; }
        /// <summary>蓝色分量。</summary>
        public byte B { get; set; }
        /// <summary>透明度分量。</summary>
        public byte A { get; set; } = 255;

        /// <summary>转换为 System.Drawing.Color。</summary>
        public Color ToColor() => Color.FromArgb(A, R, G, B);
        /// <summary>同 <see cref="ToColor"/>。</summary>
        public Color ToSystemColor() => ToColor();  // 别名方法
    }

    /// <summary>
    /// 文本布局信息
    /// </summary>
    public class TextLayout
    {
        /// <summary>布局 X 坐标。</summary>
        public float X { get; set; }
        /// <summary>布局 Y 坐标。</summary>
        public float Y { get; set; }
        /// <summary>布局宽度。</summary>
        public float Width { get; set; }
        /// <summary>布局高度。</summary>
        public float Height { get; set; }
        /// <summary>行高系数。</summary>
        public float LineHeight { get; set; } = 1.0f;
        /// <summary>水平对齐。</summary>
        public StringAlignment Alignment { get; set; } = StringAlignment.Near;
        /// <summary>字距。</summary>
        public float LetterSpacing { get; set; } = 0.0f;
        /// <summary>词距。</summary>
        public float WordSpacing { get; set; } = 0.0f;
        /// <summary>可选的 StringFormat 用于文本绘制。</summary>
        public StringFormat? StringFormat { get; set; }
        /// <summary>基线相对布局顶部的偏移（mm 或像素）。</summary>
        public float BaselineOffset { get; set; } = 0f;
    }
}
