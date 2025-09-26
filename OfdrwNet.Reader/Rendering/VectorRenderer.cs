using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Globalization;
using System.Text;
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
                Rectangle bounds = Rectangle.Round(vectorObject.Boundary);

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
        private Task<bool> RenderPathAsync(VectorObject vectorObject, Graphics graphics, RenderContext renderContext)
        {
            if (string.IsNullOrEmpty(vectorObject.PathData))
                return Task.FromResult(false);

            try
            {
                // 使用简单路径解析器而不是复杂的PathData.Parse
                using var path = ParseSimplePath(vectorObject.PathData);

                if (path.PointCount == 0)
                {
                    // 如果路径为空，绘制边界框
                    graphics.DrawRectangle(Pens.Gray, Rectangle.Round(vectorObject.Boundary));
                    return Task.FromResult(true);
                }

                // 填充
                if (vectorObject.FillStyle != null)
                {
                    var fillBrush = CreateBrush(vectorObject.FillStyle);
                    graphics.FillPath(fillBrush, path);
                    fillBrush.Dispose();
                }

                // 描边 (默认黑色)
                using var strokePen = vectorObject.StrokeStyle != null ?
                    CreatePen(vectorObject.StrokeStyle) :
                    new Pen(Color.Black, 1.0f);
                graphics.DrawPath(strokePen, path);

                return Task.FromResult(true);
            }
            catch
            {
                // 解析失败，绘制边界框
                graphics.DrawRectangle(Pens.DarkGray, Rectangle.Round(vectorObject.Boundary));
                return Task.FromResult(false);
            }
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
        /// 尝试解析浮点数
        /// </summary>
        private bool TryParseFloat(string value, out float result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = 0f;
                return false;
            }

            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        /// <summary>
        /// 简单解析路径数据（支持 M L Z Q C A 命令）
        /// </summary>
        private GraphicsPath ParseSimplePath(string pathData)
        {
            var path = new GraphicsPath();

            try
            {
                // 预处理：将命令和数字分离
                var normalizedData = NormalizePathData(pathData);
                var tokens = normalizedData.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                var currentPoint = PointF.Empty;
                var startPoint = PointF.Empty;

                for (int i = 0; i < tokens.Length; i++)
                {
                    var token = tokens[i].Trim();
                    if (string.IsNullOrEmpty(token)) continue;

                    switch (token.ToUpper())
                    {
                        case "M": // MoveTo
                            if (i + 2 < tokens.Length &&
                                TryParseFloat(tokens[i + 1], out var mx) &&
                                TryParseFloat(tokens[i + 2], out var my))
                            {
                                currentPoint = new PointF(mx, my);
                                startPoint = currentPoint;
                                i += 2;
                            }
                            break;

                        case "L": // LineTo
                            if (i + 2 < tokens.Length &&
                                TryParseFloat(tokens[i + 1], out var lx) &&
                                TryParseFloat(tokens[i + 2], out var ly))
                            {
                                var endPoint = new PointF(lx, ly);
                                path.AddLine(currentPoint, endPoint);
                                currentPoint = endPoint;
                                i += 2;
                            }
                            break;

                        case "Q": // QuadraticBezierTo
                            if (i + 4 < tokens.Length &&
                                TryParseFloat(tokens[i + 1], out var qx1) &&
                                TryParseFloat(tokens[i + 2], out var qy1) &&
                                TryParseFloat(tokens[i + 3], out var qx2) &&
                                TryParseFloat(tokens[i + 4], out var qy2))
                            {
                                var control = new PointF(qx1, qy1);
                                var endPoint = new PointF(qx2, qy2);

                                // 转换二次贝塞尔为三次贝塞尔
                                var ctrl1 = new PointF(
                                    currentPoint.X + (control.X - currentPoint.X) * 2 / 3,
                                    currentPoint.Y + (control.Y - currentPoint.Y) * 2 / 3
                                );
                                var ctrl2 = new PointF(
                                    endPoint.X + (control.X - endPoint.X) * 2 / 3,
                                    endPoint.Y + (control.Y - endPoint.Y) * 2 / 3
                                );

                                path.AddBezier(currentPoint, ctrl1, ctrl2, endPoint);
                                currentPoint = endPoint;
                                i += 4;
                            }
                            break;

                        case "C": // CubicBezierTo
                            if (i + 6 < tokens.Length &&
                                TryParseFloat(tokens[i + 1], out var cx1) &&
                                TryParseFloat(tokens[i + 2], out var cy1) &&
                                TryParseFloat(tokens[i + 3], out var cx2) &&
                                TryParseFloat(tokens[i + 4], out var cy2) &&
                                TryParseFloat(tokens[i + 5], out var cx3) &&
                                TryParseFloat(tokens[i + 6], out var cy3))
                            {
                                var ctrl1 = new PointF(cx1, cy1);
                                var ctrl2 = new PointF(cx2, cy2);
                                var endPoint = new PointF(cx3, cy3);

                                path.AddBezier(currentPoint, ctrl1, ctrl2, endPoint);
                                currentPoint = endPoint;
                                i += 6;
                            }
                            break;

                        case "A": // Arc (简化版本，转换为椭圆弧)
                            if (i + 7 < tokens.Length &&
                                TryParseFloat(tokens[i + 1], out var rx) &&
                                TryParseFloat(tokens[i + 2], out var ry) &&
                                TryParseFloat(tokens[i + 3], out var angle) &&
                                TryParseFloat(tokens[i + 4], out var largeArc) &&
                                TryParseFloat(tokens[i + 5], out var sweep) &&
                                TryParseFloat(tokens[i + 6], out var ax) &&
                                TryParseFloat(tokens[i + 7], out var ay))
                            {
                                var endPoint = new PointF(ax, ay);

                                // 简单的弧线近似，使用直线代替
                                path.AddLine(currentPoint, endPoint);
                                currentPoint = endPoint;
                                i += 7;
                            }
                            break;

                        case "Z": // ClosePath
                            if (startPoint != PointF.Empty)
                            {
                                path.AddLine(currentPoint, startPoint);
                                currentPoint = startPoint;
                            }
                            break;
                    }
                }

                if (path.PointCount == 0)
                {
                    // 如果没有成功解析任何路径，创建一个简单的矩形
                    path.AddRectangle(new RectangleF(0, 0, 10, 10));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"路径解析错误: {ex.Message}");
                path.Dispose();
                path = new GraphicsPath();
                // 创建默认形状
                path.AddRectangle(new RectangleF(0, 0, 10, 10));
            }

            return path;
        }

        /// <summary>
        /// 规范化路径数据，确保命令和坐标正确分离
        /// </summary>
        private string NormalizePathData(string pathData)
        {
            if (string.IsNullOrEmpty(pathData))
                return "";

            var result = new StringBuilder();
            var chars = pathData.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];

                // 检查是否是命令字母
                if ("MLZQCAmzlqca".Contains(c))
                {
                    // 在命令前后添加空格
                    if (result.Length > 0 && result[result.Length - 1] != ' ')
                        result.Append(' ');
                    result.Append(c);
                    result.Append(' ');
                }
                else if (c == ',' || char.IsWhiteSpace(c))
                {
                    // 替换逗号和多个空格为单个空格
                    if (result.Length > 0 && result[result.Length - 1] != ' ')
                        result.Append(' ');
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString().Trim();
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
