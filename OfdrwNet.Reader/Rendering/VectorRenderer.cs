using System;
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
                return false;

            if (!vectorObject.Visible)
                return true;

            try
            {
                // 保存图形状态
                var state = graphics.Save();

                // 设置矢量渲染质量
                graphics.SmoothingMode = renderContext.SmoothingMode;
                graphics.CompositingQuality = renderContext.CompositingQuality;

                // 应用变换矩阵
                ApplyTransform(graphics, vectorObject, renderContext);

                // 根据矢量类型进行渲染
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

                // 恢复图形状态
                graphics.Restore(state);

                return result;
            }
            catch (Exception ex)
            {
                throw new RenderException(vectorObject.Id.ToString(), $"矢量图形渲染失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查矢量对象是否在指定点
        /// </summary>
        /// <param name="vectorObject">矢量对象</param>
        /// <param name="point">测试点</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>是否命中</returns>
        public async Task<bool> HitTestAsync(VectorObject vectorObject, Point point, RenderContext renderContext)
        {
            if (vectorObject?.Boundary == null)
                return false;

            var bounds = vectorObject.Boundary;

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

            // 基本边界框测试
            if (!bounds.Contains(point))
                return false;

            // 对于路径，进行精确命中测试
            if (vectorObject.VectorType == VectorType.Path && !string.IsNullOrEmpty(vectorObject.PathData))
            {
                try
                {
                    var pathData = Model.PathData.Parse(vectorObject.PathData);
                    var path = await GetGraphicsPathAsync(pathData);
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
        /// <param name="vectorObject">矢量对象</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>边界框</returns>
        public async Task<Rectangle> GetBoundsAsync(VectorObject vectorObject, RenderContext renderContext)
        {
            if (vectorObject?.Boundary != null)
            {
                var bounds = vectorObject.Boundary;

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

        // 私有渲染方法

        /// <summary>
        /// 渲染路径
        /// </summary>
        private async Task<bool> RenderPathAsync(VectorObject vectorObject, Graphics graphics, RenderContext renderContext)
        {
            if (string.IsNullOrEmpty(vectorObject.PathData))
                return false;

            var pathData = Model.PathData.Parse(vectorObject.PathData);
            var path = await GetGraphicsPathAsync(pathData);

            // 填充
            if (vectorObject.FillStyle != null)
            {
                var fillBrush = CreateBrush(vectorObject.FillStyle);
                graphics.FillPath(fillBrush, path);
                fillBrush.Dispose();
            }

            // 描边
            if (vectorObject.StrokeStyle != null)
            {
                var strokePen = CreatePen(vectorObject.StrokeStyle);
                graphics.DrawPath(strokePen, path);
                strokePen.Dispose();
            }

            return true;
        }

        /// <summary>
        /// 渲染直线
        /// </summary>
        private async Task<bool> RenderLineAsync(VectorObject vectorObject, Graphics graphics, RenderContext renderContext)
        {
            if (vectorObject.StrokeStyle == null || vectorObject.Points == null || vectorObject.Points.Count < 2)
                return false;

            await Task.Run(() =>
            {
                var pen = CreatePen(vectorObject.StrokeStyle);
                var startPoint = vectorObject.Points[0];
                var endPoint = vectorObject.Points[1];

                graphics.DrawLine(pen, startPoint, endPoint);
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
                // 填充
                if (vectorObject.FillStyle != null)
                {
                    var fillBrush = CreateBrush(vectorObject.FillStyle);
                    graphics.FillRectangle(fillBrush, rect);
                    fillBrush.Dispose();
                }

                // 描边
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
                // 填充
                if (vectorObject.FillStyle != null)
                {
                    var fillBrush = CreateBrush(vectorObject.FillStyle);
                    graphics.FillEllipse(fillBrush, rect);
                    fillBrush.Dispose();
                }

                // 描边
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
        private async Task<bool> RenderEllipseAsync(VectorObject vectorObject, Graphics graphics, RenderContext renderContext)
        {
            return await RenderCircleAsync(vectorObject, graphics, renderContext);
        }

        /// <summary>
        /// 渲染多边形
        /// </summary>
        private async Task<bool> RenderPolygonAsync(VectorObject vectorObject, Graphics graphics, RenderContext renderContext)
        {
            if (vectorObject.Points == null || vectorObject.Points.Count < 3)
                return false;

            await Task.Run(() =>
            {
                var points = vectorObject.Points.ToArray();

                // 填充
                if (vectorObject.FillStyle != null)
                {
                    var fillBrush = CreateBrush(vectorObject.FillStyle);
                    graphics.FillPolygon(fillBrush, points);
                    fillBrush.Dispose();
                }

                // 描边
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
                return false;

            await Task.Run(() =>
            {
                var pen = CreatePen(vectorObject.StrokeStyle);
                var points = vectorObject.Points.ToArray();
                graphics.DrawLines(pen, points);
                pen.Dispose();
            });

            return true;
        }

        // 辅助方法

        /// <summary>
        /// 应用变换矩阵
        /// </summary>
        private void ApplyTransform(Graphics graphics, VectorObject vectorObject, RenderContext renderContext)
        {
            // 应用渲染上下文的变换
            if (renderContext.TransformMatrix != null)
            {
                graphics.MultiplyTransform(renderContext.TransformMatrix);
            }

            // 应用对象的CTM变换
            if (vectorObject.CTM != null)
            {
                graphics.MultiplyTransform(vectorObject.CTM);
            }

            // 应用缩放
            if (renderContext.ScaleFactor != 1.0)
            {
                graphics.ScaleTransform((float)renderContext.ScaleFactor, (float)renderContext.ScaleFactor);
            }
        }

        /// <summary>
        /// 获取图形路径
        /// </summary>
        private async Task<GraphicsPath> GetGraphicsPathAsync(Model.PathData pathData)
        {
            var cacheKey = pathData.GetHashCode().ToString();

            if (_pathCache.TryGetPath(cacheKey, out var cachedPath) && cachedPath != null)
            {
                return (GraphicsPath)cachedPath.Clone();
            }

            var path = new GraphicsPath();

            await Task.Run(() =>
            {
                // 解析路径数据并构建GraphicsPath
                ParsePathData(pathData, path);
            });

            _pathCache.AddPath(cacheKey, path);
            return (GraphicsPath)path.Clone();
        }

        /// <summary>
        /// 解析路径数据
        /// </summary>
        private void ParsePathData(Model.PathData pathData, GraphicsPath path)
        {
            if (pathData.Commands == null)
                return;

            var currentPoint = PointF.Empty;

            foreach (var command in pathData.Commands)
            {
                switch (command.Type)
                {
                    case PathCommandType.MoveTo:
                        if (command.Points?.Count > 0)
                        {
                            currentPoint = command.Points[0];
                            path.StartFigure();
                        }
                        break;

                    case PathCommandType.LineTo:
                        if (command.Points?.Count > 0)
                        {
                            foreach (var point in command.Points)
                            {
                                path.AddLine(currentPoint, point);
                                currentPoint = point;
                            }
                        }
                        break;

                    case PathCommandType.CurveTo:
                        if (command.Points?.Count >= 3)
                        {
                            for (int i = 0; i <= command.Points.Count - 3; i += 3)
                            {
                                path.AddBezier(currentPoint, command.Points[i], command.Points[i + 1], command.Points[i + 2]);
                                currentPoint = command.Points[i + 2];
                            }
                        }
                        break;

                    case PathCommandType.ClosePath:
                        path.CloseFigure();
                        break;
                }
            }
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

            // 默认画刷
            return new SolidBrush(Color.Black);
        }

        /// <summary>
        /// 创建画笔
        /// </summary>
        private Pen CreatePen(StrokeStyle strokeStyle)
        {
            var color = strokeStyle.Color?.ToSystemColor() ?? Color.Black;
            var pen = new Pen(color, strokeStyle.Width);

            // 设置线条样式
            if (strokeStyle.DashArray != null && strokeStyle.DashArray.Count > 0)
            {
                pen.DashPattern = strokeStyle.DashArray.ToArray();
            }

            // 设置线条端点样式
            pen.StartCap = ConvertLineCap(strokeStyle.StartCap);
            pen.EndCap = ConvertLineCap(strokeStyle.EndCap);

            // 设置线条连接样式
            pen.LineJoin = ConvertLineJoin(strokeStyle.LineJoin);

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
                _pathCache?.Dispose();
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
