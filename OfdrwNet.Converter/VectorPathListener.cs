using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions;
using iText.Kernel.Geom;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Canvas;
using System.Text;

namespace OfdrwNet.Converter;

/// <summary>
/// 监听PDF中的路径渲染事件，将路径转换为OFD PathObject
/// </summary>
public class VectorPathListener : IEventListener
{
    private readonly ILogger? _logger;
    private readonly List<OfdPath> _paths = new();
    private readonly double _pageHeight; // 用于坐标系转换

    // 当前图形状态（初始化为黑色描边、透明填充，避免未赋值警告；真实值待 UpdateGraphicsState 后续完善）
    private Color? _currentStrokeColor = ColorConstants.BLACK; // 默认描边色
    private Color? _currentFillColor = new DeviceRgb(0, 0, 0) { }; // 先占位，Fill=false 时不会输出
    private float _currentLineWidth = 1.0f;
    private bool _currentFill = false;
    private bool _currentStroke = true;

    public VectorPathListener(ILogger? logger = null, double pageHeight = 297.0) // A4默认高度
    {
        _logger = logger;
        _pageHeight = pageHeight;
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
            // 这些API可能不可用，先注释掉
            // _currentStrokeColor = gs.GetStrokeColor();
            // _currentFillColor = gs.GetFillColor();
            // _currentLineWidth = gs.GetLineWidth();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "[VectorPathListener] 获取图形状态失败");
        }
    }

    private string? ColorToString(Color? color)
    {
        if (color == null) return null;

        try
        {
            // 简单的颜色转换，可以根据需要扩展
            return color.ToString();
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

        var pathData = new System.Text.StringBuilder();
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var subpath in subpaths)
        {
            var segments = subpath.GetSegments();
            bool firstSegment = true;

            foreach (var segment in segments)
            {
                var points = segment.GetBasePoints();
                if (points.Count == 0) continue;

                // 更新边界
                foreach (var point in points)
                {
                    minX = Math.Min(minX, point.x);
                    minY = Math.Min(minY, point.y);
                    maxX = Math.Max(maxX, point.x);
                    maxY = Math.Max(maxY, point.y);
                }

                // 改进的路径处理：区分不同类型的段
                if (firstSegment)
                {
                    pathData.Append($"M {points[0].x:0.###} {points[0].y:0.###} ");
                    firstSegment = false;
                }

                // 根据点数量判断段类型
                if (points.Count == 2)
                {
                    // 直线段
                    pathData.Append($"L {points[1].x:0.###} {points[1].y:0.###} ");
                }
                else if (points.Count == 3)
                {
                    // 二次贝塞尔曲线
                    pathData.Append($"Q {points[1].x:0.###} {points[1].y:0.###} {points[2].x:0.###} {points[2].y:0.###} ");
                }
                else if (points.Count == 4)
                {
                    // 三次贝塞尔曲线
                    pathData.Append($"C {points[1].x:0.###} {points[1].y:0.###} {points[2].x:0.###} {points[2].y:0.###} {points[3].x:0.###} {points[3].y:0.###} ");
                }
                else if (points.Count > 4)
                {
                    // 复杂路径，连接所有点
                    for (int i = 1; i < points.Count; i++)
                    {
                        pathData.Append($"L {points[i].x:0.###} {points[i].y:0.###} ");
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

        // 修正 CTM 变换矩阵提取
        var ctm = pathRenderInfo.GetCtm();
        double[]? ctmArray = null;
        if (ctm != null)
        {
            ctmArray = new double[6];
            // iText Matrix 是 3x3 矩阵，标准 2D 变换矩阵索引：
            // | a  b  e |   | Get(0) Get(3) Get(6) |
            // | c  d  f | = | Get(1) Get(4) Get(7) |
            // | 0  0  1 |   | Get(2) Get(5) Get(8) |
            ctmArray[0] = ctm.Get(Matrix.I11); // a - x缩放
            ctmArray[1] = ctm.Get(Matrix.I12); // b - xy倾斜
            ctmArray[2] = ctm.Get(Matrix.I21); // c - yx倾斜
            ctmArray[3] = ctm.Get(Matrix.I22); // d - y缩放
            ctmArray[4] = ctm.Get(Matrix.I31); // e - x平移
            ctmArray[5] = ctm.Get(Matrix.I32); // f - y平移
        }

        return new OfdPath
        {
            Page = 0, // 将在后续设置
            X = minX,
            Y = minY,
            Width = Math.Max(maxX - minX, 0.1), // 确保最小宽度
            Height = Math.Max(maxY - minY, 0.1), // 确保最小高度
            PathData = pathData.ToString().Trim(),
            CTM = ctmArray,
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
}
