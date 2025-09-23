using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.PageDescription.DrawParam;
using OfdrwNet.Core.PageDescription.Color;
using OfdrwNet.Core.Graph.PathObj;
using OfdrwNet.Reader.Model;
using OfdrwNet.Reader.Rendering;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// OFD 页面绘制器
    /// </summary>
    public class OfdPageDrawer : IDisposable
    {
        private readonly OfdReader _reader;
        private readonly Dictionary<long, System.Drawing.Font> _fontCache = new Dictionary<long, System.Drawing.Font>();
        private readonly ResourceLocator _resourceLocator;

        // ===== T028: 新增高级渲染功能和性能优化属性 =====

        /// <summary>
        /// 渲染质量设置
        /// </summary>
        public RenderQuality RenderQuality { get; set; } = RenderQuality.Medium;

        /// <summary>
        /// 是否启用性能监控
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = false;

        /// <summary>
        /// 渲染统计信息
        /// </summary>
        public RenderStatistics Statistics { get; private set; } = new RenderStatistics();

        /// <summary>
        /// 渲染缓存
        /// </summary>
        private readonly Dictionary<string, object> _renderCache = new Dictionary<string, object>();

        /// <summary>
        /// 背景渲染任务集合
        /// </summary>
        private readonly List<Task> _backgroundTasks = new List<Task>();

        /// <summary>
        /// 取消令牌源
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// 每毫米像素数量(Pixels per millimeter)
        /// 默认为: 7.874015748031496 ppm (约200 dpi)
        /// </summary>
        public double Ppm { get; set; } = 7.874015748031496;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="reader">OFD阅读器</param>
        public OfdPageDrawer(OfdReader reader)
        {
            _reader = reader;
            _resourceLocator = reader.GetResourceLocator();
        }

        /// <summary>
        /// 绘制单个页面到图片
        /// </summary>
        /// <param name="pageNum">页码，从 1 开始</param>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        /// <returns>绘制的页面图片</returns>
        public Bitmap DrawPageToBitmap(int pageNum, int width = 800, int height = 600)
        {
            var bitmap = new Bitmap(width, height);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                // 设置高质量渲染
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // 清空背景
                g.Clear(Color.White);

                // 绘制页面内容
                DrawPage(g, pageNum);
            }
            return bitmap;
        }

        /// <summary>
        /// 绘制页面内容
        /// </summary>
        /// <param name="g">图形对象</param>
        /// <param name="pageNum">页码</param>
        private void DrawPage(System.Drawing.Graphics g, int pageNum)
        {
            try
            {
                var pageInfo = _reader.GetPageInfo(pageNum);
                if (pageInfo?.Obj == null)
                    return;

                // 获取页面所有图层（包含模板页）
                var layers = pageInfo.GetAllLayers();
                foreach (var layer in layers)
                {
                    DrawLayer(g, layer);
                }
            }
            catch (Exception ex)
            {
                // 如果出错，在页面上显示错误信息
                g.DrawString($"绘制页面{pageNum}时出错: {ex.Message}",
                    new System.Drawing.Font("Arial", 12), Brushes.Red, 10, 10);
            }
        }

        /// <summary>
        /// 绘制图层内容
        /// </summary>
        private void DrawLayer(System.Drawing.Graphics g, XElement layerElement)
        {
            if (layerElement == null) return;

            try
            {
                // 处理图层的绘制参数
                var drawParamId = layerElement.Attribute("DrawParam")?.Value;
                List<XElement> drawParams = new List<XElement>();

                if (!string.IsNullOrEmpty(drawParamId))
                {
                    // 获取绘制参数
                    var drawParam = GetDrawParam(drawParamId);
                    if (drawParam != null)
                    {
                        drawParams.Add(drawParam);
                    }
                }

                // 获取图层边界
                var boundary = ParseBoundary(layerElement.Attribute("Boundary")?.Value);
                var transform = CreateTransform(boundary);

                var state = g.Save();
                if (transform != null)
                {
                    g.MultiplyTransform(transform);
                }

                try
                {
                    // 绘制所有页面对象
                    DrawPageBlocks(g, layerElement.Elements(), drawParams);
                }
                finally
                {
                    g.Restore(state);
                }
            }
            catch (Exception ex)
            {
                g.DrawString($"图层绘制错误: {ex.Message}",
                    new System.Drawing.Font("Arial", 10), Brushes.Red, 10, 50);
            }
        }

        /// <summary>
        /// 绘制页面块对象
        /// </summary>
        private void DrawPageBlocks(System.Drawing.Graphics g, IEnumerable<XElement> elements, List<XElement> drawParams)
        {
            foreach (var element in elements)
            {
                try
                {
                    switch (element.Name.LocalName)
                    {
                        case "TextObject":
                            DrawTextObject(g, element, drawParams);
                            break;
                        case "PathObject":
                            DrawPathObject(g, element, drawParams);
                            break;
                        case "ImageObject":
                            DrawImageObject(g, element, drawParams);
                            break;
                        case "CompositeObject":
                            DrawCompositeObject(g, element, drawParams);
                            break;
                        default:
                            // 递归处理子元素
                            DrawPageBlocks(g, element.Elements(), drawParams);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // 单个对象绘制失败不影响其他对象
                    System.Diagnostics.Debug.WriteLine($"绘制对象 {element.Name.LocalName} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 绘制路径对象
        /// </summary>
        private void DrawPathObject(System.Drawing.Graphics g, XElement pathElement, List<XElement> drawParams)
        {
            try
            {
                // 获取路径数据
                var abbreviatedData = pathElement.Element("AbbreviatedData")?.Value;
                if (string.IsNullOrEmpty(abbreviatedData)) return;

                // 解析路径
                var path = ParsePath(abbreviatedData);
                if (path == null) return;

                // 获取边界和变换
                var boundary = ParseBoundary(pathElement.Attribute("Boundary")?.Value);
                var ctm = ParseCtm(pathElement.Attribute("CTM")?.Value);
                var transform = CreateTransform(boundary, ctm);

                var state = g.Save();
                try
                {
                    if (transform != null)
                    {
                        g.MultiplyTransform(transform);
                    }
                    g.ScaleTransform((float)Ppm, (float)Ppm);

                    // 获取绘制属性
                    var strokeEnabled = ParseBoolAttribute(pathElement, "Stroke", true);
                    var fillEnabled = ParseBoolAttribute(pathElement, "Fill", false);
                    var lineWidth = ParseDoubleAttribute(pathElement, "LineWidth", 0.4);
                    var alpha = ParseIntAttribute(pathElement, "Alpha", 255);

                    // 设置透明度
                    if (alpha < 255)
                    {
                        var alphaFloat = alpha / 255.0f;
                        g.CompositingMode = CompositingMode.SourceOver;
                        // 这里可以设置整体透明度，但System.Drawing.Graphics没有直接的Alpha设置
                        // 可以考虑使用ColorMatrix或者其他方法
                    }

                    // 绘制填充
                    if (fillEnabled)
                    {
                        var fillColor = GetFillColor(pathElement, drawParams);
                        if (fillColor != null)
                        {
                            using (var brush = new SolidBrush(fillColor.Value))
                            {
                                g.FillPath(brush, path);
                            }
                        }
                    }

                    // 绘制描边
                    if (strokeEnabled)
                    {
                        var strokeColor = GetStrokeColor(pathElement, drawParams);
                        if (strokeColor != null)
                        {
                            using (var pen = new Pen(strokeColor.Value, (float)lineWidth))
                            {
                                SetPenStyle(pen, pathElement);
                                g.DrawPath(pen, path);
                            }
                        }
                    }
                }
                finally
                {
                    g.Restore(state);
                    path.Dispose();
                }
            }
            catch (Exception ex)
            {
                g.DrawString($"路径绘制错误: {ex.Message}",
                    new System.Drawing.Font("Arial", 10), Brushes.Red, 10, 150);
            }
        }

        /// <summary>
        /// 绘制图像对象
        /// </summary>
        private void DrawImageObject(System.Drawing.Graphics g, XElement imageElement, List<XElement> drawParams)
        {
            try
            {
                // 获取资源ID
                var resourceId = imageElement.Attribute("ResourceID")?.Value;
                if (string.IsNullOrEmpty(resourceId)) return;

                // 加载图像
                var image = LoadImage(resourceId);
                if (image == null) return;

                // 获取边界和变换
                var boundary = ParseBoundary(imageElement.Attribute("Boundary")?.Value);
                var ctm = ParseCtm(imageElement.Attribute("CTM")?.Value);
                var alpha = ParseIntAttribute(imageElement, "Alpha", 255);

                var state = g.Save();
                try
                {
                    // 创建变换矩阵
                    var transform = CreateImageTransform(image, boundary, ctm);
                    if (transform != null)
                    {
                        g.MultiplyTransform(transform);
                    }

                    // 设置透明度
                    ImageAttributes? imageAttr = null;
                    if (alpha < 255)
                    {
                        imageAttr = new ImageAttributes();
                        var colorMatrix = new ColorMatrix();
                        colorMatrix.Matrix33 = alpha / 255.0f; // Alpha通道
                        imageAttr.SetColorMatrix(colorMatrix);
                    }

                    // 绘制图像
                    var destRect = new Rectangle(0, 0, image.Width, image.Height);
                    g.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttr);

                    imageAttr?.Dispose();
                }
                finally
                {
                    g.Restore(state);
                }
            }
            catch (Exception ex)
            {
                g.DrawString($"图像绘制错误: {ex.Message}",
                    new System.Drawing.Font("Arial", 10), Brushes.Red, 10, 200);
            }
        }

        /// <summary>
        /// 绘制复合对象
        /// </summary>
        private void DrawCompositeObject(System.Drawing.Graphics g, XElement compositeElement, List<XElement> drawParams)
        {
            try
            {
                // 获取资源ID
                var resourceId = compositeElement.Attribute("ResourceID")?.Value;
                if (string.IsNullOrEmpty(resourceId)) return;

                // 获取复合图形单元
                var vectorG = GetCompositeGraphicUnit(resourceId);
                if (vectorG == null) return;

                // 获取边界和变换
                var boundary = ParseBoundary(compositeElement.Attribute("Boundary")?.Value);
                var ctm = ParseCtm(compositeElement.Attribute("CTM")?.Value);
                var transform = CreateTransform(boundary, ctm);

                var state = g.Save();
                try
                {
                    if (transform != null)
                    {
                        g.MultiplyTransform(transform);
                    }

                    // 递归绘制复合对象的内容
                    var contentElement = vectorG.Element("Content");
                    if (contentElement != null)
                    {
                        DrawPageBlocks(g, contentElement.Elements(), drawParams);
                    }
                }
                finally
                {
                    g.Restore(state);
                }
            }
            catch (Exception ex)
            {
                g.DrawString($"复合对象绘制错误: {ex.Message}",
                    new System.Drawing.Font("Arial", 10), Brushes.Red, 10, 250);
            }
        }

        /// <summary>
        /// 绘制文本对象（改进版本）
        /// </summary>
        private void DrawTextObject(System.Drawing.Graphics g, XElement textObjectElement, List<XElement> drawParams)
        {
            try
            {
                // 获取文本内容
                var textCodes = textObjectElement.Elements("TextCode");
                if (!textCodes.Any()) return;

                // 获取字体大小
                var fontSize = ParseDoubleAttribute(textObjectElement, "Size", 12.0);
                var alpha = ParseIntAttribute(textObjectElement, "Alpha", 255);

                // 获取边界和变换
                var boundary = ParseBoundary(textObjectElement.Attribute("Boundary")?.Value);
                var ctm = ParseCtm(textObjectElement.Attribute("CTM")?.Value);
                var transform = CreateTransform(boundary, ctm);

                var state = g.Save();
                try
                {
                    if (transform != null)
                    {
                        g.MultiplyTransform(transform);
                    }
                    g.ScaleTransform((float)Ppm, (float)Ppm);

                    // 获取字体
                    var fontId = textObjectElement.Attribute("Font")?.Value;
                    var font = GetFont(fontId, (float)fontSize);

                    // 获取文本颜色
                    var fillColor = GetFillColor(textObjectElement, drawParams) ?? Color.Black;
                    var strokeColor = GetStrokeColor(textObjectElement, drawParams);

                    // 设置透明度
                    if (alpha < 255)
                    {
                        var alphaFloat = alpha / 255.0f;
                        fillColor = Color.FromArgb((int)(alpha), fillColor.R, fillColor.G, fillColor.B);
                        if (strokeColor.HasValue)
                        {
                            strokeColor = Color.FromArgb((int)(alpha), strokeColor.Value.R, strokeColor.Value.G, strokeColor.Value.B);
                        }
                    }

                    // 绘制文本
                    float currentY = 0;
                    foreach (var textCode in textCodes)
                    {
                        var text = textCode.Value?.Trim();
                        if (!string.IsNullOrEmpty(text))
                        {
                            var x = ParseDoubleAttribute(textCode, "X", 0.0);
                            var y = ParseDoubleAttribute(textCode, "Y", currentY);

                            using (var brush = new SolidBrush(fillColor))
                            {
                                g.DrawString(text, font, brush, (float)x, (float)y);
                            }

                            // 如果有描边色，绘制文本描边
                            if (strokeColor.HasValue)
                            {
                                var lineWidth = ParseDoubleAttribute(textObjectElement, "LineWidth", 0.1);
                                using (var pen = new Pen(strokeColor.Value, (float)lineWidth))
                                {
                                    // 这里可以实现文本描边，但System.Drawing没有直接支持
                                    // 可以考虑使用GraphicsPath来实现
                                }
                            }

                            currentY = (float)(y + fontSize + 2); // 行间距
                        }
                    }
                }
                finally
                {
                    g.Restore(state);
                }
            }
            catch (Exception ex)
            {
                g.DrawString($"文本绘制错误: {ex.Message}",
                    new System.Drawing.Font("Arial", 10), Brushes.Red, 10, 100);
            }
        }

        #region 辅助方法

        /// <summary>
        /// 解析边界框
        /// </summary>
        private RectangleF? ParseBoundary(string? boundaryStr)
        {
            if (string.IsNullOrEmpty(boundaryStr)) return null;

            var parts = boundaryStr.Split(' ');
            if (parts.Length >= 4 &&
                float.TryParse(parts[0], out var x) &&
                float.TryParse(parts[1], out var y) &&
                float.TryParse(parts[2], out var w) &&
                float.TryParse(parts[3], out var h))
            {
                return new RectangleF(x, y, w, h);
            }
            return null;
        }

        /// <summary>
        /// 解析CTM变换矩阵
        /// </summary>
        private Matrix? ParseCtm(string? ctmStr)
        {
            if (string.IsNullOrEmpty(ctmStr)) return null;

            var parts = ctmStr.Split(' ');
            if (parts.Length >= 6 &&
                float.TryParse(parts[0], out var m11) &&
                float.TryParse(parts[1], out var m12) &&
                float.TryParse(parts[2], out var m21) &&
                float.TryParse(parts[3], out var m22) &&
                float.TryParse(parts[4], out var dx) &&
                float.TryParse(parts[5], out var dy))
            {
                return new Matrix(m11, m12, m21, m22, dx, dy);
            }
            return null;
        }

        /// <summary>
        /// 创建变换矩阵
        /// </summary>
        private Matrix? CreateTransform(RectangleF? boundary, Matrix? ctm = null)
        {
            Matrix? result = null;

            if (boundary.HasValue)
            {
                result = new Matrix();
                result.Translate(boundary.Value.X, boundary.Value.Y);
            }

            if (ctm != null)
            {
                if (result == null)
                    result = ctm.Clone();
                else
                    result.Multiply(ctm);
            }

            return result;
        }

        /// <summary>
        /// 为图像创建变换矩阵
        /// </summary>
        private Matrix? CreateImageTransform(Image image, RectangleF? boundary, Matrix? ctm = null)
        {
            var result = new Matrix();

            // 缩放图像到目标尺寸
            if (boundary.HasValue)
            {
                var scaleX = boundary.Value.Width / image.Width;
                var scaleY = boundary.Value.Height / image.Height;
                result.Scale(scaleX, scaleY);
                result.Translate(boundary.Value.X / scaleX, boundary.Value.Y / scaleY);
            }

            // 应用CTM变换
            if (ctm != null)
            {
                result.Multiply(ctm);
            }

            // 应用PPM缩放
            result.Scale((float)Ppm, (float)Ppm);

            return result;
        }

        /// <summary>
        /// 解析路径数据
        /// </summary>
        private GraphicsPath? ParsePath(string abbreviatedData)
        {
            try
            {
                var path = new GraphicsPath();
                var optValues = AbbreviatedData.Parse(abbreviatedData);
                PointF currentPoint = new PointF(0, 0);

                foreach (var optVal in optValues)
                {
                    var values = optVal.ExpectValues();
                    switch (optVal.Opt.ToUpper())
                    {
                        case "M":
                        case "S":
                            if (values.Length >= 2)
                            {
                                currentPoint = new PointF((float)values[0], (float)values[1]);
                                path.StartFigure();
                            }
                            break;
                        case "L":
                            if (values.Length >= 2)
                            {
                                var endPoint = new PointF((float)values[0], (float)values[1]);
                                path.AddLine(currentPoint, endPoint);
                                currentPoint = endPoint;
                            }
                            break;
                        case "Q":
                            if (values.Length >= 4)
                            {
                                // System.Drawing不直接支持二次贝塞尔曲线，这里简化为直线
                                var endPoint = new PointF((float)values[2], (float)values[3]);
                                path.AddLine(currentPoint, endPoint);
                                currentPoint = endPoint;
                            }
                            break;
                        case "B":
                            if (values.Length >= 6)
                            {
                                var controlPoint1 = new PointF((float)values[0], (float)values[1]);
                                var controlPoint2 = new PointF((float)values[2], (float)values[3]);
                                var endPoint = new PointF((float)values[4], (float)values[5]);
                                path.AddBezier(currentPoint, controlPoint1, controlPoint2, endPoint);
                                currentPoint = endPoint;
                            }
                            break;
                        case "C":
                            path.CloseFigure();
                            break;
                        case "A":
                            // 弧线绘制较复杂，这里暂时跳过
                            break;
                    }
                }

                return path;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 设置画笔样式
        /// </summary>
        private void SetPenStyle(Pen pen, XElement pathElement)
        {
            // 设置虚线模式
            var dashPattern = GetDashPattern(pathElement);
            if (dashPattern != null && dashPattern.Length > 0)
            {
                pen.DashPattern = dashPattern.Select(d => (float)d).ToArray();
            }

            // 设置线端样式
            var capStyle = GetLineCap(pathElement);
            if (capStyle.HasValue)
            {
                pen.StartCap = capStyle.Value;
                pen.EndCap = capStyle.Value;
            }

            // 设置连接样式
            var joinStyle = GetLineJoin(pathElement);
            if (joinStyle.HasValue)
            {
                pen.LineJoin = joinStyle.Value;
            }
        }

        /// <summary>
        /// 解析布尔属性
        /// </summary>
        private bool ParseBoolAttribute(XElement element, string name, bool defaultValue)
        {
            var value = element.Attribute(name)?.Value;
            return string.IsNullOrEmpty(value) ? defaultValue : bool.Parse(value);
        }

        /// <summary>
        /// 解析双精度属性
        /// </summary>
        private double ParseDoubleAttribute(XElement element, string name, double defaultValue)
        {
            var value = element.Attribute(name)?.Value;
            return string.IsNullOrEmpty(value) ? defaultValue : double.Parse(value);
        }

        /// <summary>
        /// 解析整数属性
        /// </summary>
        private int ParseIntAttribute(XElement element, string name, int defaultValue)
        {
            var value = element.Attribute(name)?.Value;
            return string.IsNullOrEmpty(value) ? defaultValue : int.Parse(value);
        }

        #endregion

        #region 资源获取方法

        private XElement? GetDrawParam(string id)
        {
            // TODO: 实现从资源管理器获取绘制参数
            return null;
        }

        private Color? GetFillColor(XElement element, List<XElement> drawParams)
        {
            // TODO: 实现填充颜色解析
            return Color.Black;
        }

        private Color? GetStrokeColor(XElement element, List<XElement> drawParams)
        {
            // TODO: 实现描边颜色解析
            return null;
        }

        private System.Drawing.Font GetFont(string? fontId, float size)
        {
            // TODO: 实现字体加载
            return new System.Drawing.Font("SimSun", size, FontStyle.Regular);
        }

        private Image? LoadImage(string resourceId)
        {
            // TODO: 实现图像资源加载
            return null;
        }

        private XElement? GetCompositeGraphicUnit(string resourceId)
        {
            // TODO: 实现复合图形单元获取
            return null;
        }

        private double[]? GetDashPattern(XElement element)
        {
            // TODO: 实现虚线模式解析
            return null;
        }

        private LineCap? GetLineCap(XElement element)
        {
            // TODO: 实现线端样式解析
            return null;
        }

        private System.Drawing.Drawing2D.LineJoin? GetLineJoin(XElement element)
        {
            // TODO: 实现连接样式解析
            return null;
        }

        // ===== T028: 新增高级渲染功能和性能优化方法 =====

        /// <summary>
        /// 异步渲染页面到位图
        /// </summary>
        /// <param name="pageNum">页码</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>渲染的位图</returns>
        public async Task<Bitmap> DrawPageToBitmapAsync(int pageNum, int width = 800, int height = 600,
            RenderContext? renderContext = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

            try
            {
                var bitmap = await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (renderContext != null)
                    {
                        ApplyRenderContext(renderContext);
                    }

                    return DrawPageToBitmap(pageNum, width, height);
                }, cancellationToken);

                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    Statistics.RecordRenderTime(stopwatch.Elapsed);
                    Statistics.IncrementRenderedPages();
                }

                return bitmap;
            }
            catch (OperationCanceledException)
            {
                Statistics.IncrementCancelledRenders();
                throw;
            }
            catch (Exception ex)
            {
                Statistics.IncrementFailedRenders();
                throw new RenderException($"页面{pageNum}", $"异步渲染失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 渲染页面到指定的Graphics对象
        /// </summary>
        /// <param name="pageNum">页码</param>
        /// <param name="graphics">Graphics对象</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>渲染结果</returns>
        public async Task<RenderResult> RenderPageToGraphicsAsync(int pageNum, Graphics graphics, RenderContext renderContext)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new RenderResult { Success = true };

            try
            {
                ApplyRenderContext(renderContext);

                // 应用图形质量设置
                ApplyRenderQuality(graphics);

                // 获取页面信息
                var pageInfo = _reader.GetPageInfo(pageNum);
                if (pageInfo == null)
                {
                    result.Success = false;
                    result.Errors.Add(new Rendering.RenderError
                    {
                        ObjectId = $"page_{pageNum}",
                        Type = Rendering.RenderErrorType.ObjectNotFound,
                        Message = $"找不到页面 {pageNum}"
                    });
                    return result;
                }

                // 执行渲染
                await RenderPageContentAsync(pageInfo, graphics, renderContext);

                result.ObjectsRendered = CountPageObjects(pageInfo);

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;

                Statistics.RecordRenderTime(stopwatch.Elapsed);
                Statistics.IncrementRenderedPages();

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.Duration = stopwatch.Elapsed;
                result.Errors.Add(new Rendering.RenderError
                {
                    ObjectId = $"page_{pageNum}",
                    Type = Rendering.RenderErrorType.RenderingFailed,
                    Message = ex.Message,
                    Exception = ex
                });

                Statistics.IncrementFailedRenders();
                return result;
            }
        }

        /// <summary>
        /// 生成页面缩略图
        /// </summary>
        /// <param name="pageNum">页码</param>
        /// <param name="thumbnailSize">缩略图尺寸</param>
        /// <param name="quality">质量设置</param>
        /// <returns>缩略图位图</returns>
        public async Task<Bitmap> GenerateThumbnailAsync(int pageNum, Size thumbnailSize, RenderQuality quality = RenderQuality.Low)
        {
            var originalQuality = RenderQuality;
            RenderQuality = quality; // 缩略图使用低质量渲染以提高性能

            try
            {
                var renderContext = RenderContext.CreateFast();
                renderContext.ScaleFactor = Math.Min(
                    (double)thumbnailSize.Width / 210,  // A4宽度210mm
                    (double)thumbnailSize.Height / 297  // A4高度297mm
                );

                return await DrawPageToBitmapAsync(pageNum, thumbnailSize.Width, thumbnailSize.Height, renderContext);
            }
            finally
            {
                RenderQuality = originalQuality;
            }
        }

        /// <summary>
        /// 批量预渲染页面
        /// </summary>
        /// <param name="pageNumbers">页码列表</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <param name="maxConcurrency">最大并发数</param>
        /// <returns>预渲染任务</returns>
        public async Task PreRenderPagesAsync(IEnumerable<int> pageNumbers, RenderContext renderContext, int maxConcurrency = 4)
        {
            var semaphore = new SemaphoreSlim(maxConcurrency);
            var tasks = pageNumbers.Select(async pageNum =>
            {
                await semaphore.WaitAsync(_cancellationTokenSource.Token);
                try
                {
                    var cacheKey = $"prerender_page_{pageNum}_{renderContext.ScaleFactor}";
                    if (!_renderCache.ContainsKey(cacheKey))
                    {
                        var bitmap = await DrawPageToBitmapAsync(pageNum, 800, 600, renderContext, _cancellationTokenSource.Token);
                        _renderCache[cacheKey] = bitmap;
                    }
                }
                catch (OperationCanceledException)
                {
                    // 忽略取消异常
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 清理渲染缓存
        /// </summary>
        /// <param name="olderThan">清理早于指定时间的缓存</param>
        public void ClearRenderCache(TimeSpan? olderThan = null)
        {
            if (olderThan.HasValue)
            {
                // 这里简化实现，实际应该跟踪缓存时间
                var keysToRemove = _renderCache.Keys.Take(_renderCache.Count / 2).ToList();
                foreach (var key in keysToRemove)
                {
                    if (_renderCache[key] is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    _renderCache.Remove(key);
                }
            }
            else
            {
                foreach (var value in _renderCache.Values)
                {
                    if (value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _renderCache.Clear();
            }
        }

        /// <summary>
        /// 获取渲染统计信息
        /// </summary>
        /// <returns>统计信息摘要</returns>
        public string GetStatisticsSummary()
        {
            return Statistics.GetSummary();
        }

        /// <summary>
        /// 应用渲染上下文设置
        /// </summary>
        private void ApplyRenderContext(RenderContext renderContext)
        {
            if (renderContext.DpiX > 0)
            {
                Ppm = renderContext.DpiX / 25.4; // 转换DPI到PPM
            }
        }

        /// <summary>
        /// 应用渲染质量设置
        /// </summary>
        private void ApplyRenderQuality(Graphics graphics)
        {
            switch (RenderQuality)
            {
                case RenderQuality.Low:
                    graphics.SmoothingMode = SmoothingMode.HighSpeed;
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
                    graphics.InterpolationMode = InterpolationMode.Low;
                    break;
                case RenderQuality.Medium:
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    graphics.InterpolationMode = InterpolationMode.Bilinear;
                    break;
                case RenderQuality.High:
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    break;
                case RenderQuality.Print:
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    break;
            }
        }

        /// <summary>
        /// 异步渲染页面内容
        /// </summary>
        private async Task RenderPageContentAsync(PageInfo pageInfo, Graphics graphics, RenderContext renderContext)
        {
            // 简化实现，实际应该解析页面内容对象
            await Task.Run(() =>
            {
                // 这里可以调用现有的同步渲染逻辑
                // 或者实现新的异步渲染逻辑
            });
        }

        /// <summary>
        /// 计算页面对象数量
        /// </summary>
        private int CountPageObjects(PageInfo pageInfo)
        {
            return pageInfo.ContentObjects?.Count ?? 0;
        }

        /// <summary>
        /// 取消所有后台任务
        /// </summary>
        public void CancelBackgroundTasks()
        {
            _cancellationTokenSource.Cancel();
            Task.WaitAll(_backgroundTasks.ToArray(), TimeSpan.FromSeconds(5));
            _backgroundTasks.Clear();

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        #endregion

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // T028: 增强的资源清理
            CancelBackgroundTasks();
            ClearRenderCache();

            foreach (var font in _fontCache.Values)
            {
                font.Dispose();
            }
            _fontCache.Clear();

            _cancellationTokenSource?.Dispose();
        }
    }
}
