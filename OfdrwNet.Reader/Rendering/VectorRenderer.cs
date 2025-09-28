using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using OfdrwNet.Reader.Model;

namespace OfdrwNet.Reader.Rendering
{
    /// <summary>
    /// 矢量图形渲染器
    /// 负责渲染OFD文档中的矢量图形对象
    /// </summary>
    public class VectorRenderer : IDisposable
    {
        private readonly IResourceManager _resourceManager;
        private readonly PathCache _pathCache;
        private bool _disposed = false;

        private static bool IsUnified()
        {
            try
            {
                var t = Type.GetType("OfdrwNet.Reader.Rendering.RenderingConfig");
                if (t != null)
                {
                    var p = t.GetProperty("UnifiedScalingMode");
                    if (p != null)
                    {
                        return (bool)p.GetValue(null)!;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="resourceManager">资源管理器</param>
        public VectorRenderer(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _pathCache = new PathCache();
        }

        /// <summary>
        /// 异步渲染矢量图形对象
        /// </summary>
        /// <param name="vectorObject">矢量图形对象</param>
        /// <param name="graphics">图形上下文</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>渲染是否成功</returns>
        public async Task<bool> RenderAsync(VectorObject vectorObject, Graphics graphics, RenderContext renderContext)
        {
            if (vectorObject == null || graphics == null || renderContext == null)
            {
                return false;
            }

            if (!vectorObject.Visible)
            {
                return true;
            }

            try
            {
                System.Diagnostics.Trace.WriteLine($"[VectorRenderer] Begin render Id={vectorObject.Id} Type={vectorObject.VectorType} Boundary=({vectorObject.Boundary.X},{vectorObject.Boundary.Y},{vectorObject.Boundary.Width},{vectorObject.Boundary.Height}) StrokeW={vectorObject.StrokeStyle?.Width} PathLen={(vectorObject.PathData?.Length ?? 0)} Points={(vectorObject.Points?.Count ?? 0)}");

                var state = graphics.Save();
                graphics.SmoothingMode = renderContext.SmoothingMode;
                graphics.CompositingQuality = renderContext.CompositingQuality;

                ApplyTransform(graphics, vectorObject, renderContext);

                var result = vectorObject.VectorType switch
                {
                    VectorType.Path => await RenderPathAsync(vectorObject, graphics, renderContext),
                    VectorType.Line => await RenderLineAsync(vectorObject, graphics, renderContext),
                    VectorType.Rectangle => await RenderRectangleAsync(vectorObject, graphics, renderContext),
                    VectorType.Circle => await RenderCircleAsync(vectorObject, graphics, renderContext),
                    VectorType.Ellipse => await RenderEllipseAsync(vectorObject, graphics, renderContext),
                    VectorType.Polygon => await RenderPolygonAsync(vectorObject, graphics, renderContext),
                    VectorType.Polyline => await RenderPolylineAsync(vectorObject, graphics, renderContext),
                    _ => false
                };

                graphics.Restore(state);
                System.Diagnostics.Trace.WriteLine($"[VectorRenderer] End render Id={vectorObject.Id} Success={result}");

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[VectorRenderer] Exception Id={vectorObject?.Id} {ex.Message}");
                throw new RenderException(vectorObject?.Id.ToString() ?? string.Empty, $"矢量图形渲染失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查矢量对象是否在指定点
        /// </summary>
        public async Task<bool> HitTestAsync(VectorObject vectorObject, Point point, RenderContext renderContext)
        {
            if (vectorObject?.Boundary == null)
            {
                return false;
            }

            var bounds = vectorObject.Boundary;

            if (renderContext.ScaleFactor != 1.0)
            {
                bounds = new Rectangle(
                    (int)(bounds.X * renderContext.ScaleFactor),
                    (int)(bounds.Y * renderContext.ScaleFactor),
                    (int)(bounds.Width * renderContext.ScaleFactor),
                    (int)(bounds.Height * renderContext.ScaleFactor)
                );
            }

            if (!bounds.Contains(point))
            {
                return false;
            }

            if (vectorObject.VectorType == VectorType.Path && !string.IsNullOrEmpty(vectorObject.PathData))
            {
                try
                {
                    using var path = await GetGraphicsPathAsync(vectorObject.PathData);
                    return path.IsVisible(point);
                }
                catch
                {
                    return bounds.Contains(point);
                }
            }

            return true;
        }

        /// <summary>
        /// 获取矢量对象的边界框
        /// </summary>
        public Task<Rectangle> GetBoundsAsync(VectorObject vectorObject, RenderContext renderContext)
        {
            if (vectorObject?.Boundary != null)
            {
                var bounds = Rectangle.Round(vectorObject.Boundary);

                if (renderContext.ScaleFactor != 1.0)
                {
                    bounds = new Rectangle(
                        (int)(bounds.X * renderContext.ScaleFactor),
                        (int)(bounds.Y * renderContext.ScaleFactor),
                        (int)(bounds.Width * renderContext.ScaleFactor),
                        (int)(bounds.Height * renderContext.ScaleFactor)
                    );
                }

                return Task.FromResult(bounds);
            }

            return Task.FromResult(Rectangle.Empty);
        }

        /// <summary>
        /// 渲染路径
        /// </summary>
        private async Task<bool> RenderPathAsync(VectorObject vectorObject, Graphics graphics, RenderContext renderContext)
        {
            if (string.IsNullOrEmpty(vectorObject.PathData))
            {
                return false;
            }

            try
            {
                System.Diagnostics.Trace.WriteLine($"[VectorRenderer] RenderPath Id={vectorObject.Id} PathLen={vectorObject.PathData.Length} StrokeW={vectorObject.StrokeStyle?.Width} Fill={(vectorObject.FillStyle != null)}");

                using var path = await GetGraphicsPathAsync(vectorObject.PathData);

                if (path.PointCount == 0)
                {
                    graphics.DrawRectangle(Pens.Gray, Rectangle.Round(vectorObject.Boundary));
                    return true;
                }

                if (vectorObject.FillStyle != null)
                {
                    var fillBrush = CreateBrush(vectorObject.FillStyle);
                    graphics.FillPath(fillBrush, path);
                    fillBrush.Dispose();
                }

                using var strokePen = vectorObject.StrokeStyle != null
                    ? CreatePen(vectorObject.StrokeStyle)
                    : new Pen(Color.Black, 1.0f);
                graphics.DrawPath(strokePen, path);
                System.Diagnostics.Trace.WriteLine($"[VectorRenderer] DrawPath Id={vectorObject.Id} PenW={strokePen.Width} PointCount={path.PointCount}");

                return true;
            }
            catch
            {
                graphics.DrawRectangle(Pens.DarkGray, Rectangle.Round(vectorObject.Boundary));
                return false;
            }
        }

        /// <summary>
        /// 渲染直线
        /// </summary>
        private async Task<bool> RenderLineAsync(VectorObject vectorObject, Graphics graphics, RenderContext renderContext)
        {
            if (vectorObject.StrokeStyle == null || vectorObject.Points == null || vectorObject.Points.Count < 2)
            {
                return false;
            }

            await Task.Run(() =>
            {
                var pen = CreatePen(vectorObject.StrokeStyle);
                var startPoint = vectorObject.Points[0];
                var endPoint = vectorObject.Points[1];

                graphics.DrawLine(pen, startPoint, endPoint);
                System.Diagnostics.Trace.WriteLine($"[VectorRenderer] DrawLine Id={vectorObject.Id} PenW={pen.Width} Start=({startPoint.X},{startPoint.Y}) End=({endPoint.X},{endPoint.Y})");
                pen.Dispose();
            });

            return true;
        }

        /// <summary>
        /// 渲染矩形
        /// </summary>
        private async Task<bool> RenderRectangleAsync(VectorObject vectorObject, Graphics graphics, RenderContext renderContext)
        {
            var rect = vectorObject.Boundary;

            await Task.Run(() =>
            {
                if (vectorObject.FillStyle != null)
                {
                    var fillBrush = CreateBrush(vectorObject.FillStyle);
                    graphics.FillRectangle(fillBrush, rect);
                    fillBrush.Dispose();
                }

                if (vectorObject.StrokeStyle != null)
                {
                    var strokePen = CreatePen(vectorObject.StrokeStyle);
                    graphics.DrawRectangle(strokePen, rect);
                    strokePen.Dispose();
                }
            });

            return true;
        }

        /// <summary>
        /// 渲染圆形
        /// </summary>
        private async Task<bool> RenderCircleAsync(VectorObject vectorObject, Graphics graphics, RenderContext renderContext)
        {
            var rect = vectorObject.Boundary;

            await Task.Run(() =>
            {
                if (vectorObject.FillStyle != null)
                {
                    var fillBrush = CreateBrush(vectorObject.FillStyle);
                    graphics.FillEllipse(fillBrush, rect);
                    fillBrush.Dispose();
                }

                if (vectorObject.StrokeStyle != null)
                {
                    var strokePen = CreatePen(vectorObject.StrokeStyle);
                    graphics.DrawEllipse(strokePen, rect);
                    strokePen.Dispose();
                }
            });

            return true;
        }

        /// <summary>
        /// 渲染椭圆
        /// </summary>
        private Task<bool> RenderEllipseAsync(VectorObject vectorObject, Graphics graphics, RenderContext renderContext)
        {
            return RenderCircleAsync(vectorObject, graphics, renderContext);
        }

        /// <summary>
        /// 渲染多边形
        /// </summary>
        private async Task<bool> RenderPolygonAsync(VectorObject vectorObject, Graphics graphics, RenderContext renderContext)
        {
            if (vectorObject.Points == null || vectorObject.Points.Count < 3)
            {
                return false;
            }

            await Task.Run(() =>
            {
                var points = vectorObject.Points.ToArray();

                if (vectorObject.FillStyle != null)
                {
                    var fillBrush = CreateBrush(vectorObject.FillStyle);
                    graphics.FillPolygon(fillBrush, points);
                    fillBrush.Dispose();
                }

                if (vectorObject.StrokeStyle != null)
                {
                    var strokePen = CreatePen(vectorObject.StrokeStyle);
                    graphics.DrawPolygon(strokePen, points);
                    strokePen.Dispose();
                }
            });

            return true;
        }

        /// <summary>
        /// 渲染折线
        /// </summary>
        private async Task<bool> RenderPolylineAsync(VectorObject vectorObject, Graphics graphics, RenderContext renderContext)
        {
            if (vectorObject.StrokeStyle == null || vectorObject.Points == null || vectorObject.Points.Count < 2)
            {
                return false;
            }

            await Task.Run(() =>
            {
                var pen = CreatePen(vectorObject.StrokeStyle);
                var points = vectorObject.Points.ToArray();
                graphics.DrawLines(pen, points);
                pen.Dispose();
            });

            return true;
        }

        /// <summary>
        /// 应用变换矩阵
        /// </summary>
        private void ApplyTransform(Graphics graphics, VectorObject vectorObject, RenderContext renderContext)
        {
            if (renderContext.TransformMatrix != null)
            {
                graphics.MultiplyTransform(renderContext.TransformMatrix);
            }

            bool unified = IsUnified();
            if (unified && vectorObject.CTM != null)
            {
                graphics.MultiplyTransform(vectorObject.CTM);
            }
            else if (!unified && vectorObject.CTM != null)
            {
                System.Diagnostics.Trace.WriteLine("[VectorRenderer][DEBUG] Skip CTM in pixel mode");
            }
        }

        /// <summary>
        /// 获取图形路径
        /// </summary>
        private Task<GraphicsPath> GetGraphicsPathAsync(string pathData)
        {
            if (string.IsNullOrWhiteSpace(pathData))
            {
                return Task.FromResult(new GraphicsPath());
            }

            var cacheKey = pathData;

            if (_pathCache.TryGetPath(cacheKey, out var cachedPath) && cachedPath != null)
            {
                return Task.FromResult((GraphicsPath)cachedPath.Clone());
            }

            var segments = PathGeometryUtil.Parse(pathData);
            var path = PathGeometryUtil.ToGraphicsPath(segments);

            _pathCache.AddPath(cacheKey, path);
            return Task.FromResult((GraphicsPath)path.Clone());
        }

        /// <summary>
        /// 创建画刷
        /// </summary>
        private Brush CreateBrush(FillStyle fillStyle)
        {
            if (fillStyle.Color != null)
            {
                return new SolidBrush(fillStyle.Color.ToSystemColor());
            }

            return new SolidBrush(Color.Black);
        }

        /// <summary>
        /// 创建画笔
        /// </summary>
        private Pen CreatePen(StrokeStyle strokeStyle)
        {
            var color = strokeStyle.Color?.ToSystemColor() ?? Color.Black;
            bool unified = IsUnified();
            float rawWidth = strokeStyle.Width;
            float effectiveWidth = rawWidth;

            if (!unified)
            {
                const float minLiftThreshold = 0.15f;
                const float minEffective = 0.5f;
                if (effectiveWidth <= minLiftThreshold)
                {
                    effectiveWidth = minEffective;
                }
                else if (effectiveWidth < 0.5f)
                {
                    effectiveWidth *= 0.85f;
                }
            }

            var pen = new Pen(color, effectiveWidth);

            if (strokeStyle.DashArray != null && strokeStyle.DashArray.Count > 0)
            {
                pen.DashPattern = strokeStyle.DashArray.ToArray();
            }

            pen.StartCap = ConvertLineCap(strokeStyle.StartCap);
            pen.EndCap = ConvertLineCap(strokeStyle.EndCap);
            pen.LineJoin = ConvertLineJoin(strokeStyle.LineJoin);

            if (effectiveWidth != rawWidth)
            {
                System.Diagnostics.Trace.WriteLine($"[VectorRenderer] Hairline adjusted rawWidth={rawWidth} -> {effectiveWidth}");
            }

            return pen;
        }

        /// <summary>
        /// 转换线条端点样式
        /// </summary>
        private LineCap ConvertLineCap(LineCapType capType)
        {
            return capType switch
            {
                LineCapType.Round => LineCap.Round,
                LineCapType.Square => LineCap.Square,
                _ => LineCap.Flat
            };
        }

        /// <summary>
        /// 转换线条连接样式
        /// </summary>
        private LineJoin ConvertLineJoin(LineJoinType joinType)
        {
            return joinType switch
            {
                LineJoinType.Round => LineJoin.Round,
                LineJoinType.Bevel => LineJoin.Bevel,
                _ => LineJoin.Miter
            };
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _pathCache.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 路径缓存管理器
    /// </summary>
    internal class PathCache : IDisposable
    {
        private readonly Dictionary<string, GraphicsPath> _pathCache = new Dictionary<string, GraphicsPath>();
        private readonly object _lockObject = new object();

        public bool TryGetPath(string key, out GraphicsPath? path)
        {
            lock (_lockObject)
            {
                return _pathCache.TryGetValue(key, out path);
            }
        }

        public void AddPath(string key, GraphicsPath path)
        {
            lock (_lockObject)
            {
                if (!_pathCache.ContainsKey(key))
                {
                    var clonedPath = (GraphicsPath?)path.Clone();
                    if (clonedPath != null)
                    {
                        _pathCache[key] = clonedPath;
                    }
                }
            }
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                foreach (var path in _pathCache.Values)
                {
                    path.Dispose();
                }
                _pathCache.Clear();
            }
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
