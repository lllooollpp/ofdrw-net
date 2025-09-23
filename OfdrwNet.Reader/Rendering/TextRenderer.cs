using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading.Tasks;
using OfdrwNet.Reader.Model;

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
                // 保存图形状态
                var state = graphics.Save();

                // 应用变换矩阵
                ApplyTransform(graphics, textObject, renderContext);

                // 设置文本渲染质量
                graphics.TextRenderingHint = renderContext.TextRenderingHint;

                // 获取字体
                var font = await GetFontAsync(textObject.Font, renderContext);

                // 获取画刷
                var brush = CreateBrush(textObject.Color);

                // 渲染文本
                await RenderTextContentAsync(textObject, graphics, font, brush, renderContext);

                // 恢复图形状态
                graphics.Restore(state);

                return true;
            }
            catch (Exception ex)
            {
                throw new RenderException(textObject.Id.ToString(), $"文本渲染失败: {ex.Message}", ex);
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
                    textObject.Boundary.X,
                    textObject.Boundary.Y,
                    (int)size.Width,
                    (int)size.Height
                );
            }

            return textObject?.Boundary ?? Rectangle.Empty;
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

            var cacheKey = $"{fontInfo.Name}_{fontInfo.Size}_{fontInfo.Style}_{renderContext.ScaleFactor}";

            if (_fontCache.TryGetFont(cacheKey, out var cachedFont) && cachedFont != null)
            {
                return cachedFont;
            }

            // 尝试从资源管理器获取字体
            Font font;
            try
            {
                var resourceFont = await _resourceManager.GetFontAsync(fontInfo.Name);
                var scaledSize = (float)(fontInfo.Size * renderContext.ScaleFactor);
                font = new Font(resourceFont.FontFamily, scaledSize, fontInfo.Style);
            }
            catch
            {
                // 回退到系统字体
                var scaledSize = (float)(fontInfo.Size * renderContext.ScaleFactor);
                font = new Font(fontInfo.Name, scaledSize, fontInfo.Style);
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

        /// <summary>
        /// 异步渲染文本内容
        /// </summary>
        private async Task RenderTextContentAsync(TextObject textObject, Graphics graphics, Font font, Brush brush, RenderContext renderContext)
        {
            await Task.Run(() =>
            {
                if (textObject.Layout != null)
                {
                    // 使用布局信息渲染
                    var layoutRect = new RectangleF(
                        (float)textObject.Layout.X,
                        (float)textObject.Layout.Y,
                        (float)textObject.Layout.Width,
                        (float)textObject.Layout.Height
                    );

                    var stringFormat = textObject.Layout.StringFormat ?? StringFormat.GenericDefault;
                    graphics.DrawString(textObject.Text, font, brush, layoutRect, stringFormat);
                }
                else
                {
                    // 简单文本渲染
                    graphics.DrawString(textObject.Text, font, brush, textObject.Boundary.Location);
                }
            });
        }

        /// <summary>
        /// 释放资源
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

    /// <summary>
    /// 字体缓存管理器
    /// </summary>
    internal class FontCache : IDisposable
    {
        private readonly Dictionary<string, Font> _fontCache = new Dictionary<string, Font>();
        private readonly Font _defaultFont;

        public FontCache()
        {
            _defaultFont = new Font("Arial", 12);
        }

        public bool TryGetFont(string key, out Font? font)
        {
            font = null;
            return _fontCache.TryGetValue(key, out font) && font != null;
        }

        public void AddFont(string key, Font font)
        {
            if (!_fontCache.ContainsKey(key))
            {
                _fontCache[key] = font;
            }
        }

        public Font GetDefaultFont()
        {
            return _defaultFont;
        }

        public void Dispose()
        {
            foreach (var font in _fontCache.Values)
            {
                font.Dispose();
            }
            _fontCache.Clear();
            _defaultFont?.Dispose();
        }
    }
}
