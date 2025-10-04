using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading.Tasks;
using OfdrwNet.Reader.Model;
using System.Diagnostics;
// note: RenderingConfig 位于同命名空间，但编译器当前未识别（可能增量编译缓存问题），改为显式完全限定调用

namespace OfdrwNet.Reader.Rendering
{
    /// <summary>
    /// 文本渲染器
    /// 负责渲染OFD文档中的文本对象
    /// </summary>
    /// <summary>
    /// 文本渲染器，负责渲染OFD文档中的文本对象，支持字体管理和文本样式
    /// </summary>
    public class TextRenderer : IDisposable
    {
        private readonly IResourceManager _resourceManager;
        private readonly FontCache _fontCache;
        private bool _disposed = false;
        private static bool IsUnified()
        {
            try
            {
                var t = Type.GetType("OfdrwNet.Reader.Rendering.RenderingConfig");
                if (t != null)
                {
                    var p = t.GetProperty("UnifiedScalingMode");
                    if (p != null) return (bool)p.GetValue(null)!;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="resourceManager">资源管理器</param>
        public TextRenderer(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _fontCache = new FontCache();
        }

        /// <summary>
        /// 异步渲染文本对象
        /// </summary>
        /// <param name="textObject">文本对象</param>
        /// <param name="graphics">图形上下文</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>渲染是否成功</returns>
        public async Task<bool> RenderAsync(TextObject textObject, Graphics graphics, RenderContext renderContext)
        {
            if (textObject == null || graphics == null || renderContext == null)
                return false;

            if (!textObject.Visible || string.IsNullOrEmpty(textObject.Text))
                return true;

            try
            {
                // 统一记录开始日志: ID / Boundary / CTM / 字体信息 (若已存在) / 文本长度
                string ctmStr = "none";
                try
                {
                    var mat = textObject.CTM ?? textObject.OriginalCTM;
                    if (mat != null)
                    {
                        var m = mat.Elements; // [m11, m12, m21, m22, dx, dy]
                        ctmStr = $"[{m[0]:0.###},{m[1]:0.###},{m[2]:0.###},{m[3]:0.###},{m[4]:0.###},{m[5]:0.###}]{(textObject.CtmIsInternalGlyph && textObject.CTM == null ? "(internal-folded)" : string.Empty)}";
                    }
                }
                catch { }
                var fontInfo = textObject.Font;
                string fontDesc = fontInfo == null ? "(no-font)" : $"{fontInfo.Name ?? "?"},{fontInfo.Size:0.###},{fontInfo.Style}";
                // 文本预览（控制长度，转义换行/制表）
                string? raw = textObject.Text;
                string preview;
                if (string.IsNullOrEmpty(raw)) preview = ""; else
                {
                    preview = raw.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
                    if (preview.Length > 80) preview = preview.Substring(0, 80) + "...";
                }
                Debug.WriteLine($"[TextRenderer] Begin Text Id={textObject.Id} Boundary=({textObject.Boundary.X:0.###},{textObject.Boundary.Y:0.###},{textObject.Boundary.Width:0.###},{textObject.Boundary.Height:0.###}) CTM={ctmStr} Font={fontDesc} Length={(textObject.Text?.Length ?? 0)} Text=\"{preview}\"");
                
                // Validate Graphics state before attempting to save
                try
                {
                    // Test access to Graphics properties to ensure it's valid
                    _ = graphics.IsClipEmpty;
                    _ = graphics.DpiX;
                }
                catch (Exception gex)
                {
                    // Graphics object is in invalid state, skip rendering
                    Debug.WriteLine($"[TextRenderer] Graphics invalid for object {textObject.Id}: {gex.Message}");
                    return false;
                }

                GraphicsState? state = null;
                bool stateSaved = false;
                try
                {
                    // 保存图形状态
                    state = graphics.Save();
                    stateSaved = true;
                }
                catch (ArgumentException)
                {
                    // Graphics.Save() can fail with "Parameter is not valid" in certain threading scenarios
                    // Fall back to rendering without save/restore
                    Debug.WriteLine($"[TextRenderer] Graphics.Save() failed for object {textObject.Id}, rendering without state save");
                }

                try
                {
                    // 应用变换矩阵
                    ApplyTransform(graphics, textObject, renderContext);

                    // 安全设置文本渲染质量
                    SafeApplyTextRenderingHint(graphics, renderContext);

                    // 获取字体
                    var font = await GetFontAsync(textObject.Font, renderContext);

                    // 获取画刷并确保释放
                    using (var brush = CreateBrush(textObject.Color))
                    {
                        // 渲染文本
                        await RenderTextContentAsync(textObject, graphics, font, brush, renderContext);
                    }

                    Debug.WriteLine($"[TextRenderer] End Text Id={textObject.Id} Success=True");
                    return true;
                }
                finally
                {
                    if (stateSaved && state != null)
                    {
                        try
                        {
                            // 恢复图形状态
                            graphics.Restore(state);
                        }
                        catch (ArgumentException)
                        {
                            // Ignore restore failures - graphics might have been invalidated
                            Debug.WriteLine($"[TextRenderer] Graphics.Restore() failed for object {textObject.Id}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TextRenderer] End Text Id={textObject?.Id} Success=False Error={ex.Message}");
                var tid = textObject?.Id?.ToString() ?? string.Empty;
                throw new RenderException(tid, $"文本渲染失败: {ex.Message}", ex);
            }
        }

        private static void SafeApplyTextRenderingHint(Graphics graphics, RenderContext ctx)
        {
            if (graphics == null) return;
            try
            {
                // 防御：某些 GDI+ 上下文（例如打印/已释放）可能抛出 ArgumentException
                if (ctx != null)
                {
                    var hint = ctx.TextRenderingHint;
                    // 枚举值范围验证（System.Drawing.Text.TextRenderingHint 0..5，含 ClearTypeGridFit=5）
                    if ((int)hint < 0 || (int)hint > (int)System.Drawing.Text.TextRenderingHint.ClearTypeGridFit)
                    {
                        hint = System.Drawing.Text.TextRenderingHint.SystemDefault;
                    }
                    graphics.TextRenderingHint = hint;
                }
            }
            catch (ArgumentException)
            {
                // 回退：忽略并使用系统默认，减少噪音
            }
            catch (Exception)
            {
                // 其它异常亦忽略（保持渲染不中断）
            }
        }

        /// <summary>
        /// 测量文本尺寸
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <param name="fontInfo">字体信息</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>文本尺寸</returns>
        public async Task<SizeF> MeasureTextAsync(string text, FontInfo fontInfo, RenderContext renderContext)
        {
            if (string.IsNullOrEmpty(text))
                return SizeF.Empty;

            try
            {
                var font = await GetFontAsync(fontInfo, renderContext);

                using var tempGraphics = Graphics.FromImage(new Bitmap(1, 1));
                tempGraphics.TextRenderingHint = renderContext.TextRenderingHint;
                return tempGraphics.MeasureString(text, new Font(font.FontFamily, font.Size, font.Style));
            }
            catch
            {
                return SizeF.Empty;
            }
        }

        /// <summary>
        /// 检查文本对象是否在指定点
        /// </summary>
        /// <param name="textObject">文本对象</param>
        /// <param name="point">测试点</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>是否命中</returns>
        public Task<bool> HitTestAsync(TextObject textObject, Point point, RenderContext renderContext)
        {
            if (textObject?.Layout == null)
                return Task.FromResult(false);

            var bounds = new Rectangle(
                (int)textObject.Layout.X,
                (int)textObject.Layout.Y,
                (int)textObject.Layout.Width,
                (int)textObject.Layout.Height
            );

            // 应用缩放变换
            if (renderContext.ScaleFactor != 1.0)
            {
                bounds = new Rectangle(
                    (int)(bounds.X * renderContext.ScaleFactor),
                    (int)(bounds.Y * renderContext.ScaleFactor),
                    (int)(bounds.Width * renderContext.ScaleFactor),
                    (int)(bounds.Height * renderContext.ScaleFactor)
                );
            }

            return Task.FromResult(bounds.Contains(point));
        }

        /// <summary>
        /// 获取文本对象的边界框
        /// </summary>
        /// <param name="textObject">文本对象</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>边界框</returns>
        public async Task<Rectangle> GetBoundsAsync(TextObject textObject, RenderContext renderContext)
        {
            if (textObject?.Layout != null)
            {
                var bounds = new Rectangle(
                    (int)textObject.Layout.X,
                    (int)textObject.Layout.Y,
                    (int)textObject.Layout.Width,
                    (int)textObject.Layout.Height
                );

                // 应用缩放变换
                if (renderContext.ScaleFactor != 1.0)
                {
                    bounds = new Rectangle(
                        (int)(bounds.X * renderContext.ScaleFactor),
                        (int)(bounds.Y * renderContext.ScaleFactor),
                        (int)(bounds.Width * renderContext.ScaleFactor),
                        (int)(bounds.Height * renderContext.ScaleFactor)
                    );
                }

                return bounds;
            }

            // 如果没有布局信息，尝试测量文本
            if (!string.IsNullOrEmpty(textObject?.Text) && textObject.Font != null)
            {
                var size = await MeasureTextAsync(textObject.Text, textObject.Font, renderContext);
                return new Rectangle(
                    (int)textObject.Boundary.X,
                    (int)textObject.Boundary.Y,
                    (int)size.Width,
                    (int)size.Height
                );
            }

            return textObject != null ? Rectangle.Round(textObject.Boundary) : Rectangle.Empty;
        }

        // 私有方法

        /// <summary>
        /// 应用变换矩阵
        /// </summary>
        private void ApplyTransform(Graphics graphics, TextObject textObject, RenderContext renderContext)
        {
            // 应用渲染上下文的变换
            if (renderContext.TransformMatrix != null)
            {
                graphics.MultiplyTransform(renderContext.TransformMatrix);
            }

            // 应用对象的CTM变换
            if (textObject.CTM != null && !textObject.CtmIsInternalGlyph)
            {
                graphics.MultiplyTransform(textObject.CTM);
            }

            // 方案B：已使用像素坐标，禁止再次缩放。
        }

        /// <summary>
        /// 获取字体
        /// </summary>
        private async Task<Font> GetFontAsync(FontInfo? fontInfo, RenderContext renderContext)
        {
            if (fontInfo == null)
            {
                return _fontCache.GetDefaultFont();
            }

            var name = string.IsNullOrWhiteSpace(fontInfo.Name) ? _fontCache.GetDefaultFont().Name : fontInfo.Name!;
            var cacheKey = $"{name}_{fontInfo.Size}_{fontInfo.Style}_{renderContext.ScaleFactor}";

            if (_fontCache.TryGetFont(cacheKey, out var cachedFont) && cachedFont != null)
            {
                return cachedFont;
            }

            Font font;
            try
            {
                var resourceFont = await _resourceManager.GetFontAsync(name); // 获取底层字体（可能只是占位）
                bool unified = IsUnified();
                // unified: fontInfo.Size 目前为 mm，需要转换为像素 = mm * Ppm * ScaleFactor
                // legacy(pixel) 管线: fontInfo.Size 已为像素大小（Extract 时算好），只需要乘缩放 ScaleFactor
                float sizePx = unified
                    ? (float)(fontInfo.Size * renderContext.Ppm * renderContext.ScaleFactor)
                    : (float)(fontInfo.Size * renderContext.ScaleFactor);
                // 使用 GraphicsUnit.Pixel，避免默认 Point -> 额外 96/72 放大导致“字体偏大”
                font = new Font(resourceFont.FontFamily, Math.Max(0.1f, sizePx), fontInfo.Style, GraphicsUnit.Pixel);
            }
            catch
            {
                bool unified = IsUnified();
                float sizePx = unified
                    ? (float)(fontInfo.Size * renderContext.Ppm * renderContext.ScaleFactor)
                    : (float)(fontInfo.Size * renderContext.ScaleFactor);
                font = new Font(name, Math.Max(0.1f, sizePx), fontInfo.Style, GraphicsUnit.Pixel);
            }

            // 样式可用性验证（某些字体不支持 Italic/Bold 组合会触发内部错误）
            try
            {
                if (!font.FontFamily.IsStyleAvailable(font.Style))
                {
                    var fallbackStyle = FontStyle.Regular;
                    if (!font.FontFamily.IsStyleAvailable(fallbackStyle))
                    {
                        fallbackStyle = FontStyle.Regular; // 仍不可用则保持
                    }
                    if (fallbackStyle != font.Style)
                    {
                        var replaced = new Font(font.FontFamily, font.Size, fallbackStyle);
                        font.Dispose();
                        font = replaced;
                        Debug.WriteLine($"[TextRenderer] Fallback font style -> {fallbackStyle} for '{name}'");
                    }
                }
            }
            catch (Exception fex)
            {
                Debug.WriteLine($"[TextRenderer] Font style availability check failed: {fex.Message}");
            }

            _fontCache.AddFont(cacheKey, font);
            return font;
        }

        /// <summary>
        /// 创建画刷
        /// </summary>
        private Brush CreateBrush(ColorInfo? colorInfo)
        {
            if (colorInfo != null)
            {
                return new SolidBrush(colorInfo.ToSystemColor());
            }
            return new SolidBrush(Color.Black);
        }

        private Task RenderTextContentAsync(TextObject textObject, Graphics graphics, Font font, Brush brush, RenderContext renderContext)
        {
            // 重要：GDI+ 对象 (Graphics) 不是线程安全的，不能在 Task.Run 的线程池里使用。
            // 之前的实现把 DrawString 放进 Task.Run，可能导致跨线程访问产生 System.ArgumentException("Parameter is not valid.")。
            // 这里改为同步执行并返回已完成的 Task。

            if (graphics == null || textObject == null || font == null || brush == null)
            {
                return Task.CompletedTask; // 防御式退出
            }

            SafeApplyTextRenderingHint(graphics, renderContext);

            // （移除未使用的 IsRectangleRenderable 本地函数）

            // 超长文本做一个软限制（防御：异常数据导致 GDI+ 失败）
            var rawText = textObject.Text ?? string.Empty;
            // 过滤控制字符（除 \r \n \t），防止奇异 glyph 引发 GDI 异常
            Span<char> buffer = rawText.ToCharArray();
            for (int i = 0; i < buffer.Length; i++)
            {
                var c = buffer[i];
                if (c < 0x20 && c != '\r' && c != '\n' && c != '\t') buffer[i] = ' ';
            }
            var text = new string(buffer);
            if (text.Length == 0 || string.IsNullOrWhiteSpace(text))
            {
                return Task.CompletedTask; // 空白文本直接跳过
            }
            if (text.Length > 20000)
            {
                Debug.WriteLine($"[TextRenderer] Skip or truncate very long text length={text.Length} Id={textObject.Id}");
                text = text.Substring(0, 20000);
            }

            try
            {
                // 线程诊断
                Debug.WriteLine($"[TextRenderer] Thread={Environment.CurrentManagedThreadId} IsThreadPool={(System.Threading.Thread.CurrentThread.IsThreadPoolThread)} Font='{font.Name}' RectLayout?={(textObject.Layout!=null)}");
                try
                {
                    _ = graphics.DpiX; // 访问一个属性确保 graphics 尚未被释放
                }
                catch (Exception dpiEx)
                {
                    Debug.WriteLine($"[TextRenderer] Graphics DPI access failed: {dpiEx.Message}");
                }
                // 优先使用布局矩形
                if (textObject.Layout != null &&
                    textObject.Layout.Width > 0 &&
                    textObject.Layout.Height > 0)
                {
                    var lx = (float)textObject.Layout.X;
                    var ly = (float)textObject.Layout.Y;
                    var lw = (float)textObject.Layout.Width;
                    var lh = (float)textObject.Layout.Height;

                    bool unified = IsUnified();
                    if (unified)
                    {
                        var factor = (float)(renderContext.Ppm * renderContext.ScaleFactor);
                        lx *= factor; ly *= factor; lw *= factor; lh *= factor;
                    }

                    bool IsValid(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
                    if (IsValid(lx) && IsValid(ly) && IsValid(lw) && IsValid(lh) && lw > 0 && lh > 0)
                    {
                        // 基线计算：使用字体度量纠正 ly，使文本视觉上位于 boundary 内部合理位置
                        var ascent = font.FontFamily.GetCellAscent(font.Style);
                        var em = font.FontFamily.GetEmHeight(font.Style);
                        var ascentRatio = em > 0 ? (float)ascent / em : 0.8f; // 兜底0.8
                        var descent = font.FontFamily.GetCellDescent(font.Style);
                        var descentRatio = em > 0 ? (float)descent / em : 0.2f;
                        var lineHeightRatio = ascentRatio + descentRatio; // 可能 <1（很多字体），用作比例
                        var expectedLinePx = font.Size; // 像素字号
                        var actualLinePx = expectedLinePx * lineHeightRatio;
                        if (actualLinePx <= 0) actualLinePx = expectedLinePx * 1.0f;
                        // 若 boundary(height) 比行高大，垂直居中；否则顶部对齐
                        float topAdjust = ly;
                        if (lh > actualLinePx)
                        {
                            topAdjust = ly + (lh - actualLinePx) / 2f; // 居中
                        }
                        // 计算 ly 的基线位置调试：baseline = topAdjust + ascentRatio * expectedLinePx
                        var baseline = topAdjust + ascentRatio * expectedLinePx;
                        textObject.Layout.BaselineOffset = baseline - topAdjust; // 存储供后续调试
                        var layoutRect = new RectangleF(lx, topAdjust, lw, lh - (topAdjust - ly));

                        // 与当前可见裁剪区域取交集，避免极大或溢出的矩形进入 GDI
                        // 暂时移除裁剪可见性提前跳过逻辑，避免误判导致文字不画

                        // Clone 避免修改全局 GenericDefault / 复用实例造成竞争
                        var fmt = textObject.Layout.StringFormat != null
                            ? (StringFormat)textObject.Layout.StringFormat.Clone()
                            : (StringFormat)StringFormat.GenericDefault.Clone();
                        try
                        {
                            fmt.Alignment = textObject.Layout.Alignment;
                                // 动态适配：若字号远大于布局高度，按比例缩放
                                try
                                {
                                    var measured = graphics.MeasureString(text, font);
                                    // 先禁用自动缩放，保证与 boundary 对齐调试
                                }
                                catch { }
                                try { graphics.DrawString(text, font, brush, layoutRect, fmt); }
                                catch (ArgumentException firstEx) { Debug.WriteLine($"[TextRenderer] First DrawString failed, retry simple no-format. {firstEx.Message}"); graphics.DrawString(text, font, brush, layoutRect.Location); }
                                // 调试：描出布局框确认位置（蓝框）
                                // 调试边界显示（运行时可改为 true 观察布局框）
                                bool debugDrawBounds = false;
                                if (debugDrawBounds)
                                {
                                    try { graphics.DrawRectangle(Pens.Blue, layoutRect.X, layoutRect.Y, layoutRect.Width, layoutRect.Height); } catch { }
                                }
                        }
                        finally
                        {
                            fmt.Dispose();
                        }
                        return Task.CompletedTask;
                    }
                }

                // 回退：使用边界左上角 + 简单基线调整
                var x = textObject.Boundary.X;
                var y = textObject.Boundary.Y;
                try
                {
                    var ascent = font.FontFamily.GetCellAscent(font.Style);
                    var em = font.FontFamily.GetEmHeight(font.Style);
                    var ascentRatio = em > 0 ? (float)ascent / em : 0.8f;
                    // 让文字 baseline 贴 boundary.Top + ascentRatio*size (如 boundary 太矮则不做调整)
                    if (textObject.Boundary.Height >= font.Size * 0.6f)
                    {
                        // boundary 的 top 作为行框 top
                        // baseline = top + ascentRatio * font.Size
                        // DrawString 使用 layout 矩形时已经处理，这里 fallback 使用点绘制：设定 y = top + ( (boundaryHeight - lineHeight)/2 ) for centering
                        var descent = font.FontFamily.GetCellDescent(font.Style);
                        var descentRatio = em > 0 ? (float)descent / em : 0.2f;
                        var lineHeightPx = font.Size * (ascentRatio + descentRatio);
                        if (lineHeightPx <= 0) lineHeightPx = font.Size;
                        if (textObject.Boundary.Height > lineHeightPx)
                        {
                            y = textObject.Boundary.Y + (textObject.Boundary.Height - lineHeightPx) / 2f; // 居中
                        }
                    }
                }
                catch { }
                bool unifiedFallback = IsUnified();
                if (unifiedFallback)
                {
                    var factorFallback = (float)(renderContext.Ppm * renderContext.ScaleFactor);
                    x *= factorFallback; y *= factorFallback;
                }

                if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y))
                {
                    // 再次防御，避免无效坐标
                    x = 0;
                    y = 0;
                }
                try
                {
                    try
                    {
                        var measured = graphics.MeasureString(text, font);
                        // 禁用无布局回退自动缩放
                    }
                    catch { }
                    graphics.DrawString(text, font, brush, x, y);
                    bool debugDrawBounds2 = false;
                    if (debugDrawBounds2)
                    {
                        try { graphics.DrawRectangle(Pens.Blue, textObject.Boundary.X, textObject.Boundary.Y, textObject.Boundary.Width, textObject.Boundary.Height); } catch { }
                    }
                }
                catch (ArgumentException firstEx2)
                {
                    Debug.WriteLine($"[TextRenderer] Fallback DrawString failed once, retry with point (0,0). {firstEx2.Message}");
                    graphics.DrawString(text, font, brush, 0f, 0f);
                }
            }
            catch (ArgumentException argEx)
            {
                // 捕获典型的 "Parameter is not valid."，附加上下文信息帮助调试
                var detail = $"DrawString 参数异常: len={text?.Length}, font='{font?.Name}', layout=({textObject.Layout?.X},{textObject.Layout?.Y},{textObject.Layout?.Width},{textObject.Layout?.Height}), boundary=({textObject.Boundary.X},{textObject.Boundary.Y},{textObject.Boundary.Width},{textObject.Boundary.Height})";
                Debug.WriteLine($"[TextRenderer][ArgumentException] {detail}\nStack: {argEx.StackTrace}");
                throw new RenderException(textObject.Id.ToString(), detail, argEx);
            }
            catch (Exception ex)
            {
                throw new RenderException(textObject.Id.ToString(), "绘制文本时发生未预期异常", ex);
            }

            return Task.CompletedTask;
        }

    /// <summary>
    /// 释放字体缓存。
    /// </summary>
    public void Dispose()
        {
            if (!_disposed)
            {
                _fontCache?.Dispose();
                _disposed = true;
            }
        }
    }

    internal class FontCache : IDisposable
    {
        private readonly Dictionary<string, Font> _cache = new();
        private readonly Font _default = new("Arial", 12f);

        public bool TryGetFont(string key, out Font? font) => _cache.TryGetValue(key, out font);
        public void AddFont(string key, Font font) { if (!_cache.ContainsKey(key)) _cache[key] = font; }
        public Font GetDefaultFont() => _default;
        public void Dispose()
        {
            foreach (var f in _cache.Values) f.Dispose();
            _default.Dispose();
            _cache.Clear();
        }
    }
}
