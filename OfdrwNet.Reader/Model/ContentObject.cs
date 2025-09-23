using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Reader.Model
{
    /// <summary>
    /// 内容对象基类
    /// 所有页面内容对象的基础接口和行为
    /// </summary>
    public abstract class ContentObject
    {
        /// <summary>
        /// 对象ID
        /// </summary>
        public StId Id { get; set; } = new StId(0);

        /// <summary>
        /// 边界框
        /// </summary>
        public Rectangle Boundary { get; set; }

        /// <summary>
        /// 当前变换矩阵 (Current Transformation Matrix)
        /// </summary>
        public Matrix? CTM { get; set; }

        /// <summary>
        /// Z序（绘制顺序）
        /// </summary>
        public int ZOrder { get; set; }

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// 缓存是否有效
        /// </summary>
        public bool IsCacheValid { get; set; }

        /// <summary>
        /// 缓存时间戳
        /// </summary>
        public DateTime CacheTimestamp { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        protected ContentObject()
        {
            CacheTimestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// 异步渲染对象到图形上下文
        /// </summary>
        /// <param name="graphics">图形上下文</param>
        /// <param name="context">渲染上下文</param>
        /// <returns>渲染是否成功</returns>
        public abstract Task<bool> RenderAsync(Graphics graphics, RenderContext context);

        /// <summary>
        /// 获取对象边界
        /// </summary>
        /// <param name="context">渲染上下文</param>
        /// <returns>对象边界</returns>
        public abstract Rectangle GetBounds(RenderContext context);

        /// <summary>
        /// 点击测试
        /// </summary>
        /// <param name="point">测试点</param>
        /// <param name="context">渲染上下文</param>
        /// <returns>是否命中</returns>
        public abstract bool HitTest(Point point, RenderContext context);

        /// <summary>
        /// 应用变换矩阵到图形上下文
        /// </summary>
        /// <param name="graphics">图形上下文</param>
        /// <param name="context">渲染上下文</param>
        protected virtual void ApplyTransform(Graphics graphics, RenderContext context)
        {
            // 应用渲染上下文的变换
            if (context.TransformMatrix != null)
            {
                graphics.MultiplyTransform(context.TransformMatrix);
            }

            // 应用对象的CTM变换
            if (CTM != null)
            {
                graphics.MultiplyTransform(CTM);
            }
        }

        /// <summary>
        /// 检查对象是否在视口内
        /// </summary>
        /// <param name="context">渲染上下文</param>
        /// <returns>是否在视口内</returns>
        protected virtual bool IsInViewport(RenderContext context)
        {
            if (context.ViewPort.IsEmpty)
                return true;

            var bounds = GetBounds(context);
            return context.ViewPort.IntersectsWith(bounds);
        }

        /// <summary>
        /// 无效化缓存
        /// </summary>
        public virtual void InvalidateCache()
        {
            IsCacheValid = false;
            CacheTimestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            CTM?.Dispose();
        }
    }

    /// <summary>
    /// 文本对象
    /// </summary>
    public class TextObject : ContentObject
    {
        /// <summary>
        /// 文本内容
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 字体信息
        /// </summary>
        public FontInfo? Font { get; set; }

        /// <summary>
        /// 颜色信息
        /// </summary>
        public ColorInfo? Color { get; set; }

        /// <summary>
        /// 文本布局
        /// </summary>
        public TextLayout? Layout { get; set; }

        /// <summary>
        /// 渲染文本对象
        /// </summary>
        public override async Task<bool> RenderAsync(Graphics graphics, RenderContext context)
        {
            if (!Visible || string.IsNullOrEmpty(Text) || !IsInViewport(context))
                return true;

            try
            {
                var state = graphics.Save();

                // 应用变换
                ApplyTransform(graphics, context);

                // 设置文本渲染质量
                graphics.TextRenderingHint = context.TextRenderingHint;

                // 创建字体和画刷
                using var font = CreateFont(context);
                using var brush = CreateBrush();

                // 渲染文本
                if (Layout != null)
                {
                    // 使用布局信息渲染
                    var layoutRect = new RectangleF(
                        (float)Layout.X, (float)Layout.Y,
                        (float)Layout.Width, (float)Layout.Height);
                    graphics.DrawString(Text, font, brush, layoutRect, Layout.StringFormat);
                }
                else
                {
                    // 简单文本渲染
                    graphics.DrawString(Text, font, brush, Boundary.Location);
                }

                graphics.Restore(state);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取文本边界
        /// </summary>
        public override Rectangle GetBounds(RenderContext context)
        {
            if (Layout != null)
            {
                return new Rectangle(
                    (int)Layout.X, (int)Layout.Y,
                    (int)Layout.Width, (int)Layout.Height);
            }
            return Boundary;
        }

        /// <summary>
        /// 文本点击测试
        /// </summary>
        public override bool HitTest(Point point, RenderContext context)
        {
            return GetBounds(context).Contains(point);
        }

        private System.Drawing.Font CreateFont(RenderContext context)
        {
            if (Font != null)
            {
                var size = (float)(Font.Size * context.ScaleFactor);
                return new System.Drawing.Font(Font.Name, size, Font.Style);
            }
            return new System.Drawing.Font("Arial", 12);
        }

        private Brush CreateBrush()
        {
            if (Color != null)
            {
                return new SolidBrush(Color.ToSystemColor());
            }
            return new SolidBrush(System.Drawing.Color.Black);
        }
    }

    /// <summary>
    /// 图像对象
    /// </summary>
    public class ImageObject : ContentObject
    {
        /// <summary>
        /// 资源ID
        /// </summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// 图像信息
        /// </summary>
        public ImageInfo? ImageInfo { get; set; }

        /// <summary>
        /// 图像缩放方式
        /// </summary>
        public ImageScaling Scaling { get; set; } = ImageScaling.Stretch;

        /// <summary>
        /// 渲染图像对象
        /// </summary>
        public override async Task<bool> RenderAsync(Graphics graphics, RenderContext context)
        {
            if (!Visible || string.IsNullOrEmpty(ResourceId) || !IsInViewport(context))
                return true;

            try
            {
                var state = graphics.Save();

                // 应用变换
                ApplyTransform(graphics, context);

                // 设置图像渲染质量
                graphics.InterpolationMode = context.InterpolationMode;

                // TODO: 从资源管理器获取图像
                // var image = await resourceManager.GetImageAsync(ResourceId);

                // 暂时创建占位符图像进行测试
                using var placeholderImage = CreatePlaceholderImage();

                // 计算目标矩形
                var destRect = CalculateDestinationRectangle(context);

                // 绘制图像
                graphics.DrawImage(placeholderImage, destRect);

                graphics.Restore(state);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取图像边界
        /// </summary>
        public override Rectangle GetBounds(RenderContext context)
        {
            return Boundary;
        }

        /// <summary>
        /// 图像点击测试
        /// </summary>
        public override bool HitTest(Point point, RenderContext context)
        {
            return GetBounds(context).Contains(point);
        }

        private Rectangle CalculateDestinationRectangle(RenderContext context)
        {
            // 根据缩放方式计算目标矩形
            switch (Scaling)
            {
                case ImageScaling.None:
                    return new Rectangle(Boundary.X, Boundary.Y,
                        ImageInfo?.Width ?? Boundary.Width,
                        ImageInfo?.Height ?? Boundary.Height);

                case ImageScaling.Uniform:
                    return CalculateUniformScaling();

                case ImageScaling.UniformToFill:
                    return CalculateUniformToFillScaling();

                case ImageScaling.Stretch:
                default:
                    return Boundary;
            }
        }

        private Rectangle CalculateUniformScaling()
        {
            if (ImageInfo == null) return Boundary;

            double scaleX = (double)Boundary.Width / ImageInfo.Width;
            double scaleY = (double)Boundary.Height / ImageInfo.Height;
            double scale = Math.Min(scaleX, scaleY);

            int width = (int)(ImageInfo.Width * scale);
            int height = (int)(ImageInfo.Height * scale);

            int x = Boundary.X + (Boundary.Width - width) / 2;
            int y = Boundary.Y + (Boundary.Height - height) / 2;

            return new Rectangle(x, y, width, height);
        }

        private Rectangle CalculateUniformToFillScaling()
        {
            if (ImageInfo == null) return Boundary;

            double scaleX = (double)Boundary.Width / ImageInfo.Width;
            double scaleY = (double)Boundary.Height / ImageInfo.Height;
            double scale = Math.Max(scaleX, scaleY);

            int width = (int)(ImageInfo.Width * scale);
            int height = (int)(ImageInfo.Height * scale);

            int x = Boundary.X + (Boundary.Width - width) / 2;
            int y = Boundary.Y + (Boundary.Height - height) / 2;

            return new Rectangle(x, y, width, height);
        }

        private Bitmap CreatePlaceholderImage()
        {
            var bitmap = new Bitmap(100, 100);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(System.Drawing.Color.LightGray);
            g.DrawRectangle(Pens.DarkGray, 0, 0, 99, 99);
            g.DrawLine(Pens.DarkGray, 0, 0, 99, 99);
            g.DrawLine(Pens.DarkGray, 0, 99, 99, 0);
            return bitmap;
        }
    }

    /// <summary>
    /// 矢量对象
    /// </summary>
    public class VectorObject : ContentObject
    {
        /// <summary>
        /// 路径数据
        /// </summary>
        public string PathData { get; set; } = string.Empty;

        /// <summary>
        /// 描边信息
        /// </summary>
        public StrokeInfo? Stroke { get; set; }

        /// <summary>
        /// 填充信息
        /// </summary>
        public FillInfo? Fill { get; set; }

        /// <summary>
        /// 渲染矢量对象
        /// </summary>
        public override async Task<bool> RenderAsync(Graphics graphics, RenderContext context)
        {
            if (!Visible || string.IsNullOrEmpty(PathData) || !IsInViewport(context))
                return true;

            try
            {
                var state = graphics.Save();

                // 应用变换
                ApplyTransform(graphics, context);

                // 设置渲染质量
                graphics.SmoothingMode = context.SmoothingMode;

                // 解析路径数据
                using var path = ParsePathData(PathData);

                // 填充路径
                if (Fill != null && Fill.Enabled)
                {
                    using var fillBrush = CreateFillBrush();
                    graphics.FillPath(fillBrush, path);
                }

                // 描边路径
                if (Stroke != null && Stroke.Enabled)
                {
                    using var strokePen = CreateStrokePen(context);
                    graphics.DrawPath(strokePen, path);
                }

                graphics.Restore(state);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取矢量边界
        /// </summary>
        public override Rectangle GetBounds(RenderContext context)
        {
            return Boundary;
        }

        /// <summary>
        /// 矢量点击测试
        /// </summary>
        public override bool HitTest(Point point, RenderContext context)
        {
            try
            {
                using var path = ParsePathData(PathData);

                // 检查填充区域
                if (Fill != null && Fill.Enabled && path.IsVisible(point))
                {
                    return true;
                }

                // 检查描边区域
                if (Stroke != null && Stroke.Enabled)
                {
                    using var pen = CreateStrokePen(context);
                    return path.IsOutlineVisible(point, pen);
                }

                return false;
            }
            catch
            {
                return GetBounds(context).Contains(point);
            }
        }

        private GraphicsPath ParsePathData(string pathData)
        {
            // TODO: 实现完整的SVG路径解析
            // 这里提供一个简化的实现
            var path = new GraphicsPath();

            if (string.IsNullOrEmpty(pathData))
                return path;

            // 简单的矩形路径示例
            if (pathData.StartsWith("M") && pathData.Contains("L"))
            {
                path.AddRectangle(Boundary);
            }

            return path;
        }

        private Brush CreateFillBrush()
        {
            if (Fill?.Color != null)
            {
                return new SolidBrush(Fill.Color.ToSystemColor());
            }
            return new SolidBrush(System.Drawing.Color.Black);
        }

        private Pen CreateStrokePen(RenderContext context)
        {
            var color = Stroke?.Color?.ToSystemColor() ?? System.Drawing.Color.Black;
            var width = (float)((Stroke?.Width ?? 1.0) * context.ScaleFactor);

            var pen = new Pen(color, width);

            if (Stroke != null)
            {
                pen.LineJoin = Stroke.LineJoin;
                pen.StartCap = Stroke.StartCap;
                pen.EndCap = Stroke.EndCap;

                if (Stroke.DashPattern != null && Stroke.DashPattern.Length > 0)
                {
                    pen.DashPattern = Stroke.DashPattern;
                }
            }

            return pen;
        }
    }

    // 辅助类定义

    /// <summary>
    /// 字体信息
    /// </summary>
    public class FontInfo
    {
        public string Name { get; set; } = "Arial";
        public float Size { get; set; } = 12;
        public FontStyle Style { get; set; } = FontStyle.Regular;
    }

    /// <summary>
    /// 颜色信息
    /// </summary>
    public class ColorInfo
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; } = 255;

        public System.Drawing.Color ToSystemColor()
        {
            return System.Drawing.Color.FromArgb(A, R, G, B);
        }
    }

    /// <summary>
    /// 文本布局
    /// </summary>
    public class TextLayout
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public StringFormat? StringFormat { get; set; }
    }

    /// <summary>
    /// 图像信息
    /// </summary>
    public class ImageInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; } = "PNG";
    }

    /// <summary>
    /// 图像缩放方式
    /// </summary>
    public enum ImageScaling
    {
        None,
        Uniform,
        UniformToFill,
        Stretch
    }

    /// <summary>
    /// 描边信息
    /// </summary>
    public class StrokeInfo
    {
        public bool Enabled { get; set; } = true;
        public ColorInfo? Color { get; set; }
        public double Width { get; set; } = 1.0;
        public LineJoin LineJoin { get; set; } = LineJoin.Miter;
        public LineCap StartCap { get; set; } = LineCap.Flat;
        public LineCap EndCap { get; set; } = LineCap.Flat;
        public float[]? DashPattern { get; set; }
    }

    /// <summary>
    /// 填充信息
    /// </summary>
    public class FillInfo
    {
        public bool Enabled { get; set; } = true;
        public ColorInfo? Color { get; set; }
    }
}
