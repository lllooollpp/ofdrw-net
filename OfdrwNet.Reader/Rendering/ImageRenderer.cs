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

            // 应用对象的CTM变换
            if (imageObject.CTM != null)
            {
                graphics.MultiplyTransform(imageObject.CTM);
            }

            // 应用缩放
            if (renderContext.ScaleFactor != 1.0)
            {
                graphics.ScaleTransform((float)renderContext.ScaleFactor, (float)renderContext.ScaleFactor);
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
                    return null;

                var image = Image.FromStream(imageData);

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
                throw new InvalidOperationException($"加载图像失败: {imageObject.ResourceId}", ex);
            }
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
        private async Task RenderImageContentAsync(ImageObject imageObject, Graphics graphics, Image image, RenderContext renderContext)
        {
            await Task.Run(() =>
            {
                var destRect = new Rectangle(
                    imageObject.Boundary.X,
                    imageObject.Boundary.Y,
                    imageObject.Boundary.Width,
                    imageObject.Boundary.Height
                );

                // 应用透明度
                if (imageObject.Alpha < 1.0f)
                {
                    var colorMatrix = new ColorMatrix();
                    colorMatrix.Matrix33 = imageObject.Alpha; // 设置Alpha值

                    var imageAttributes = new ImageAttributes();
                    imageAttributes.SetColorMatrix(colorMatrix);

                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttributes);

                    imageAttributes.Dispose();
                }
                else
                {
                    graphics.DrawImage(image, destRect);
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
