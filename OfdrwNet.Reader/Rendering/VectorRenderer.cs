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
                    System.Diagnostics.Trace.WriteLine($"[VectorRenderer] Graphics invalid for object {vectorObject.Id}: {gex.Message}");
                    return false;
                }

                GraphicsState? state = null;
                bool stateSaved = false;
                try
                {
                    state = graphics.Save();
                    stateSaved = true;
                }
                catch (ArgumentException)
                {
                    // Graphics.Save() can fail with "Parameter is not valid" in certain threading scenarios
                    // Fall back to rendering without save/restore
                    System.Diagnostics.Trace.WriteLine($"[VectorRenderer] Graphics.Save() failed for object {vectorObject.Id}, rendering without state save");
                }

                try
                {
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

                    return result;
                }
                finally
                {
                    if (stateSaved && state != null)
                    {
                        try
                        {
                            graphics.Restore(state);
                        }
                        catch (ArgumentException)
                        {
                            // Ignore restore failures - graphics might have been invalidated
                            System.Diagnostics.Trace.WriteLine($"[VectorRenderer] Graphics.Restore() failed for object {vectorObject.Id}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[VectorRenderer] Exception rendering object {vectorObject.Id}: {ex.Message}");
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
                using var path = await GetGraphicsPathAsync(vectorObject.PathData);

                if (path.PointCount == 0)
                {
                    graphics.DrawRectangle(Pens.Gray, Rectangle.Round(vectorObject.Boundary));
                    return true;
                }

                // If rendering pipeline skipped applying CTM to Graphics (non-unified mode), apply CTM directly to path
                try
                {
                    bool unified = IsUnified();
                    if (!unified)
                    {
                        // Build combined transform: translate by boundary then apply CTM if any (match OfdPageDrawer.CreateTransform)
                        var transform = new Matrix();
                        transform.Translate((float)vectorObject.Boundary.X, (float)vectorObject.Boundary.Y);
                        if (vectorObject.CTM != null)
                        {
                            try { transform.Multiply(vectorObject.CTM); } catch { }
                        }

                        // Clone to avoid mutating cached path
                        var tempPath = (GraphicsPath)path.Clone();
                        try
                        {
                            tempPath.Transform(transform);

                            // Then scale to pixels using renderContext.Ppm and ScaleFactor
                            var scale = (float)(renderContext.Ppm * renderContext.ScaleFactor);
                            if (Math.Abs(scale - 1f) > 1e-6)
                            {
                                using var scaleMatrix = new Matrix();
                                scaleMatrix.Scale(scale, scale);
                                tempPath.Transform(scaleMatrix);
                            }

                            // honor fill rule if provided
                            if (vectorObject.FillStyle != null && vectorObject.FillStyle.FillRule == FillRule.EvenOdd)
                                tempPath.FillMode = FillMode.Alternate; // Even-odd -> Alternate

                            var fillBrush = vectorObject.FillStyle != null ? CreateBrush(vectorObject.FillStyle) : null;
                            if (fillBrush != null)
                            {
                                graphics.FillPath(fillBrush, tempPath);
                                fillBrush.Dispose();
                            }

                            using var strokePen = vectorObject.StrokeStyle != null
                                ? CreatePen(vectorObject.StrokeStyle)
                                : new Pen(Color.Black, 1.0f);

                            graphics.DrawPath(strokePen, tempPath);
                        }
                        finally
                        {
                            tempPath.Dispose();
                            transform.Dispose();
                        }

                        return true;
                    }
                }
                catch (Exception)
                {
                    // fall through to normal drawing with original path
                }

                // honor fill rule if provided
                if (vectorObject.FillStyle != null && vectorObject.FillStyle.FillRule == FillRule.EvenOdd)
                    path.FillMode = FillMode.Alternate; // Even-odd -> Alternate

                if (vectorObject.FillStyle != null)
                {
                    var fillBrush = CreateBrush(vectorObject.FillStyle);
                    graphics.FillPath(fillBrush, path);
                    fillBrush.Dispose();
                }

                using var strokePen2 = vectorObject.StrokeStyle != null
                    ? CreatePen(vectorObject.StrokeStyle)
                    : new Pen(Color.Black, 1.0f);
                graphics.DrawPath(strokePen2, path);

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
                try
                {
                    var startPoint = vectorObject.Points[0];
                    var endPoint = vectorObject.Points[1];

                    // If not unified mode, apply object's CTM and boundary translation directly to points (in document units), then scale to pixels
                    bool unified = IsUnified();
                    if (!unified)
                    {
                        var pts = new PointF[] { startPoint, endPoint };
                        var m = new Matrix();
                        m.Translate((float)vectorObject.Boundary.X, (float)vectorObject.Boundary.Y);
                        if (vectorObject.CTM != null)
                        {
                            try { m.Multiply(vectorObject.CTM); } catch { }
                        }
                        try { m.TransformPoints(pts); } catch { }
                        // Scale to pixels
                        var scale = (float)(renderContext.Ppm * renderContext.ScaleFactor);
                        if (Math.Abs(scale - 1f) > 1e-6)
                        {
                            pts[0].X *= scale; pts[0].Y *= scale;
                            pts[1].X *= scale; pts[1].Y *= scale;
                        }
                        startPoint = pts[0];
                        endPoint = pts[1];
                        m.Dispose();
                    }

                    graphics.DrawLine(pen, startPoint, endPoint);
                }
                finally
                {
                    pen.Dispose();
                }
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
                // If non-unified, handled above
                bool unified = IsUnified();
                if (!unified && vectorObject.CTM != null)
                {
                    using var path = new GraphicsPath();
                    path.AddRectangle(rect);
                    var transform = new Matrix();
                    transform.Translate((float)vectorObject.Boundary.X, (float)vectorObject.Boundary.Y);
                    if (vectorObject.CTM != null)
                    {
                        try { transform.Multiply(vectorObject.CTM); } catch { }
                    }
                    try { path.Transform(transform); } catch { }
                    // scale to pixels
                    var scale = (float)(renderContext.Ppm * renderContext.ScaleFactor);
                    if (Math.Abs(scale - 1f) > 1e-6)
                    {
                        using var s = new Matrix(); s.Scale(scale, scale); path.Transform(s);
                    }

                    if (vectorObject.FillStyle != null)
                    {
                        var fillBrush = CreateBrush(vectorObject.FillStyle);
                        graphics.FillPath(fillBrush, path);
                        fillBrush.Dispose();
                    }

                    if (vectorObject.StrokeStyle != null)
                    {
                        using var strokePen = CreatePen(vectorObject.StrokeStyle);
                        graphics.DrawPath(strokePen, path);
                    }

                    transform.Dispose();
                    return;
                }

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
                bool unified = IsUnified();
                if (!unified && vectorObject.CTM != null)
                {
                    using var path = new GraphicsPath();
                    path.AddEllipse(rect);
                    var transform = new Matrix();
                    transform.Translate((float)vectorObject.Boundary.X, (float)vectorObject.Boundary.Y);
                    if (vectorObject.CTM != null)
                    {
                        try { transform.Multiply(vectorObject.CTM); } catch { }
                    }
                    try { path.Transform(transform); } catch { }
                    // scale to pixels
                    var scale = (float)(renderContext.Ppm * renderContext.ScaleFactor);
                    if (Math.Abs(scale - 1f) > 1e-6)
                    {
                        using var s = new Matrix(); s.Scale(scale, scale); path.Transform(s);
                    }

                    if (vectorObject.FillStyle != null)
                    {
                        var fillBrush = CreateBrush(vectorObject.FillStyle);
                        graphics.FillPath(fillBrush, path);
                        fillBrush.Dispose();
                    }

                    if (vectorObject.StrokeStyle != null)
                    {
                        using var strokePen = CreatePen(vectorObject.StrokeStyle);
                        graphics.DrawPath(strokePen, path);
                    }

                    transform.Dispose();
                    return;
                }

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
        /// 渲染椭圆（别名，转发到圆形渲染实现）
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

                bool unified = IsUnified();
                if (!unified)
                {
                    var m = new Matrix();
                    m.Translate((float)vectorObject.Boundary.X, (float)vectorObject.Boundary.Y);
                    if (vectorObject.CTM != null)
                    {
                        try { m.Multiply(vectorObject.CTM); } catch { }
                    }
                    try { m.TransformPoints(points); } catch { }
                    var scale = (float)(renderContext.Ppm * renderContext.ScaleFactor);
                    if (Math.Abs(scale - 1f) > 1e-6)
                    {
                        for (int i = 0; i < points.Length; i++)
                        {
                            points[i].X *= scale; points[i].Y *= scale;
                        }
                    }
                    m.Dispose();
                }

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
                try
                {
                    var points = vectorObject.Points.ToArray();
                    bool unified = IsUnified();
                    if (!unified)
                    {
                        var m = new Matrix();
                        m.Translate((float)vectorObject.Boundary.X, (float)vectorObject.Boundary.Y);
                        if (vectorObject.CTM != null)
                        {
                            try { m.Multiply(vectorObject.CTM); } catch { }
                        }
                        try { m.TransformPoints(points); } catch { }
                        var scale = (float)(renderContext.Ppm * renderContext.ScaleFactor);
                        if (Math.Abs(scale - 1f) > 1e-6)
                        {
                            for (int i = 0; i < points.Length; i++)
                            {
                                points[i].X *= scale; points[i].Y *= scale;
                            }
                        }
                        m.Dispose();
                    }

                    graphics.DrawLines(pen, points);
                }
                finally
                {
                    pen.Dispose();
                }
            });

            return true;
        }

        /// <summary>
        /// 应用变换矩阵
        /// </summary>
        private void ApplyTransform(Graphics graphics, VectorObject vectorObject, RenderContext renderContext)
        {
            bool unified = IsUnified();

            // In unified mode we apply the object's boundary translation first so local coordinates map to page
            if (unified)
            {
                // translate by object's boundary (paths are defined in local coordinates)
                if (vectorObject?.Boundary != null)
                {
                    graphics.TranslateTransform((float)vectorObject.Boundary.X, (float)vectorObject.Boundary.Y);
                }

                // Note: renderContext.TransformMatrix (viewport/global transforms) is applied once by the caller (RenderingEngine)
                // Avoid reapplying it here to prevent double transform.
                if (vectorObject.CTM != null)
                {
                    graphics.MultiplyTransform(vectorObject.CTM);
                }
            }
            else
            {
                // Non-unified (pixel mode): render methods apply boundary+CTM per-primitive to avoid altering Graphics state globally
                if (renderContext.TransformMatrix != null)
                {
                    graphics.MultiplyTransform(renderContext.TransformMatrix);
                }

                if (vectorObject.CTM != null)
                {
                    System.Diagnostics.Trace.WriteLine("[VectorRenderer][DEBUG] Skip CTM in pixel mode (handled per-primitive)");
                }
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
