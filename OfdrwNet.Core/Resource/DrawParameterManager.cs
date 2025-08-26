using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Resource;

/// <summary>
/// OFD绘制参数资源管理器
/// 负责管理OFD文档中的绘制参数资源，包括笔画样式、填充样式、线型等
/// </summary>
public class DrawParameterManager : IDisposable
{
    /// <summary>
    /// 线型资源缓存
    /// </summary>
    private readonly Dictionary<string, LineType> _lineTypes;

    /// <summary>
    /// 填充样式资源缓存
    /// </summary>
    private readonly Dictionary<string, FillStyle> _fillStyles;

    /// <summary>
    /// 笔画样式资源缓存
    /// </summary>
    private readonly Dictionary<string, StrokeStyle> _strokeStyles;

    /// <summary>
    /// 绘制状态资源缓存
    /// </summary>
    private readonly Dictionary<string, DrawState> _drawStates;

    /// <summary>
    /// 资源ID生成器
    /// </summary>
    private int _nextResourceId = 1;

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DrawParameterManager()
    {
        _lineTypes = new Dictionary<string, LineType>();
        _fillStyles = new Dictionary<string, FillStyle>();
        _strokeStyles = new Dictionary<string, StrokeStyle>();
        _drawStates = new Dictionary<string, DrawState>();
        
        // 初始化默认绘制参数
        InitializeDefaultParameters();
    }

    /// <summary>
    /// 初始化默认绘制参数
    /// </summary>
    private void InitializeDefaultParameters()
    {
        // 默认实线
        var solidLine = new LineType(new StId(0))
        {
            Name = "Solid",
            DashPattern = Array.Empty<double>(),
            DashOffset = 0,
            LineCap = LineCap.Butt,
            LineJoin = LineJoin.Miter
        };
        _lineTypes["0"] = solidLine;

        // 默认虚线
        var dashedLine = new LineType(new StId(1))
        {
            Name = "Dashed",
            DashPattern = new[] { 5.0, 5.0 },
            DashOffset = 0,
            LineCap = LineCap.Butt,
            LineJoin = LineJoin.Miter
        };
        _lineTypes["1"] = dashedLine;

        // 默认点线
        var dottedLine = new LineType(new StId(2))
        {
            Name = "Dotted",
            DashPattern = new[] { 1.0, 3.0 },
            DashOffset = 0,
            LineCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        _lineTypes["2"] = dottedLine;

        _nextResourceId = 3;
    }

    #region 线型管理

    /// <summary>
    /// 添加线型
    /// </summary>
    /// <param name="name">线型名称</param>
    /// <param name="dashPattern">虚线模式</param>
    /// <param name="lineCap">线端样式</param>
    /// <param name="lineJoin">线连接样式</param>
    /// <param name="dashOffset">虚线偏移</param>
    /// <returns>线型对象</returns>
    public LineType AddLineType(string name, double[] dashPattern, LineCap lineCap = LineCap.Butt, 
        LineJoin lineJoin = LineJoin.Miter, double dashOffset = 0)
    {
        var lineTypeId = new StId(_nextResourceId++);
        var lineType = new LineType(lineTypeId)
        {
            Name = name,
            DashPattern = dashPattern ?? Array.Empty<double>(),
            DashOffset = dashOffset,
            LineCap = lineCap,
            LineJoin = lineJoin
        };

        _lineTypes[lineTypeId.ToString()] = lineType;
        return lineType;
    }

    /// <summary>
    /// 获取线型
    /// </summary>
    /// <param name="lineTypeId">线型ID</param>
    /// <returns>线型对象</returns>
    public LineType? GetLineType(StId lineTypeId)
    {
        return _lineTypes.TryGetValue(lineTypeId.ToString(), out var lineType) ? lineType : null;
    }

    /// <summary>
    /// 获取默认实线
    /// </summary>
    /// <returns>实线线型</returns>
    public LineType GetDefaultSolidLine()
    {
        return _lineTypes["0"];
    }

    /// <summary>
    /// 获取默认虚线
    /// </summary>
    /// <returns>虚线线型</returns>
    public LineType GetDefaultDashedLine()
    {
        return _lineTypes["1"];
    }

    /// <summary>
    /// 获取默认点线
    /// </summary>
    /// <returns>点线线型</returns>
    public LineType GetDefaultDottedLine()
    {
        return _lineTypes["2"];
    }

    #endregion

    #region 填充样式管理

    /// <summary>
    /// 添加实色填充样式
    /// </summary>
    /// <param name="color">填充颜色</param>
    /// <returns>填充样式对象</returns>
    public FillStyle AddSolidFillStyle(Color color)
    {
        var fillStyleId = new StId(_nextResourceId++);
        var fillStyle = new FillStyle(fillStyleId, FillType.Solid)
        {
            Color = color
        };

        _fillStyles[fillStyleId.ToString()] = fillStyle;
        return fillStyle;
    }

    /// <summary>
    /// 添加渐变填充样式
    /// </summary>
    /// <param name="gradientType">渐变类型</param>
    /// <param name="colors">渐变颜色列表</param>
    /// <param name="stops">渐变停止点</param>
    /// <returns>填充样式对象</returns>
    public FillStyle AddGradientFillStyle(GradientType gradientType, List<Color> colors, List<double> stops)
    {
        var fillStyleId = new StId(_nextResourceId++);
        var fillStyle = new FillStyle(fillStyleId, FillType.Gradient)
        {
            GradientType = gradientType,
            GradientColors = colors,
            GradientStops = stops
        };

        _fillStyles[fillStyleId.ToString()] = fillStyle;
        return fillStyle;
    }

    /// <summary>
    /// 添加图案填充样式
    /// </summary>
    /// <param name="patternImage">图案图像</param>
    /// <param name="patternType">图案类型</param>
    /// <returns>填充样式对象</returns>
    public FillStyle AddPatternFillStyle(ImageResource patternImage, PatternType patternType = PatternType.Tile)
    {
        var fillStyleId = new StId(_nextResourceId++);
        var fillStyle = new FillStyle(fillStyleId, FillType.Pattern)
        {
            PatternImage = patternImage,
            PatternType = patternType
        };

        _fillStyles[fillStyleId.ToString()] = fillStyle;
        return fillStyle;
    }

    /// <summary>
    /// 获取填充样式
    /// </summary>
    /// <param name="fillStyleId">填充样式ID</param>
    /// <returns>填充样式对象</returns>
    public FillStyle? GetFillStyle(StId fillStyleId)
    {
        return _fillStyles.TryGetValue(fillStyleId.ToString(), out var fillStyle) ? fillStyle : null;
    }

    #endregion

    #region 笔画样式管理

    /// <summary>
    /// 添加笔画样式
    /// </summary>
    /// <param name="lineWidth">线宽</param>
    /// <param name="strokeColor">笔画颜色</param>
    /// <param name="lineType">线型</param>
    /// <returns>笔画样式对象</returns>
    public StrokeStyle AddStrokeStyle(double lineWidth, Color strokeColor, LineType? lineType = null)
    {
        var strokeStyleId = new StId(_nextResourceId++);
        var strokeStyle = new StrokeStyle(strokeStyleId)
        {
            LineWidth = lineWidth,
            StrokeColor = strokeColor,
            LineType = lineType ?? GetDefaultSolidLine()
        };

        _strokeStyles[strokeStyleId.ToString()] = strokeStyle;
        return strokeStyle;
    }

    /// <summary>
    /// 获取笔画样式
    /// </summary>
    /// <param name="strokeStyleId">笔画样式ID</param>
    /// <returns>笔画样式对象</returns>
    public StrokeStyle? GetStrokeStyle(StId strokeStyleId)
    {
        return _strokeStyles.TryGetValue(strokeStyleId.ToString(), out var strokeStyle) ? strokeStyle : null;
    }

    #endregion

    #region 绘制状态管理

    /// <summary>
    /// 添加绘制状态
    /// </summary>
    /// <param name="strokeStyle">笔画样式</param>
    /// <param name="fillStyle">填充样式</param>
    /// <param name="opacity">不透明度</param>
    /// <returns>绘制状态对象</returns>
    public DrawState AddDrawState(StrokeStyle? strokeStyle = null, FillStyle? fillStyle = null, double opacity = 1.0)
    {
        var drawStateId = new StId(_nextResourceId++);
        var drawState = new DrawState(drawStateId)
        {
            StrokeStyle = strokeStyle,
            FillStyle = fillStyle,
            Opacity = opacity
        };

        _drawStates[drawStateId.ToString()] = drawState;
        return drawState;
    }

    /// <summary>
    /// 获取绘制状态
    /// </summary>
    /// <param name="drawStateId">绘制状态ID</param>
    /// <returns>绘制状态对象</returns>
    public DrawState? GetDrawState(StId drawStateId)
    {
        return _drawStates.TryGetValue(drawStateId.ToString(), out var drawState) ? drawState : null;
    }

    #endregion

    #region 资源管理

    /// <summary>
    /// 生成绘制参数XML
    /// </summary>
    /// <returns>绘制参数XML内容</returns>
    public string GenerateDrawParameterXml()
    {
        var xml = "";

        // 生成线型XML
        if (_lineTypes.Count > 3) // 超过默认的3个线型
        {
            xml += "  <ofd:DrawParams>\n";
            
            foreach (var lineType in _lineTypes.Values.Skip(3))
            {
                xml += $"    <ofd:LineWidth ID='{lineType.Id}' LineWidth='1.0'>\n";
                if (lineType.DashPattern.Length > 0)
                {
                    var pattern = string.Join(" ", lineType.DashPattern);
                    xml += $"      <ofd:DashPattern>{pattern}</ofd:DashPattern>\n";
                }
                xml += $"      <ofd:LineCap>{lineType.LineCap}</ofd:LineCap>\n";
                xml += $"      <ofd:LineJoin>{lineType.LineJoin}</ofd:LineJoin>\n";
                xml += "    </ofd:LineWidth>\n";
            }
            
            xml += "  </ofd:DrawParams>\n";
        }

        return xml;
    }

    /// <summary>
    /// 获取所有线型
    /// </summary>
    /// <returns>线型列表</returns>
    public List<LineType> GetAllLineTypes()
    {
        return _lineTypes.Values.ToList();
    }

    /// <summary>
    /// 获取所有填充样式
    /// </summary>
    /// <returns>填充样式列表</returns>
    public List<FillStyle> GetAllFillStyles()
    {
        return _fillStyles.Values.ToList();
    }

    /// <summary>
    /// 获取所有笔画样式
    /// </summary>
    /// <returns>笔画样式列表</returns>
    public List<StrokeStyle> GetAllStrokeStyles()
    {
        return _strokeStyles.Values.ToList();
    }

    /// <summary>
    /// 获取所有绘制状态
    /// </summary>
    /// <returns>绘制状态列表</returns>
    public List<DrawState> GetAllDrawStates()
    {
        return _drawStates.Values.ToList();
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public DrawParameterStatistics GetStatistics()
    {
        return new DrawParameterStatistics
        {
            LineTypeCount = _lineTypes.Count,
            FillStyleCount = _fillStyles.Count,
            StrokeStyleCount = _strokeStyles.Count,
            DrawStateCount = _drawStates.Count
        };
    }

    /// <summary>
    /// 清空所有自定义绘制参数（保留默认的）
    /// </summary>
    public void ClearCustom()
    {
        // 只保留前3个默认线型
        var defaultLineTypes = _lineTypes.Take(3).ToList();
        _lineTypes.Clear();
        
        foreach (var kvp in defaultLineTypes)
        {
            _lineTypes[kvp.Key] = kvp.Value;
        }

        _fillStyles.Clear();
        _strokeStyles.Clear();
        _drawStates.Clear();
        _nextResourceId = 3;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _lineTypes.Clear();
            _fillStyles.Clear();
            _strokeStyles.Clear();
            _drawStates.Clear();
            _disposed = true;
        }
    }

    #endregion

    /// <summary>
    /// 获取管理器摘要信息
    /// </summary>
    /// <returns>摘要字符串</returns>
    public override string ToString()
    {
        return $"DrawParameterManager[LineTypes={_lineTypes.Count}, FillStyles={_fillStyles.Count}, StrokeStyles={_strokeStyles.Count}, DrawStates={_drawStates.Count}]";
    }
}

#region 绘制参数类定义

/// <summary>
/// 线型类
/// </summary>
public class LineType
{
    public StId Id { get; set; }
    public string? Name { get; set; }
    public double[] DashPattern { get; set; } = Array.Empty<double>();
    public double DashOffset { get; set; }
    public LineCap LineCap { get; set; } = LineCap.Butt;
    public LineJoin LineJoin { get; set; } = LineJoin.Miter;

    public LineType(StId id)
    {
        Id = id;
    }

    public override string ToString()
    {
        return $"LineType[ID={Id}, Name={Name}, Pattern=[{string.Join(",", DashPattern)}]]";
    }
}

/// <summary>
/// 填充样式类
/// </summary>
public class FillStyle
{
    public StId Id { get; set; }
    public FillType Type { get; set; }
    public Color? Color { get; set; }
    public GradientType GradientType { get; set; }
    public List<Color>? GradientColors { get; set; }
    public List<double>? GradientStops { get; set; }
    public ImageResource? PatternImage { get; set; }
    public PatternType PatternType { get; set; }

    public FillStyle(StId id, FillType type)
    {
        Id = id;
        Type = type;
    }

    public override string ToString()
    {
        return $"FillStyle[ID={Id}, Type={Type}]";
    }
}

/// <summary>
/// 笔画样式类
/// </summary>
public class StrokeStyle
{
    public StId Id { get; set; }
    public double LineWidth { get; set; } = 1.0;
    public Color? StrokeColor { get; set; }
    public LineType? LineType { get; set; }

    public StrokeStyle(StId id)
    {
        Id = id;
    }

    public override string ToString()
    {
        return $"StrokeStyle[ID={Id}, Width={LineWidth}]";
    }
}

/// <summary>
/// 绘制状态类
/// </summary>
public class DrawState
{
    public StId Id { get; set; }
    public StrokeStyle? StrokeStyle { get; set; }
    public FillStyle? FillStyle { get; set; }
    public double Opacity { get; set; } = 1.0;

    public DrawState(StId id)
    {
        Id = id;
    }

    public override string ToString()
    {
        return $"DrawState[ID={Id}, Opacity={Opacity:F2}]";
    }
}

#endregion

#region 枚举定义

/// <summary>
/// 线端样式枚举
/// </summary>
public enum LineCap
{
    Butt,   // 平头
    Round,  // 圆头
    Square  // 方头
}

/// <summary>
/// 线连接样式枚举
/// </summary>
public enum LineJoin
{
    Miter,  // 尖角
    Round,  // 圆角
    Bevel   // 斜角
}

/// <summary>
/// 填充类型枚举
/// </summary>
public enum FillType
{
    None,     // 无填充
    Solid,    // 实色填充
    Gradient, // 渐变填充
    Pattern   // 图案填充
}

/// <summary>
/// 渐变类型枚举
/// </summary>
public enum GradientType
{
    Linear,  // 线性渐变
    Radial   // 径向渐变
}

/// <summary>
/// 图案类型枚举
/// </summary>
public enum PatternType
{
    Tile,    // 平铺
    Stretch, // 拉伸
    Center   // 居中
}

#endregion

/// <summary>
/// 绘制参数统计信息
/// </summary>
public class DrawParameterStatistics
{
    public int LineTypeCount { get; set; }
    public int FillStyleCount { get; set; }
    public int StrokeStyleCount { get; set; }
    public int DrawStateCount { get; set; }

    public override string ToString()
    {
        return $"DrawParameterStats[LineTypes={LineTypeCount}, FillStyles={FillStyleCount}, StrokeStyles={StrokeStyleCount}, DrawStates={DrawStateCount}]";
    }
}