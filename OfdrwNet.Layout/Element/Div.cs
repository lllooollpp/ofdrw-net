using OfdrwNet.Core;
using OfdrwNet.Core.BasicType;
using System.Xml.Linq;

namespace OfdrwNet.Layout.Element;

/// <summary>
/// 基础布局元素
/// 对应 Java 版本的 org.ofdrw.layout.element.Div
/// 所有布局元素的基类，提供基本的位置、尺寸、样式等属性
/// </summary>
public class Div
{
    /// <summary>
    /// X坐标
    /// </summary>
    public double? X { get; set; }

    /// <summary>
    /// Y坐标
    /// </summary>
    public double? Y { get; set; }

    /// <summary>
    /// 宽度
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// 高度
    /// </summary>
    public double? Height { get; set; }

    /// <summary>
    /// 定位方式
    /// </summary>
    public Position Position { get; set; } = Position.Relative;

    /// <summary>
    /// 浮动方式
    /// </summary>
    public AFloat Float { get; set; } = AFloat.None;

    /// <summary>
    /// 图层类型
    /// </summary>
    public LayerType Layer { get; set; } = LayerType.Body;

    /// <summary>
    /// 边距 [上, 右, 下, 左]
    /// </summary>
    private double[] _margin = { 0, 0, 0, 0 };

    /// <summary>
    /// 内边距 [上, 右, 下, 左]
    /// </summary>
    private double[] _padding = { 0, 0, 0, 0 };

    /// <summary>
    /// 边框宽度
    /// </summary>
    public double BorderWidth { get; set; } = 0;

    /// <summary>
    /// 边框颜色
    /// </summary>
    public string? BorderColor { get; set; }

    /// <summary>
    /// 背景颜色
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// 子元素列表
    /// </summary>
    public List<Div> Children { get; set; } = new List<Div>();

    /// <summary>
    /// 元素类型标识
    /// </summary>
    public virtual string ElementType => "Div";

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public Div() { }

    /// <summary>
    /// 指定尺寸的构造函数
    /// </summary>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public Div(double width, double height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// 指定位置和尺寸的构造函数
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public Div(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// 设置位置
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>this</returns>
    public Div SetXY(double x, double y)
    {
        X = x;
        Y = y;
        return this;
    }

    /// <summary>
    /// 设置尺寸
    /// </summary>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>this</returns>
    public Div SetSize(double width, double height)
    {
        Width = width;
        Height = height;
        return this;
    }

    /// <summary>
    /// 设置定位方式
    /// </summary>
    /// <param name="position">定位方式</param>
    /// <returns>this</returns>
    public Div SetPosition(Position position)
    {
        Position = position;
        return this;
    }

    /// <summary>
    /// 设置浮动方式
    /// </summary>
    /// <param name="floatType">浮动方式</param>
    /// <returns>this</returns>
    public Div SetFloat(AFloat floatType)
    {
        Float = floatType;
        return this;
    }

    /// <summary>
    /// 设置图层
    /// </summary>
    /// <param name="layer">图层类型</param>
    /// <returns>this</returns>
    public Div SetLayer(LayerType layer)
    {
        Layer = layer;
        return this;
    }

    /// <summary>
    /// 设置外边距
    /// </summary>
    /// <param name="margin">边距数组</param>
    /// <returns>this</returns>
    public Div SetMargin(params double[] margin)
    {
        _margin = ProcessSpacingArray(margin);
        return this;
    }

    /// <summary>
    /// 获取外边距
    /// </summary>
    /// <returns>[上, 右, 下, 左]</returns>
    public double[] GetMargin() => (double[])_margin.Clone();

    /// <summary>
    /// 设置内边距
    /// </summary>
    /// <param name="padding">内边距数组</param>
    /// <returns>this</returns>
    public Div SetPadding(params double[] padding)
    {
        _padding = ProcessSpacingArray(padding);
        return this;
    }

    /// <summary>
    /// 获取内边距
    /// </summary>
    /// <returns>[上, 右, 下, 左]</returns>
    public double[] GetPadding() => (double[])_padding.Clone();

    /// <summary>
    /// 设置边框
    /// </summary>
    /// <param name="width">边框宽度</param>
    /// <param name="color">边框颜色</param>
    /// <returns>this</returns>
    public Div SetBorder(double width, string? color = null)
    {
        BorderWidth = width;
        BorderColor = color ?? "#000000";
        return this;
    }

    /// <summary>
    /// 设置背景颜色
    /// </summary>
    /// <param name="color">颜色值</param>
    /// <returns>this</returns>
    public Div SetBackgroundColor(string color)
    {
        BackgroundColor = color;
        return this;
    }

    /// <summary>
    /// 设置背景颜色 (RGB)
    /// </summary>
    /// <param name="r">红色分量 (0-255)</param>
    /// <param name="g">绿色分量 (0-255)</param>
    /// <param name="b">蓝色分量 (0-255)</param>
    /// <returns>this</returns>
    public Div SetBackgroundColor(int r, int g, int b)
    {
        BackgroundColor = $"#{r:X2}{g:X2}{b:X2}";
        return this;
    }

    /// <summary>
    /// 添加子元素
    /// </summary>
    /// <param name="child">子元素</param>
    /// <returns>this</returns>
    public Div Add(Div child)
    {
        if (child != null)
        {
            Children.Add(child);
        }
        return this;
    }

    /// <summary>
    /// 计算额外宽度（边距+内边距+边框）
    /// </summary>
    /// <returns>额外宽度</returns>
    public double WidthPlus()
    {
        return _margin[1] + _margin[3] + _padding[1] + _padding[3] + BorderWidth * 2;
    }

    /// <summary>
    /// 计算额外高度（边距+内边距+边框）
    /// </summary>
    /// <returns>额外高度</returns>
    public double HeightPlus()
    {
        return _margin[0] + _margin[2] + _padding[0] + _padding[2] + BorderWidth * 2;
    }

    /// <summary>
    /// 预处理方法，在布局前调用
    /// </summary>
    /// <param name="width">可用宽度</param>
    public virtual void DoPrepare(double width)
    {
        // 子类可以重写此方法进行特定的预处理
    }

    /// <summary>
    /// 处理间距数组，支持1-4个参数
    /// </summary>
    /// <param name="spacing">间距参数</param>
    /// <returns>标准化的间距数组[上, 右, 下, 左]</returns>
    private static double[] ProcessSpacingArray(double[] spacing)
    {
        return spacing?.Length switch
        {
            1 => new[] { spacing[0], spacing[0], spacing[0], spacing[0] },
            2 => new[] { spacing[0], spacing[1], spacing[0], spacing[1] },
            3 => new[] { spacing[0], spacing[1], spacing[2], spacing[1] },
            4 => new[] { spacing[0], spacing[1], spacing[2], spacing[3] },
            _ => new[] { 0.0, 0.0, 0.0, 0.0 }
        };
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    public override string ToString()
    {
        return $"{ElementType}({X}, {Y}, {Width}, {Height})";
    }
}

/// <summary>
/// 定位方式枚举
/// </summary>
public enum Position
{
    /// <summary>
    /// 相对定位
    /// </summary>
    Relative,

    /// <summary>
    /// 绝对定位
    /// </summary>
    Absolute,

    /// <summary>
    /// 固定定位
    /// </summary>
    Fixed
}

/// <summary>
/// 浮动方式枚举
/// </summary>
public enum AFloat
{
    /// <summary>
    /// 不浮动
    /// </summary>
    None,

    /// <summary>
    /// 左浮动
    /// </summary>
    Left,

    /// <summary>
    /// 右浮动
    /// </summary>
    Right,

    /// <summary>
    /// 居中
    /// </summary>
    Center
}