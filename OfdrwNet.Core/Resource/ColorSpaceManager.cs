using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Resource;

/// <summary>
/// OFD颜色空间管理器
/// 负责管理OFD文档中的颜色空间定义和颜色资源
/// </summary>
public class ColorSpaceManager : IDisposable
{
    /// <summary>
    /// 颜色空间缓存
    /// Key: 颜色空间ID，Value: 颜色空间对象
    /// </summary>
    private readonly Dictionary<string, ColorSpace> _colorSpaces;

    /// <summary>
    /// 颜色资源缓存
    /// Key: 颜色ID，Value: 颜色对象
    /// </summary>
    private readonly Dictionary<string, Color> _colors;

    /// <summary>
    /// 颜色空间ID生成器
    /// </summary>
    private int _nextColorSpaceId = 1;

    /// <summary>
    /// 颜色ID生成器
    /// </summary>
    private int _nextColorId = 1;

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ColorSpaceManager()
    {
        _colorSpaces = new Dictionary<string, ColorSpace>();
        _colors = new Dictionary<string, Color>();
        
        // 添加默认颜色空间
        InitializeDefaultColorSpaces();
    }

    /// <summary>
    /// 初始化默认颜色空间
    /// </summary>
    private void InitializeDefaultColorSpaces()
    {
        // 添加标准RGB颜色空间
        var rgbColorSpace = new ColorSpace(new StId(0), ColorSpaceType.RGB)
        {
            Name = "RGB",
            Description = "标准RGB颜色空间"
        };
        _colorSpaces["0"] = rgbColorSpace;

        // 添加标准CMYK颜色空间
        var cmykColorSpace = new ColorSpace(new StId(1), ColorSpaceType.CMYK)
        {
            Name = "CMYK",
            Description = "标准CMYK颜色空间"
        };
        _colorSpaces["1"] = cmykColorSpace;

        // 添加灰度颜色空间
        var grayColorSpace = new ColorSpace(new StId(2), ColorSpaceType.Gray)
        {
            Name = "Gray",
            Description = "标准灰度颜色空间"
        };
        _colorSpaces["2"] = grayColorSpace;

        _nextColorSpaceId = 3;
    }

    /// <summary>
    /// 添加自定义颜色空间
    /// </summary>
    /// <param name="type">颜色空间类型</param>
    /// <param name="name">颜色空间名称</param>
    /// <param name="description">描述</param>
    /// <returns>颜色空间对象</returns>
    public ColorSpace AddColorSpace(ColorSpaceType type, string name, string? description = null)
    {
        var colorSpaceId = new StId(_nextColorSpaceId++);
        var colorSpace = new ColorSpace(colorSpaceId, type)
        {
            Name = name,
            Description = description
        };

        _colorSpaces[colorSpaceId.ToString()] = colorSpace;
        return colorSpace;
    }

    /// <summary>
    /// 获取颜色空间
    /// </summary>
    /// <param name="colorSpaceId">颜色空间ID</param>
    /// <returns>颜色空间对象，如果不存在则返回null</returns>
    public ColorSpace? GetColorSpace(StId colorSpaceId)
    {
        return _colorSpaces.TryGetValue(colorSpaceId.ToString(), out var colorSpace) ? colorSpace : null;
    }

    /// <summary>
    /// 获取默认RGB颜色空间
    /// </summary>
    /// <returns>RGB颜色空间</returns>
    public ColorSpace GetDefaultRgbColorSpace()
    {
        return _colorSpaces["0"];
    }

    /// <summary>
    /// 获取默认CMYK颜色空间
    /// </summary>
    /// <returns>CMYK颜色空间</returns>
    public ColorSpace GetDefaultCmykColorSpace()
    {
        return _colorSpaces["1"];
    }

    /// <summary>
    /// 获取默认灰度颜色空间
    /// </summary>
    /// <returns>灰度颜色空间</returns>
    public ColorSpace GetDefaultGrayColorSpace()
    {
        return _colorSpaces["2"];
    }

    /// <summary>
    /// 创建RGB颜色
    /// </summary>
    /// <param name="red">红色分量（0-255）</param>
    /// <param name="green">绿色分量（0-255）</param>
    /// <param name="blue">蓝色分量（0-255）</param>
    /// <param name="alpha">透明度（0-255，可选）</param>
    /// <returns>颜色对象</returns>
    public Color CreateRgbColor(byte red, byte green, byte blue, byte alpha = 255)
    {
        var colorId = new StId(_nextColorId++);
        var color = new Color(colorId, GetDefaultRgbColorSpace())
        {
            Components = new[] { red / 255.0, green / 255.0, blue / 255.0 },
            Alpha = alpha / 255.0
        };

        _colors[colorId.ToString()] = color;
        return color;
    }

    /// <summary>
    /// 创建CMYK颜色
    /// </summary>
    /// <param name="cyan">青色分量（0-100）</param>
    /// <param name="magenta">品红分量（0-100）</param>
    /// <param name="yellow">黄色分量（0-100）</param>
    /// <param name="black">黑色分量（0-100）</param>
    /// <returns>颜色对象</returns>
    public Color CreateCmykColor(double cyan, double magenta, double yellow, double black)
    {
        var colorId = new StId(_nextColorId++);
        var color = new Color(colorId, GetDefaultCmykColorSpace())
        {
            Components = new[] { cyan / 100.0, magenta / 100.0, yellow / 100.0, black / 100.0 }
        };

        _colors[colorId.ToString()] = color;
        return color;
    }

    /// <summary>
    /// 创建灰度颜色
    /// </summary>
    /// <param name="gray">灰度值（0-255）</param>
    /// <returns>颜色对象</returns>
    public Color CreateGrayColor(byte gray)
    {
        var colorId = new StId(_nextColorId++);
        var color = new Color(colorId, GetDefaultGrayColorSpace())
        {
            Components = new[] { gray / 255.0 }
        };

        _colors[colorId.ToString()] = color;
        return color;
    }

    /// <summary>
    /// 从十六进制字符串创建RGB颜色
    /// </summary>
    /// <param name="hexColor">十六进制颜色字符串（如 "#FF0000" 或 "FF0000"）</param>
    /// <returns>颜色对象</returns>
    public Color CreateColorFromHex(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor))
        {
            throw new ArgumentException("颜色字符串不能为空", nameof(hexColor));
        }

        // 移除 # 前缀
        hexColor = hexColor.TrimStart('#');

        if (hexColor.Length != 6 && hexColor.Length != 8)
        {
            throw new ArgumentException("颜色字符串格式不正确，应为6位或8位十六进制", nameof(hexColor));
        }

        var red = Convert.ToByte(hexColor.Substring(0, 2), 16);
        var green = Convert.ToByte(hexColor.Substring(2, 2), 16);
        var blue = Convert.ToByte(hexColor.Substring(4, 2), 16);
        var alpha = hexColor.Length == 8 ? Convert.ToByte(hexColor.Substring(6, 2), 16) : (byte)255;

        return CreateRgbColor(red, green, blue, alpha);
    }

    /// <summary>
    /// 获取颜色
    /// </summary>
    /// <param name="colorId">颜色ID</param>
    /// <returns>颜色对象，如果不存在则返回null</returns>
    public Color? GetColor(StId colorId)
    {
        return _colors.TryGetValue(colorId.ToString(), out var color) ? color : null;
    }

    /// <summary>
    /// 获取所有颜色空间
    /// </summary>
    /// <returns>颜色空间列表</returns>
    public List<ColorSpace> GetAllColorSpaces()
    {
        return _colorSpaces.Values.ToList();
    }

    /// <summary>
    /// 获取所有颜色
    /// </summary>
    /// <returns>颜色列表</returns>
    public List<Color> GetAllColors()
    {
        return _colors.Values.ToList();
    }

    /// <summary>
    /// 生成颜色空间XML
    /// </summary>
    /// <returns>颜色空间XML内容</returns>
    public string GenerateColorSpaceXml()
    {
        var xml = "";
        
        if (_colorSpaces.Count > 3) // 超过默认的3个颜色空间
        {
            xml += "  <ofd:ColorSpaces>\n";
            
            foreach (var colorSpace in _colorSpaces.Values.Skip(3)) // 跳过默认的3个
            {
                xml += $"    <ofd:ColorSpace ID='{colorSpace.Id}' Type='{colorSpace.Type}'";
                if (!string.IsNullOrEmpty(colorSpace.Name))
                {
                    xml += $" Name='{colorSpace.Name}'";
                }
                xml += ">\n";
                
                if (!string.IsNullOrEmpty(colorSpace.Description))
                {
                    xml += $"      <ofd:Description>{colorSpace.Description}</ofd:Description>\n";
                }
                
                xml += "    </ofd:ColorSpace>\n";
            }
            
            xml += "  </ofd:ColorSpaces>\n";
        }
        
        return xml;
    }

    /// <summary>
    /// 获取颜色管理器统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public ColorManagerStatistics GetStatistics()
    {
        var stats = new ColorManagerStatistics
        {
            TotalColorSpaces = _colorSpaces.Count,
            TotalColors = _colors.Count,
            RgbColorSpaces = _colorSpaces.Values.Count(cs => cs.Type == ColorSpaceType.RGB),
            CmykColorSpaces = _colorSpaces.Values.Count(cs => cs.Type == ColorSpaceType.CMYK),
            GrayColorSpaces = _colorSpaces.Values.Count(cs => cs.Type == ColorSpaceType.Gray)
        };

        return stats;
    }

    /// <summary>
    /// 清空所有自定义颜色和颜色空间（保留默认的）
    /// </summary>
    public void ClearCustom()
    {
        // 只保留前3个默认颜色空间
        var defaultColorSpaces = _colorSpaces.Take(3).ToList();
        _colorSpaces.Clear();
        
        foreach (var kvp in defaultColorSpaces)
        {
            _colorSpaces[kvp.Key] = kvp.Value;
        }

        _colors.Clear();
        _nextColorSpaceId = 3;
        _nextColorId = 1;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _colorSpaces.Clear();
            _colors.Clear();
            _disposed = true;
        }
    }

    /// <summary>
    /// 获取管理器摘要信息
    /// </summary>
    /// <returns>摘要字符串</returns>
    public override string ToString()
    {
        return $"ColorSpaceManager[ColorSpaces={_colorSpaces.Count}, Colors={_colors.Count}]";
    }
}

/// <summary>
/// 颜色空间类
/// </summary>
public class ColorSpace
{
    /// <summary>
    /// 颜色空间ID
    /// </summary>
    public StId Id { get; set; }

    /// <summary>
    /// 颜色空间类型
    /// </summary>
    public ColorSpaceType Type { get; set; }

    /// <summary>
    /// 颜色空间名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 描述信息
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">颜色空间ID</param>
    /// <param name="type">颜色空间类型</param>
    public ColorSpace(StId id, ColorSpaceType type)
    {
        Id = id;
        Type = type;
    }

    /// <summary>
    /// 获取颜色空间摘要信息
    /// </summary>
    /// <returns>摘要字符串</returns>
    public override string ToString()
    {
        return $"ColorSpace[ID={Id}, Type={Type}, Name={Name}]";
    }
}

/// <summary>
/// 颜色类
/// </summary>
public class Color
{
    /// <summary>
    /// 颜色ID
    /// </summary>
    public StId Id { get; set; }

    /// <summary>
    /// 所属颜色空间
    /// </summary>
    public ColorSpace ColorSpace { get; set; }

    /// <summary>
    /// 颜色分量值（0.0-1.0）
    /// </summary>
    public double[] Components { get; set; }

    /// <summary>
    /// 透明度（0.0-1.0）
    /// </summary>
    public double Alpha { get; set; } = 1.0;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">颜色ID</param>
    /// <param name="colorSpace">所属颜色空间</param>
    public Color(StId id, ColorSpace colorSpace)
    {
        Id = id;
        ColorSpace = colorSpace ?? throw new ArgumentNullException(nameof(colorSpace));
        Components = Array.Empty<double>();
    }

    /// <summary>
    /// 转换为十六进制字符串（仅适用于RGB颜色）
    /// </summary>
    /// <returns>十六进制颜色字符串</returns>
    public string ToHexString()
    {
        if (ColorSpace.Type != ColorSpaceType.RGB || Components.Length < 3)
        {
            throw new InvalidOperationException("只有RGB颜色才能转换为十六进制字符串");
        }

        var red = (byte)(Components[0] * 255);
        var green = (byte)(Components[1] * 255);
        var blue = (byte)(Components[2] * 255);
        var alpha = (byte)(Alpha * 255);

        return alpha == 255 
            ? $"#{red:X2}{green:X2}{blue:X2}" 
            : $"#{red:X2}{green:X2}{blue:X2}{alpha:X2}";
    }

    /// <summary>
    /// 获取颜色摘要信息
    /// </summary>
    /// <returns>摘要字符串</returns>
    public override string ToString()
    {
        var componentsStr = string.Join(", ", Components.Select(c => c.ToString("F2")));
        return $"Color[ID={Id}, Space={ColorSpace.Type}, Components=[{componentsStr}], Alpha={Alpha:F2}]";
    }
}

/// <summary>
/// 颜色空间类型枚举
/// </summary>
public enum ColorSpaceType
{
    /// <summary>
    /// RGB颜色空间
    /// </summary>
    RGB,

    /// <summary>
    /// CMYK颜色空间
    /// </summary>
    CMYK,

    /// <summary>
    /// 灰度颜色空间
    /// </summary>
    Gray,

    /// <summary>
    /// LAB颜色空间
    /// </summary>
    LAB,

    /// <summary>
    /// 自定义颜色空间
    /// </summary>
    Custom
}

/// <summary>
/// 颜色管理器统计信息
/// </summary>
public class ColorManagerStatistics
{
    /// <summary>
    /// 总颜色空间数量
    /// </summary>
    public int TotalColorSpaces { get; set; }

    /// <summary>
    /// 总颜色数量
    /// </summary>
    public int TotalColors { get; set; }

    /// <summary>
    /// RGB颜色空间数量
    /// </summary>
    public int RgbColorSpaces { get; set; }

    /// <summary>
    /// CMYK颜色空间数量
    /// </summary>
    public int CmykColorSpaces { get; set; }

    /// <summary>
    /// 灰度颜色空间数量
    /// </summary>
    public int GrayColorSpaces { get; set; }

    /// <summary>
    /// 获取统计摘要
    /// </summary>
    /// <returns>统计摘要字符串</returns>
    public override string ToString()
    {
        return $"ColorStatistics[Spaces={TotalColorSpaces}, Colors={TotalColors}, RGB={RgbColorSpaces}, CMYK={CmykColorSpaces}, Gray={GrayColorSpaces}]";
    }
}