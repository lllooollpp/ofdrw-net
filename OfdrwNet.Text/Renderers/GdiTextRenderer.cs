using System.Drawing;
using System.Drawing.Text;
using System.Xml.Linq;

namespace OfdrwNet.Text.Renderers;

/// <summary>
/// 基于 System.Drawing 的文本渲染器实现
/// 整合原 TextRenderer 中的文本渲染逻辑
/// </summary>
public class GdiTextRenderer : IOfdTextRenderer
{
    private readonly Dictionary<string, FontFamily> _fontFamilyCache = new();
    private readonly object _cacheLock = new object();

    /// <summary>
    /// 渲染文本对象到 GDI+ 图形上下文
    /// </summary>
    /// <param name="context">渲染上下文（应为 Graphics）</param>
    /// <param name="textObject">文本对象元素</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>渲染任务</returns>
    public async Task RenderTextObjectAsync(object context, XElement textObject, CancellationToken cancellationToken = default)
    {
        if (context is not Graphics graphics)
            throw new ArgumentException("Context must be Graphics for GdiTextRenderer", nameof(context));

        try
        {
            // 解析文本属性
            var boundary = TextRenderingUtils.ParseBoundary(textObject);
            var fontSize = TextRenderingUtils.GetFontSize(textObject);
            var fontId = TextRenderingUtils.GetFontId(textObject);

            // 获取字体
            var font = CreateFont(fontId ?? "SimSun", fontSize);
            using var brush = new SolidBrush(Color.Black);

            // 获取文本内容
            var textCodes = TextRenderingUtils.ExtractTextCodes(textObject);

            foreach (var textCode in textCodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrEmpty(textCode.Text) && font != null)
                {
                    try
                    {
                        // 计算绘制位置，添加基线校正
                        var baselineOffset = CalculateBaselineOffset(font, fontSize);
                        var drawPoint = new PointF(
                            boundary.X + textCode.X,
                            boundary.Y + textCode.Y - baselineOffset  // 减去基线偏移
                        );

                        // 使用 Graphics.DrawString 进行文本渲染
                        graphics.DrawString(textCode.Text, font, brush, drawPoint);
                    }
                    catch (Exception ex)
                    {
                        // 记录但不中断渲染
                        System.Diagnostics.Debug.WriteLine($"[GdiTextRenderer] 渲染文本失败: {ex.Message}");
                    }
                }
            }

            font?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GdiTextRenderer] 渲染文本对象失败: {ex.Message}");
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
    /// 创建字体
    /// </summary>
    /// <param name="fontName">字体名称</param>
    /// <param name="fontSize">字体大小</param>
    /// <returns>字体对象</returns>
    private Font? CreateFont(string fontName, float fontSize)
    {
        try
        {
            var fontFamily = GetFontFamily(fontName);
            if (fontFamily != null)
            {
                // 使用像素单位而不是点单位，提供更精确的控制
                return new Font(fontFamily, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
            }

            // 回退到系统默认字体
            return new Font(FontFamily.GenericSansSerif, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GdiTextRenderer] 创建字体失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取字体族
    /// </summary>
    /// <param name="fontName">字体名称</param>
    /// <returns>字体族</returns>
    private FontFamily? GetFontFamily(string fontName)
    {
        lock (_cacheLock)
        {
            if (_fontFamilyCache.TryGetValue(fontName, out var cachedFamily))
                return cachedFamily;

            try
            {
                // 首先尝试系统字体
                var families = FontFamily.Families;
                var family = families.FirstOrDefault(f =>
                    string.Equals(f.Name, fontName, StringComparison.OrdinalIgnoreCase));

                if (family != null)
                {
                    _fontFamilyCache[fontName] = family;
                    return family;
                }

                // 字体映射表
                var fontMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["SimSun"] = "SimSun",
                    ["宋体"] = "SimSun",
                    ["KaiTi"] = "KaiTi",
                    ["楷体"] = "KaiTi",
                    ["Microsoft YaHei"] = "Microsoft YaHei",
                    ["微软雅黑"] = "Microsoft YaHei",
                    ["Times New Roman"] = "Times New Roman",
                    ["Arial"] = "Arial"
                };

                if (fontMappings.TryGetValue(fontName, out var mappedName))
                {
                    family = families.FirstOrDefault(f =>
                        string.Equals(f.Name, mappedName, StringComparison.OrdinalIgnoreCase));

                    if (family != null)
                    {
                        _fontFamilyCache[fontName] = family;
                        return family;
                    }
                }

                // 回退到默认字体
                family = FontFamily.GenericSansSerif;
                _fontFamilyCache[fontName] = family;
                return family;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GdiTextRenderer] 获取字体族失败: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// 计算基线偏移量
    /// </summary>
    /// <param name="font">字体</param>
    /// <param name="fontSize">字体大小</param>
    /// <returns>基线偏移量</returns>
    private float CalculateBaselineOffset(Font? font, float fontSize)
    {
        var config = TextRenderingConfig.Instance;

        if (!config.EnableBaselineCorrection)
            return 0f;

        if (font?.FontFamily == null)
        {
            var defaultOffset = fontSize * config.GdiBaselineOffsetFactor;
            if (config.EnableDebugOutput)
                System.Diagnostics.Debug.WriteLine($"[GdiTextRenderer] 使用默认偏移: {defaultOffset:F2}");
            return defaultOffset;
        }

        try
        {
            // 获取字体度量信息
            var fontFamily = font.FontFamily;
            var emHeight = fontFamily.GetEmHeight(FontStyle.Regular);
            var ascent = fontFamily.GetCellAscent(FontStyle.Regular);

            if (emHeight > 0)
            {
                var ascentRatio = (float)ascent / emHeight;
                var baseOffset = fontSize * ascentRatio;
                var adjustedOffset = baseOffset * config.GdiBaselineOffsetFactor;

                if (config.EnableDebugOutput)
                    System.Diagnostics.Debug.WriteLine($"[GdiTextRenderer] 计算偏移: {adjustedOffset:F2}, 原始={baseOffset:F2}, 系数={config.GdiBaselineOffsetFactor}");

                return adjustedOffset;
            }
        }
        catch (Exception ex)
        {
            if (config.EnableDebugOutput)
                System.Diagnostics.Debug.WriteLine($"[GdiTextRenderer] 获取字体度量失败: {ex.Message}");
        }

        // 如果获取字体度量失败，使用估算值
        var fallbackOffset = fontSize * 0.8f * config.GdiBaselineOffsetFactor;
        if (config.EnableDebugOutput)
            System.Diagnostics.Debug.WriteLine($"[GdiTextRenderer] 使用回退偏移: {fallbackOffset:F2}");

        return fallbackOffset;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        lock (_cacheLock)
        {
            foreach (var family in _fontFamilyCache.Values)
            {
                try
                {
                    family?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GdiTextRenderer] 释放字体族失败: {ex.Message}");
                }
            }
            _fontFamilyCache.Clear();
        }
    }
}
