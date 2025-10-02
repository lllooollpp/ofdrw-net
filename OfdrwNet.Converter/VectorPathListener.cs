using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions;
using iText.Kernel.Geom;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Canvas;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OfdrwNet.Converter;

/// <summary>
/// 监听PDF中的路径渲染事件，将路径转换为OFD PathObject
/// </summary>
public class VectorPathListener : IEventListener
{
    private readonly ILogger? _logger;
    private readonly List<OfdPath> _paths = new();
    private readonly double _pageHeightPt; // 用于坐标系转换（PDF坐标系，单位：pt）

    // 当前图形状态（初始化为黑色描边、透明填充，避免未赋值警告；真实值待 UpdateGraphicsState 后续完善）
    private Color? _currentStrokeColor = ColorConstants.BLACK; // 默认描边色
    private Color? _currentFillColor = new DeviceRgb(0, 0, 0) { }; // 先占位，Fill=false 时不会输出
    private float _currentLineWidth = 1.0f;
    private bool _currentFill = false;
    private bool _currentStroke = true;

    public VectorPathListener(ILogger? logger = null, double pageHeightPt = 842.0) // 默认 A4 高度 (297mm -> 842pt)
    {
        _logger = logger;
        _pageHeightPt = pageHeightPt;
    }

    public void EventOccurred(IEventData data, EventType type)
    {
        switch (type)
        {
            case EventType.RENDER_PATH:
                HandlePathRender((PathRenderInfo)data);
                break;
            case EventType.BEGIN_TEXT:
            case EventType.END_TEXT:
            case EventType.RENDER_TEXT:
                // 忽略文本事件
                break;
        }
    }

    private void HandlePathRender(PathRenderInfo pathRenderInfo)
    {
        var path = pathRenderInfo.GetPath();
        if (path == null || path.GetSubpaths().Count == 0)
        {
            _logger?.LogDebug("[VectorPathListener] 跳过空路径");
            return;
        }

        try
        {
            UpdateGraphicsState(pathRenderInfo.GetGraphicsState());
            // 获取渲染模式
            var operation = pathRenderInfo.GetOperation();

            // 分析渲染模式来确定填充和描边
            AnalyzeRenderMode(operation);

            var ofdPath = ConvertPathToOfd(pathRenderInfo);
            if (ofdPath != null)
            {
                _paths.Add(ofdPath);
                _logger?.LogDebug("[VectorPathListener] 转换路径成功: Page={Page}, PathData长度={Length}, Op={Op}",
                    ofdPath.Page, ofdPath.PathData?.Length ?? 0, operation);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "[VectorPathListener] 路径转换失败");
        }
    }

    private void AnalyzeRenderMode(int operation)
    {
        // PDF 路径操作码分析
        // S = stroke, f = fill, B = fill then stroke, n = no-op
        _currentStroke = (operation & 1) != 0; // 描边
        _currentFill = (operation & 2) != 0;   // 填充
    }

    private void UpdateGraphicsState(CanvasGraphicsState? gs)
    {
        if (gs == null) return;

        try
        {
            _currentStrokeColor = gs.GetStrokeColor();
            _currentFillColor = gs.GetFillColor();
            _currentLineWidth = (float)(gs.GetLineWidth() * ConvertHelper.Pt2Mm);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "[VectorPathListener] 获取图形状态失败");
        }
    }

    private static string? ColorToString(Color? color)
    {
        if (color == null) return null;

        try
        {
            var components = color.GetColorValue();
            if (components == null || components.Length < 3)
            {
                return color.ToString();
            }
            int r = (int)Math.Round(Math.Clamp(components[0], 0, 1) * 255.0);
            int g = (int)Math.Round(Math.Clamp(components[1], 0, 1) * 255.0);
            int b = (int)Math.Round(Math.Clamp(components[2], 0, 1) * 255.0);
            return FormattableString.Invariant($"{r} {g} {b}");
        }
        catch
        {
            return null;
        }
    }

    private OfdPath? ConvertPathToOfd(PathRenderInfo pathRenderInfo)
    {
        var path = pathRenderInfo.GetPath();
        if (path == null) return null;

        var subpaths = path.GetSubpaths();
        if (subpaths.Count == 0) return null;

        var pathData = new StringBuilder();
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        var ctm = pathRenderInfo.GetCtm();
        double a = 1, b = 0, c = 0, d = 1, e = 0, f = 0;
        if (ctm != null)
        {
            a = ctm.Get(Matrix.I11);
            b = ctm.Get(Matrix.I12);
            c = ctm.Get(Matrix.I21);
            d = ctm.Get(Matrix.I22);
            e = ctm.Get(Matrix.I31);
            f = ctm.Get(Matrix.I32);
        }

        foreach (var subpath in subpaths)
        {
            var segments = subpath.GetSegments();
            bool firstSegment = true;

            foreach (var segment in segments)
            {
                var points = segment.GetBasePoints();
                if (points.Count == 0) continue;

                var converted = new List<(double X, double Y)>(points.Count);
                foreach (var point in points)
                {
                    // 应用 CTM 转换后再换算到 OFD 坐标系（mm，左上原点）
                    double tx = a * point.x + c * point.y + e;
                    double ty = b * point.x + d * point.y + f;
                    double xMm = tx * ConvertHelper.Pt2Mm;
                    double yMm = (_pageHeightPt - ty) * ConvertHelper.Pt2Mm;
                    converted.Add((xMm, yMm));

                    minX = Math.Min(minX, xMm);
                    minY = Math.Min(minY, yMm);
                    maxX = Math.Max(maxX, xMm);
                    maxY = Math.Max(maxY, yMm);
                }

                if (firstSegment)
                {
                    pathData.Append($"M {FormatDouble(converted[0].X)} {FormatDouble(converted[0].Y)} ");
                    firstSegment = false;
                }

                // 根据点数量判断段类型
                if (converted.Count == 2)
                {
                    // 直线段
                    pathData.Append($"L {FormatDouble(converted[1].X)} {FormatDouble(converted[1].Y)} ");
                }
                else if (converted.Count == 3)
                {
                    // 二次贝塞尔曲线
                    pathData.Append($"Q {FormatDouble(converted[1].X)} {FormatDouble(converted[1].Y)} {FormatDouble(converted[2].X)} {FormatDouble(converted[2].Y)} ");
                }
                else if (converted.Count == 4)
                {
                    // 三次贝塞尔曲线
                    pathData.Append($"C {FormatDouble(converted[1].X)} {FormatDouble(converted[1].Y)} {FormatDouble(converted[2].X)} {FormatDouble(converted[2].Y)} {FormatDouble(converted[3].X)} {FormatDouble(converted[3].Y)} ");
                }
                else if (converted.Count > 4)
                {
                    // 复杂路径，连接所有点
                    for (int i = 1; i < converted.Count; i++)
                    {
                        pathData.Append($"L {FormatDouble(converted[i].X)} {FormatDouble(converted[i].Y)} ");
                    }
                }
            }

            // 如果子路径是闭合的，添加 Z 命令
            if (subpath.IsClosed())
            {
                pathData.Append("Z ");
            }
        }

        if (pathData.Length == 0) return null;

        if (double.IsInfinity(minX) || double.IsInfinity(minY) || double.IsInfinity(maxX) || double.IsInfinity(maxY))
        {
            return null;
        }

        return new OfdPath
        {
            Page = 0, // 将在后续设置
            X = minX,
            Y = minY,
            Width = Math.Max(maxX - minX, 0.1), // 确保最小宽度
            Height = Math.Max(maxY - minY, 0.1), // 确保最小高度
            PathData = pathData.ToString().Trim(),
            CTM = null,
            // 设置样式属性
            Stroke = _currentStroke,
            Fill = _currentFill,
            StrokeColor = ColorToString(_currentStrokeColor),
            FillColor = ColorToString(_currentFillColor),
            LineWidth = _currentLineWidth
        };
    }

    public ICollection<EventType> GetSupportedEvents()
    {
        return new[] { EventType.RENDER_PATH };
    }

    public List<OfdPath> GetPaths()
    {
        return _paths;
    }

    private static string FormatDouble(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
