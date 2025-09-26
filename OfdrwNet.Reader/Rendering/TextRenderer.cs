using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading.Tasks;
using OfdrwNet.Reader.Model;
using System.Diagnostics;

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
                // Graphics 有效性快速探测（可能被外部提前释放）
                try
                {
                    _ = graphics.DpiX; // 访问属性，如果已释放会抛
                    var clip = graphics.VisibleClipBounds; // 也可触发潜在无效状态
                    if (clip.Width <= 0 || clip.Height <= 0)
                    {
                        Debug.WriteLine($"[TextRenderer] Skip drawing: empty clip bounds for TextObject {textObject.Id}");
                        return true; // 不渲染但视为成功
                    }
                }
                catch (Exception gex)
                {
                    Debug.WriteLine($"[TextRenderer] Graphics invalid early, skip TextObject {textObject.Id}: {gex.Message}");
                    return true; // 不中断整体渲染
                }
                // 保存图形状态
                var state = graphics.Save();

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

                // 恢复图形状态
                graphics.Restore(state);

                return true;
            }
            catch (Exception ex)
            {
                throw new RenderException(textObject.Id.ToString(), $"文本渲染失败: {ex.Message}", ex);
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

                return tempGraphics.MeasureString(text, font);
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
            if (textObject.CTM != null)
            {
                graphics.MultiplyTransform(textObject.CTM);
            }

            // 应用缩放
            if (renderContext.ScaleFactor != 1.0)
            {
                graphics.ScaleTransform((float)renderContext.ScaleFactor, (float)renderContext.ScaleFactor);
            }
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
                var resourceFont = await _resourceManager.GetFontAsync(name); // 保持同步上下文，避免 UI 线程丢失
                var scaledSize = (float)(fontInfo.Size * renderContext.ScaleFactor);
                font = new Font(resourceFont.FontFamily, scaledSize, fontInfo.Style);
            }
            catch
            {
                var scaledSize = (float)(fontInfo.Size * renderContext.ScaleFactor);
                font = new Font(name, scaledSize, fontInfo.Style);
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

            // 本地辅助函数：检测矩形是否可渲染
            static bool IsRectangleRenderable(RectangleF r)
                => r.Width > 0 && r.Height > 0 &&
                   !float.IsNaN(r.X) && !float.IsNaN(r.Y) && !float.IsNaN(r.Width) && !float.IsNaN(r.Height) &&
                   !float.IsInfinity(r.X) && !float.IsInfinity(r.Y) && !float.IsInfinity(r.Width) && !float.IsInfinity(r.Height);

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

                    bool IsValid(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
                    if (IsValid(lx) && IsValid(ly) && IsValid(lw) && IsValid(lh) && lw > 0 && lh > 0)
                    {
                        var layoutRect = new RectangleF(lx, ly, lw, lh);

                        // 与当前可见裁剪区域取交集，避免极大或溢出的矩形进入 GDI
                        try
                        {
                            var clip = graphics.VisibleClipBounds; // 可能返回 Empty（未设置 clip）
                            if (!clip.IsEmpty && !clip.Contains(layoutRect))
                            {
                                var intersection = RectangleF.Intersect(clip, layoutRect);
                                if (IsRectangleRenderable(intersection))
                                {
                                    layoutRect = intersection;
                                }
                                else if (!clip.IntersectsWith(layoutRect))
                                {
                                    Debug.WriteLine($"[TextRenderer] Layout rect completely outside clip. Skip draw. Id={textObject.Id}");
                                    return Task.CompletedTask;
                                }
                            }
                        }
                        catch (Exception eClip)
                        {
                            Debug.WriteLine($"[TextRenderer] Clip bounds check failed: {eClip.Message}");
                        }

                        // Clone 避免修改全局 GenericDefault / 复用实例造成竞争
                        var fmt = textObject.Layout.StringFormat != null
                            ? (StringFormat)textObject.Layout.StringFormat.Clone()
                            : (StringFormat)StringFormat.GenericDefault.Clone();
                        try
                        {
                            fmt.Alignment = textObject.Layout.Alignment;
                            try
                            {
                                graphics.DrawString(text, font, brush, layoutRect, fmt);
                            }
                            catch (ArgumentException firstEx)
                            {
                                Debug.WriteLine($"[TextRenderer] First DrawString failed, retry simple no-format. {firstEx.Message}");
                                // 去除 StringFormat 再尝试一次（某些 GDI+ 在特定格式组合下会抛）
                                graphics.DrawString(text, font, brush, layoutRect.Location);
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
                var y = textObject.Boundary.Y + font.Size * 0.8f; // baseline tweak

                if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y))
                {
                    // 再次防御，避免无效坐标
                    x = 0;
                    y = 0;
                }
                try
                {
                    graphics.DrawString(text, font, brush, x, y);
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
