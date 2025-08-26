using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Layout;

/// <summary>
/// 虚拟页面样式
/// 对应 Java 版本的 org.ofdrw.layout.PageLayout
/// 用于定义页面的尺寸、边距等布局属性
/// </summary>
public class PageLayout
{
    /// <summary>
    /// 页面宽度 (毫米)
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// 页面高度 (毫米)
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// 外边距
    /// 页边距：上下都是2.54厘米，左右都是3.17厘米
    /// [上, 右, 下, 左]
    /// </summary>
    private double[] _margin = { 25.4, 31.7, 25.4, 31.7 };

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="width">页面宽度</param>
    /// <param name="height">页面高度</param>
    public PageLayout(double width, double height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// 从StBox构造
    /// </summary>
    /// <param name="box">边界框</param>
    public PageLayout(StBox box)
    {
        if (box == null)
        {
            throw new ArgumentNullException(nameof(box), "box 为空");
        }
        Width = box.Width;
        Height = box.Height;
    }

    // 标准纸张尺寸静态方法
    public static PageLayout A0() => new(841, 1189);
    public static PageLayout A1() => new(594, 841);
    public static PageLayout A2() => new(420, 594);
    public static PageLayout A3() => new(297, 420);
    public static PageLayout A4() => new(210, 297);
    public static PageLayout A5() => new(148, 210);
    public static PageLayout A6() => new(105, 148);
    public static PageLayout A7() => new(74, 105);
    public static PageLayout A8() => new(52, 74);
    public static PageLayout A9() => new(37, 52);
    public static PageLayout A10() => new(26, 37);

    /// <summary>
    /// 设置外边距
    /// </summary>
    /// <param name="margin">边距数组</param>
    /// <returns>this</returns>
    public PageLayout SetMargin(params double[] margin)
    {
        _margin = ProcessMarginArray(margin);
        return this;
    }

    /// <summary>
    /// 获取外边距数组
    /// </summary>
    /// <returns>[上, 右, 下, 左]</returns>
    public double[] GetMargin() => (double[])_margin.Clone();

    /// <summary>
    /// 设置上边距
    /// </summary>
    public PageLayout SetMarginTop(double top)
    {
        _margin[0] = top;
        return this;
    }

    /// <summary>
    /// 获取上边距
    /// </summary>
    public double GetMarginTop() => _margin[0];

    /// <summary>
    /// 设置右边距
    /// </summary>
    public PageLayout SetMarginRight(double right)
    {
        _margin[1] = right;
        return this;
    }

    /// <summary>
    /// 获取右边距
    /// </summary>
    public double GetMarginRight() => _margin[1];

    /// <summary>
    /// 设置下边距
    /// </summary>
    public PageLayout SetMarginBottom(double bottom)
    {
        _margin[2] = bottom;
        return this;
    }

    /// <summary>
    /// 获取下边距
    /// </summary>
    public double GetMarginBottom() => _margin[2];

    /// <summary>
    /// 设置左边距
    /// </summary>
    public PageLayout SetMarginLeft(double left)
    {
        _margin[3] = left;
        return this;
    }

    /// <summary>
    /// 获取左边距
    /// </summary>
    public double GetMarginLeft() => _margin[3];

    /// <summary>
    /// 实际能放置内容的宽度
    /// </summary>
    public double ContentWidth => Width - GetMarginLeft() - GetMarginRight();

    /// <summary>
    /// 实际能放置内容的高度
    /// </summary>
    public double ContentHeight => Height - GetMarginTop() - GetMarginBottom();

    /// <summary>
    /// 绘制区域原点X
    /// </summary>
    public double StartX => GetMarginLeft();

    /// <summary>
    /// 绘制区域原点Y
    /// </summary>
    public double StartY => GetMarginTop();

    /// <summary>
    /// 页面正文的工作区域
    /// </summary>
    /// <returns>工作区域</returns>
    public Rectangle GetWorkerArea()
    {
        return new Rectangle(StartX, StartY, ContentWidth, ContentHeight);
    }

    /// <summary>
    /// 转换为CT_PageArea
    /// </summary>
    /// <returns>页面区域对象</returns>
    public StBox ToPageArea()
    {
        return new StBox(0, 0, Width, Height);
    }

    /// <summary>
    /// 处理边距数组，支持1-4个参数
    /// </summary>
    /// <param name="margin">边距参数</param>
    /// <returns>标准化的边距数组[上, 右, 下, 左]</returns>
    private static double[] ProcessMarginArray(double[] margin)
    {
        return margin?.Length switch
        {
            1 => new[] { margin[0], margin[0], margin[0], margin[0] },
            2 => new[] { margin[0], margin[1], margin[0], margin[1] },
            3 => new[] { margin[0], margin[1], margin[2], margin[1] },
            4 => new[] { margin[0], margin[1], margin[2], margin[3] },
            _ => new[] { 25.4, 31.7, 25.4, 31.7 } // 默认值
        };
    }

    /// <summary>
    /// 相等比较
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is PageLayout other)
        {
            return Math.Abs(Width - other.Width) < 0.001 && 
                   Math.Abs(Height - other.Height) < 0.001 &&
                   _margin.SequenceEqual(other._margin);
        }
        return false;
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height, _margin[0], _margin[1], _margin[2], _margin[3]);
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    public override string ToString()
    {
        return $"PageLayout({Width}×{Height}mm, Margin:[{GetMarginTop()},{GetMarginRight()},{GetMarginBottom()},{GetMarginLeft()}])";
    }
}

/// <summary>
/// 矩形区域
/// </summary>
public class Rectangle
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public Rectangle(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public override string ToString()
    {
        return $"Rectangle({X}, {Y}, {Width}, {Height})";
    }
}