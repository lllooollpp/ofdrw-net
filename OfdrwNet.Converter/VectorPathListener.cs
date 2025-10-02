using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions;
using iText.Kernel.Geom;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf;
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
    // store original PDF line width in points; final mm value will be computed per-path using CTM
    private float _currentLineWidth = 1.0f;
    private float _currentLineWidthPt = 1.0f;
    private bool _currentFill = false;
    private bool _currentStroke = true;
    private double[]? _currentDashPattern;

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
            try
            {
                // Ensure graphics state is preserved for later access (CTM/colors/etc.)
                pathRenderInfo.PreserveGraphicsState();
            }
            catch { }
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
            var lineWidthPt = gs.GetLineWidth();
            // 保存原始 pt 值，真正转换到 mm 的时机是在 ConvertPathToOfd 中（那里有 CTM 可用）
            _currentLineWidthPt = (float)lineWidthPt;

            try
            {
                var dash = gs.GetDashPattern();
                if (dash != null)
                {
                    var size = dash.Size();
                    if (size > 0)
                    {
                        var converted = new double[size];
                        var hasPositive = false;
                        for (int i = 0; i < size; i++)
                        {
                            var number = dash.GetAsNumber(i);
                            // 保持 dash pattern 为 PDF 用户空间单位（pt），在 ConvertPathToOfd 中根据 CTM 再转换为 mm
                            var value = number != null ? Math.Max(0, number.DoubleValue()) : 0.0;
                            if (value > 0.001)
                            {
                                hasPositive = true;
                            }
                            converted[i] = value;
                        }
                        _currentDashPattern = hasPositive ? converted : null;
                    }
                    else
                    {
                        _currentDashPattern = null;
                    }
                }
                else
                {
                    _currentDashPattern = null;
                }
            }
            catch (Exception dashEx)
            {
                _logger?.LogDebug(dashEx, "[VectorPathListener] 获取虚线模式失败");
                _currentDashPattern = null;
            }
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
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        var commands = new List<(char Command, List<(double X, double Y)> Points)>();

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
            if (segments.Count == 0) continue;

            bool firstSegment = true;

            foreach (var segment in segments)
            {
                var points = segment.GetBasePoints();
                if (points.Count == 0) continue;

                var converted = new List<(double X, double Y)>(points.Count);

                foreach (var point in points)
                {
                    double px = point.GetX();
                    double py = point.GetY();

                    // Apply CTM then convert to OFD mm coordinates (origin top-left)
                    double tx = a * px + c * py + e;
                    double ty = b * px + d * py + f;

                    double xMm = tx * ConvertHelper.Pt2Mm;
                    double yMm = (_pageHeightPt - ty) * ConvertHelper.Pt2Mm;

                    converted.Add((xMm, yMm));

                    minX = Math.Min(minX, xMm);
                    minY = Math.Min(minY, yMm);
                    maxX = Math.Max(maxX, xMm);
                    maxY = Math.Max(maxY, yMm);
                }

                if (converted.Count == 0) continue;

                if (firstSegment)
                {
                    commands.Add(('M', new List<(double, double)> { converted[0] }));
                    firstSegment = false;
                }

                switch (converted.Count)
                {
                    case 2:
                        commands.Add(('L', new List<(double, double)> { converted[1] }));
                        break;
                    case 3:
                        commands.Add(('Q', new List<(double, double)> { converted[1], converted[2] }));
                        break;
                    case 4:
                        commands.Add(('C', new List<(double, double)> { converted[1], converted[2], converted[3] }));
                        break;
                    default:
                        for (int i = 1; i < converted.Count; i++)
                        {
                            commands.Add(('L', new List<(double, double)> { converted[i] }));
                        }
                        break;
                }
            }

            if (subpath.IsClosed())
            {
                commands.Add(('Z', new List<(double, double)>()));
            }
        }

        if (commands.Count == 0)
        {
            return null;
        }

        if (minX == double.MaxValue || minY == double.MaxValue || maxX == double.MinValue || maxY == double.MinValue)
        {
            return null;
        }

        var baseWidth = Math.Max(maxX - minX, 0.0);
        var baseHeight = Math.Max(maxY - minY, 0.0);
        var centerX = (minX + maxX) / 2.0;
        var centerY = (minY + maxY) / 2.0;

        double finalWidth = Math.Max(baseWidth, 0.1);
        double finalHeight = Math.Max(baseHeight, 0.1);
        double finalX = minX;
        double finalY = minY;

        double scaledLineWidthMm = _currentLineWidth; // fallback if CTM not available
        double[]? scaledDashPattern = null;

        // 如果有 stroke 且我们有原始线宽 pt 信息，则尝试根据 CTM 计算真实 mm 宽度
        if (_currentStroke)
        {
            try
            {
                // Compute scale factor from CTM: approximate by transforming a unit horizontal vector
                // scaleX = length of vector (a, b) when transforming (1,0)
                var sx = Math.Sqrt(a * a + b * b);
                // For Y scale use (c,d) transforming (0,1)
                var sy = Math.Sqrt(c * c + d * d);

                // Use average scale to handle uniform scaling and small skew
                var avgScale = (sx + sy) / 2.0;

                // Convert line width from pt to mm and apply avgScale
                scaledLineWidthMm = _currentLineWidthPt * ConvertHelper.Pt2Mm * avgScale;

                // Ensure a minimal visible width (very small floor) but avoid large forced clamps
                if (scaledLineWidthMm < 0.01) scaledLineWidthMm = 0.01;

                // Convert dash pattern (stored in pt) to mm using same avgScale
                if (_currentDashPattern != null && _currentDashPattern.Length > 0)
                {
                    scaledDashPattern = new double[_currentDashPattern.Length];
                    for (int i = 0; i < _currentDashPattern.Length; i++)
                    {
                        var dashPt = _currentDashPattern[i];
                        var dashMm = dashPt * ConvertHelper.Pt2Mm * avgScale;
                        // avoid zeros that may confuse serialization
                        scaledDashPattern[i] = Math.Max(0.0, dashMm);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "[VectorPathListener] 计算线宽/虚线时出错，使用默认值");
            }

            // Use scaledLineWidthMm to expand the bounding box a bit to account for stroke thickness
            var minStrokeExtent = Math.Max(scaledLineWidthMm, 0.01);
            var extendedExtent = minStrokeExtent * 1.2;

            if (baseHeight < extendedExtent)
            {
                finalHeight = extendedExtent;
                finalY = centerY - finalHeight / 2.0;
            }
            else
            {
                finalHeight = Math.Max(baseHeight, extendedExtent);
                finalY = centerY - finalHeight / 2.0;
            }

            if (baseWidth < extendedExtent)
            {
                finalWidth = extendedExtent;
                finalX = centerX - finalWidth / 2.0;
            }
            else
            {
                finalWidth = Math.Max(baseWidth, extendedExtent);
                finalX = centerX - finalWidth / 2.0;
            }
        }

        var pathData = new StringBuilder();
        foreach (var cmd in commands)
        {
            switch (cmd.Command)
            {
                case 'M':
                    {
                        var pt = cmd.Points[0];
                        pathData.Append($"M {FormatDouble(pt.X - finalX)} {FormatDouble(pt.Y - finalY)} ");
                        break;
                    }
                case 'L':
                    {
                        var pt = cmd.Points[0];
                        pathData.Append($"L {FormatDouble(pt.X - finalX)} {FormatDouble(pt.Y - finalY)} ");
                        break;
                    }
                case 'Q':
                    {
                        var control = cmd.Points[0];
                        var end = cmd.Points[1];
                        pathData.Append($"Q {FormatDouble(control.X - finalX)} {FormatDouble(control.Y - finalY)} {FormatDouble(end.X - finalX)} {FormatDouble(end.Y - finalY)} ");
                        break;
                    }
                case 'C':
                    {
                        var c1 = cmd.Points[0];
                        var c2 = cmd.Points[1];
                        var end = cmd.Points[2];
                        pathData.Append($"C {FormatDouble(c1.X - finalX)} {FormatDouble(c1.Y - finalY)} {FormatDouble(c2.X - finalX)} {FormatDouble(c2.Y - finalY)} {FormatDouble(end.X - finalX)} {FormatDouble(end.Y - finalY)} ");
                        break;
                    }
                case 'Z':
                    pathData.Append("Z ");
                    break;
            }
        }

        if (pathData.Length == 0)
        {
            return null;
        }

        return new OfdPath
        {
            Page = 0,
            X = finalX,
            Y = finalY,
            Width = finalWidth,
            Height = finalHeight,
            PathData = pathData.ToString().Trim(),
            CTM = null,
            Stroke = _currentStroke,
            Fill = _currentFill,
            StrokeColor = ColorToString(_currentStrokeColor),
            FillColor = ColorToString(_currentFillColor),
            LineWidth = (float)scaledLineWidthMm,
            DashPattern = scaledDashPattern != null && scaledDashPattern.Length > 0 ? scaledDashPattern : null
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
