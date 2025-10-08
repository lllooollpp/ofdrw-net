using SkiaSharp;
using System.Xml.Linq;

namespace OfdrwNet.Text.Renderers;

/// <summary>
/// 基于 SkiaSharp 的文本渲染器实现
/// 整合原 ImageRenderer 中的文本渲染逻辑
/// </summary>
public class SkiaTextRenderer : IOfdTextRenderer
{
    /// <summary>
    /// 渲染文本对象到 Skia 画布
    /// </summary>
    /// <param name="context">渲染上下文（应为 SKCanvas）</param>
    /// <param name="textObject">文本对象元素</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>渲染任务</returns>
    public async Task RenderTextObjectAsync(object context, XElement textObject, CancellationToken cancellationToken = default)
    {
        if (context is not SKCanvas canvas)
            throw new ArgumentException("Context must be SKCanvas for SkiaTextRenderer", nameof(context));

        // 解析文本属性
        var boundary = TextRenderingUtils.ParseBoundary(textObject);
        var fontSize = TextRenderingUtils.GetFontSize(textObject);

        // 获取文本内容
        var textCodes = TextRenderingUtils.ExtractTextCodes(textObject);

        foreach (var textCode in textCodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrEmpty(textCode.Text))
            {
                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = fontSize,
                    IsAntialias = true,
                    Typeface = SKTypeface.Default
                };

                var drawX = boundary.X + textCode.X;
                var drawY = boundary.Y + textCode.Y + CalculateSkiaBaselineOffset(paint, fontSize);

                canvas.DrawText(textCode.Text, drawX, drawY, paint);
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 获取文本对象的边界
    /// </summary>
    /// <param name="textObject">文本对象元素</param>
    /// <returns>边界矩形</returns>
    public System.Drawing.RectangleF GetTextObjectBounds(XElement textObject)
    {
        return TextRenderingUtils.ParseBoundary(textObject);
    }

    /// <summary>
    /// 测试点是否在文本对象内
    /// </summary>
    /// <param name="textObject">文本对象元素</param>
    /// <param name="point">测试点</param>
    /// <param name="tolerance">容差</param>
    /// <returns>是否命中</returns>
    public bool HitTest(XElement textObject, System.Drawing.PointF point, float tolerance = 0f)
    {
        var bounds = GetTextObjectBounds(textObject);
        if (tolerance > 0)
        {
            bounds.Inflate(tolerance, tolerance);
        }
        return bounds.Contains(point);
    }

    /// <summary>
    /// 计算 Skia 文本的基线偏移量
    /// </summary>
    /// <param name="paint">文本画笔</param>
    /// <param name="fontSize">字体大小</param>
    /// <returns>基线偏移量</returns>
    private static float CalculateSkiaBaselineOffset(SKPaint paint, float fontSize)
    {
        var config = TextRenderingConfig.Instance;

        if (!config.EnableBaselineCorrection)
            return 0f;

        try
        {
            // 获取字体度量信息
            var fontMetrics = paint.FontMetrics;

            // 返回基线位置（正值表示从顶部向下）
            var baseOffset = -fontMetrics.Ascent;
            var adjustmentFactor = config.SkiaBaselineOffsetFactor - 1.0f;
            var adjustedOffset = baseOffset * adjustmentFactor;

            if (config.EnableDebugOutput)
                System.Diagnostics.Debug.WriteLine($"[SkiaTextRenderer] 计算偏移: {adjustedOffset:F2}, 原始={baseOffset:F2}, 系数={config.SkiaBaselineOffsetFactor}");

            return adjustedOffset;
        }
        catch (Exception ex)
        {
            if (config.EnableDebugOutput)
                System.Diagnostics.Debug.WriteLine($"[SkiaTextRenderer] 获取字体度量失败: {ex.Message}");

            // 如果获取度量失败，使用估算值
            var fallbackOffset = fontSize * (config.SkiaBaselineOffsetFactor - 1.0f);

            if (config.EnableDebugOutput)
                System.Diagnostics.Debug.WriteLine($"[SkiaTextRenderer] 使用回退偏移: {fallbackOffset:F2}");

            return fallbackOffset;
        }
    }

    /// <summary>
    /// 计算文本的实际绘制尺寸
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <param name="fontSize">字体大小</param>
    /// <param name="typeface">字体</param>
    /// <returns>文本尺寸</returns>
    public static SKSize MeasureText(string text, float fontSize, SKTypeface? typeface = null)
    {
        using var paint = new SKPaint
        {
            TextSize = fontSize,
            Typeface = typeface ?? SKTypeface.Default
        };

        var bounds = new SKRect();
        paint.MeasureText(text, ref bounds);
        return new SKSize(bounds.Width, bounds.Height);
    }

    /// <summary>
    /// 创建带抗锯齿的文本画笔
    /// </summary>
    /// <param name="fontSize">字体大小</param>
    /// <param name="color">文本颜色</param>
    /// <param name="typeface">字体</param>
    /// <returns>配置好的画笔</returns>
    public static SKPaint CreateTextPaint(float fontSize, SKColor color, SKTypeface? typeface = null)
    {
        return new SKPaint
        {
            Color = color,
            TextSize = fontSize,
            IsAntialias = true,
            Typeface = typeface ?? SKTypeface.Default,
            SubpixelText = true,
            LcdRenderText = true
        };
    }
}
