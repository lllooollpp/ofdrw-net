using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using OfdrwNet.Reader.Model;

namespace OfdrwNet.Reader.Rendering
{
    /// <summary>
    /// 图像渲染器
    /// 负责渲染OFD文档中的图像对象
    /// </summary>
    public class ImageRenderer : IDisposable
    {
        private readonly IResourceManager _resourceManager;
        private readonly ImageCache _imageCache;
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
        public ImageRenderer(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _imageCache = new ImageCache();
        }

        /// <summary>
        /// 异步渲染图像对象
        /// </summary>
        /// <param name="imageObject">图像对象</param>
        /// <param name="graphics">图形上下文</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>渲染是否成功</returns>
        public async Task<bool> RenderAsync(ImageObject imageObject, Graphics graphics, RenderContext renderContext)
        {
            if (imageObject == null || graphics == null || renderContext == null)
                return false;

            if (!imageObject.Visible)
                return true;

            try
            {
                // 保存图形状态
                var state = graphics.Save();

                // 设置图像渲染质量
                graphics.InterpolationMode = renderContext.ImageInterpolationMode;
                graphics.CompositingQuality = renderContext.CompositingQuality;
                graphics.SmoothingMode = renderContext.SmoothingMode;

                // 应用变换矩阵
                ApplyTransform(graphics, imageObject, renderContext);

                // 获取图像
                var image = await GetImageAsync(imageObject, renderContext);
                if (image == null)
                    return false;

                // 渲染图像
                await RenderImageContentAsync(imageObject, graphics, image, renderContext);

                // 恢复图形状态
                graphics.Restore(state);

                return true;
            }
            catch (Exception ex)
            {
                throw new RenderException(imageObject.Id.ToString(), $"图像渲染失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查图像对象是否在指定点
        /// </summary>
        /// <param name="imageObject">图像对象</param>
        /// <param name="point">测试点</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>是否命中</returns>
        public async Task<bool> HitTestAsync(ImageObject imageObject, Point point, RenderContext renderContext)
        {
            if (imageObject?.Boundary == null)
                return false;

            var bounds = imageObject.Boundary;

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

            return bounds.Contains(point);
        }

        /// <summary>
        /// 获取图像对象的边界框
        /// </summary>
        /// <param name="imageObject">图像对象</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>边界框</returns>
        public async Task<Rectangle> GetBoundsAsync(ImageObject imageObject, RenderContext renderContext)
        {
            if (imageObject?.Boundary != null)
            {
                var bounds = Rectangle.Round(imageObject.Boundary);

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

            return Rectangle.Empty;
        }

        /// <summary>
        /// 获取图像的原始尺寸
        /// </summary>
        /// <param name="imageObject">图像对象</param>
        /// <returns>原始尺寸</returns>
        public async Task<Size> GetOriginalSizeAsync(ImageObject imageObject)
        {
            if (imageObject == null)
                return Size.Empty;

            try
            {
                var image = await GetImageAsync(imageObject, null);
                return image?.Size ?? Size.Empty;
            }
            catch
            {
                return Size.Empty;
            }
        }

        /// <summary>
        /// 预加载图像
        /// </summary>
        /// <param name="imageObjects">图像对象列表</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>预加载任务</returns>
        public async Task PreloadImagesAsync(IEnumerable<ImageObject> imageObjects, RenderContext renderContext)
        {
            if (imageObjects == null)
                return;

            var tasks = imageObjects.Select(async imageObj =>
            {
                try
                {
                    await GetImageAsync(imageObj, renderContext);
                }
                catch
                {
                    // 忽略预加载错误
                }
            });

            await Task.WhenAll(tasks);
        }

        // 私有方法

        /// <summary>
        /// 应用变换矩阵
        /// </summary>
        private void ApplyTransform(Graphics graphics, ImageObject imageObject, RenderContext renderContext)
        {
            // 应用渲染上下文的变换
            if (renderContext.TransformMatrix != null)
            {
                graphics.MultiplyTransform(renderContext.TransformMatrix);
            }

            // 方案B（像素管线）下：Boundary 已经是最终像素坐标，CTM 中常含 mm->px 缩放 & Y 轴翻转；再次应用会把图像移出视口。
            // 仅在统一缩放模式（保持 mm 坐标）时才应用 CTM。
            bool unified = IsUnified();
            if (unified && imageObject.CTM != null)
            {
                graphics.MultiplyTransform(imageObject.CTM);
            }
            else if (!unified && imageObject.CTM != null)
            {
                // 像素模式：完全跳过 CTM（防止双重缩放/翻转）。方向纠正在 RenderImageContentAsync 内按需处理。
                System.Diagnostics.Trace.WriteLine("[ImageRenderer][DEBUG] Skip CTM entirely in pixel mode");
            }
        }

        /// <summary>
        /// 获取图像
        /// </summary>
        private async Task<Image?> GetImageAsync(ImageObject imageObject, RenderContext? renderContext)
        {
            if (string.IsNullOrEmpty(imageObject.ResourceId))
                return null;

            var cacheKey = renderContext != null
                ? $"{imageObject.ResourceId}_{renderContext.ScaleFactor}_{renderContext.ImageQuality}"
                : imageObject.ResourceId;

            if (_imageCache.TryGetImage(cacheKey, out var cachedImage))
            {
                return cachedImage;
            }

            try
            {
                // 从资源管理器获取图像
                var imageData = await _resourceManager.GetImageAsync(imageObject.ResourceId);
                if (imageData == null)
                {
                    System.Diagnostics.Trace.WriteLine($"[ImageRenderer] 图像资源为空: {imageObject.ResourceId}");
                    return CreatePlaceholderImage(imageObject.ResourceId);
                }

                System.Drawing.Image image;
                if (imageData is System.Drawing.Image img)
                {
                    image = img;
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine($"[ImageRenderer] 图像资源类型不正确: {imageObject.ResourceId}");
                    return CreatePlaceholderImage(imageObject.ResourceId);
                }

                // 根据渲染上下文调整图像
                if (renderContext != null)
                {
                    image = AdjustImageForRendering(image, renderContext, imageObject);
                }

                _imageCache.AddImage(cacheKey, image);
                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[ImageRenderer] 加载图像失败: {imageObject.ResourceId} - {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"[ImageRenderer] 异常详情: {ex}");

                // 返回占位图像而不是抛出异常，保证渲染能继续进行
                return CreatePlaceholderImage(imageObject.ResourceId);
            }
        }

        /// <summary>
        /// 创建占位图像
        /// </summary>
        private Image CreatePlaceholderImage(string resourceId)
        {
            var placeholder = new Bitmap(100, 60);
            using (var g = Graphics.FromImage(placeholder))
            {
                g.Clear(Color.LightGray);
                using (var brush = new SolidBrush(Color.Red))
                using (var font = new Font("Arial", 8))
                {
                    g.DrawString($"IMG:{resourceId}", font, brush, 2, 2);
                    g.DrawString("加载失败", font, brush, 2, 20);
                }
                // 绘制边框
                using (var pen = new Pen(Color.Red, 1))
                {
                    g.DrawRectangle(pen, 0, 0, placeholder.Width - 1, placeholder.Height - 1);
                }
            }
            return placeholder;
        }

        /// <summary>
        /// 根据渲染上下文调整图像
        /// </summary>
        private Image AdjustImageForRendering(Image originalImage, RenderContext renderContext, ImageObject imageObject)
        {
            // 如果不需要调整，直接返回原图
            if (renderContext.ScaleFactor == 1.0 && renderContext.ImageQuality == ImageQuality.Original)
            {
                return originalImage;
            }

            // 计算新尺寸
            var newWidth = (int)(imageObject.Boundary.Width * renderContext.ScaleFactor);
            var newHeight = (int)(imageObject.Boundary.Height * renderContext.ScaleFactor);

            if (newWidth <= 0 || newHeight <= 0)
                return originalImage;

            // 创建调整后的图像
            var adjustedImage = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(adjustedImage))
            {
                // 设置渲染质量
                graphics.InterpolationMode = GetInterpolationMode(renderContext.ImageQuality);
                graphics.CompositingQuality = renderContext.CompositingQuality;
                graphics.SmoothingMode = renderContext.SmoothingMode;

                // 绘制调整后的图像
                graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
            }

            return adjustedImage;
        }

        /// <summary>
        /// 根据图像质量获取插值模式
        /// </summary>
        private InterpolationMode GetInterpolationMode(ImageQuality quality)
        {
            return quality switch
            {
                ImageQuality.High => InterpolationMode.HighQualityBicubic,
                ImageQuality.Medium => InterpolationMode.HighQualityBilinear,
                ImageQuality.Low => InterpolationMode.Low,
                ImageQuality.Draft => InterpolationMode.NearestNeighbor,
                _ => InterpolationMode.Default
            };
        }

        /// <summary>
        /// 异步渲染图像内容
        /// </summary>
    private Task RenderImageContentAsync(ImageObject imageObject, Graphics graphics, Image image, RenderContext renderContext)
        {
            // 像素管线：Boundary 已为最终像素坐标；此处仅做方向与透明度处理，不再做几何 Transform。
            var x = imageObject.Boundary.X;
            var y = imageObject.Boundary.Y;
            var w = imageObject.Boundary.Width;
            var h = imageObject.Boundary.Height;
            bool unified = IsUnified();
            var destRect = new Rectangle((int)x, (int)y, (int)w, (int)h);

            // 基于 CTM 符号决定是否水平/垂直翻转（或 180°）——只在像素模式下，因为统一模式下 CTM 已由上层实际乘进 Graphics。
            bool flipH = false, flipV = false, rotate180 = false;
            Image drawImage = image;
            if (!unified && imageObject.CTM != null)
            {
                try
                {
                    var elems = imageObject.CTM.Elements; // m11,m12,m21,m22,dx,dy
                    float sx = elems[0];
                    float sy = elems[3];
                    if (sx < 0) flipH = true;
                    if (sy < 0) flipV = true;
                    if (flipH && flipV)
                    {
                        // 同时为负，相当于旋转 180°；避免先后双 Flip 额外成本，直接使用 Rotate180。
                        rotate180 = true;
                        flipH = flipV = false; // 独立标记 rotate180
                    }

                    if (rotate180 || flipH || flipV)
                    {
                        // 克隆后旋转，保持缓存原始图不被污染（不同对象可有不同 CTM）。
                        drawImage = (Image)image.Clone();
                        RotateFlipType rft = RotateFlipType.RotateNoneFlipNone;
                        if (rotate180)
                        {
                            rft = RotateFlipType.Rotate180FlipNone;
                        }
                        else if (flipH)
                        {
                            rft = RotateFlipType.RotateNoneFlipX;
                        }
                        else if (flipV)
                        {
                            rft = RotateFlipType.RotateNoneFlipY;
                        }
                        if (rft != RotateFlipType.RotateNoneFlipNone)
                        {
                            (drawImage as Bitmap)?.RotateFlip(rft);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[ImageRenderer][DEBUG] Orientation analysis failed: {ex.Message}");
                    // 失败则保持原图直接绘制
                    if (drawImage != image)
                    {
                        drawImage.Dispose();
                        drawImage = image;
                        rotate180 = flipH = flipV = false;
                    }
                }
            }

            // 透明度
            if ((float)imageObject.Alpha < 1.0f)
            {
                using var imageAttributes = new ImageAttributes();
                var colorMatrix = new ColorMatrix();
                colorMatrix.Matrix33 = (float)imageObject.Alpha;
                imageAttributes.SetColorMatrix(colorMatrix);
                graphics.DrawImage(drawImage, destRect, 0, 0, drawImage.Width, drawImage.Height, GraphicsUnit.Pixel, imageAttributes);
            }
            else
            {
                graphics.DrawImage(drawImage, destRect);
            }

            // 诊断信息
            try
            {
                Color center = Color.Empty;
                if (drawImage is Bitmap bmp && bmp.Width > 0 && bmp.Height > 0)
                {
                    center = bmp.GetPixel(drawImage.Width / 2, drawImage.Height / 2);
                    var tl = bmp.GetPixel(0, 0);
                    var tr = bmp.GetPixel(Math.Max(0, drawImage.Width - 1), 0);
                    var bl = bmp.GetPixel(0, Math.Max(0, drawImage.Height - 1));
                    var br = bmp.GetPixel(Math.Max(0, drawImage.Width - 1), Math.Max(0, drawImage.Height - 1));
                    System.Diagnostics.Trace.WriteLine($"[ImageRenderer] IMG {imageObject.ResourceId} corners TL=#{tl.ToArgb():X8} TR=#{tr.ToArgb():X8} BL=#{bl.ToArgb():X8} BR=#{br.ToArgb():X8}");
                }
                bool placeholder = drawImage.Width == 100 && drawImage.Height == 60; // 与 CreatePlaceholderImage 一致
                string ctmStr = imageObject.CTM != null ? string.Join(',', imageObject.CTM.Elements) : "<null>";
                string transformStr = imageObject.Transform != null ? string.Join(',', imageObject.Transform.Elements) : "<null>";
                System.Diagnostics.Trace.WriteLine($"[ImageRenderer] IMG {imageObject.ResourceId} dest=({destRect.X},{destRect.Y},{destRect.Width},{destRect.Height}) src=({drawImage.Width}x{drawImage.Height}) center=#{center.ToArgb():X8} placeholder={placeholder} unified={unified} alpha={imageObject.Alpha} rotate180={rotate180} flipH={flipH} flipV={flipV} CTM={ctmStr} Transform={transformStr}");
            }
            catch { }

            if (drawImage != image)
            {
                // 释放克隆（GDI 对象及时释放）
                drawImage.Dispose();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _imageCache?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 图像质量枚举
    /// </summary>
    public enum ImageQuality
    {
        /// <summary>原始质量</summary>
        Original,
        /// <summary>高质量</summary>
        High,
        /// <summary>中等质量</summary>
        Medium,
        /// <summary>低质量</summary>
        Low,
        /// <summary>草图质量</summary>
        Draft
    }

    /// <summary>
    /// 图像缓存管理器
    /// </summary>
    internal class ImageCache : IDisposable
    {
        private readonly Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();
        private readonly object _lockObject = new object();

        public bool TryGetImage(string key, out Image? image)
        {
            lock (_lockObject)
            {
                return _imageCache.TryGetValue(key, out image);
            }
        }

        public void AddImage(string key, Image image)
        {
            lock (_lockObject)
            {
                if (!_imageCache.ContainsKey(key))
                {
                    _imageCache[key] = image;
                }
            }
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                foreach (var image in _imageCache.Values)
                {
                    image.Dispose();
                }
                _imageCache.Clear();
            }
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
