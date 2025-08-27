using OfdrwNet.Core;

namespace OfdrwNet.Layout.Element;

/// <summary>
/// 盒式模型基础类
/// 
/// 每个继承Div的对象都提供泛型参数T，用于简化链式调用。
/// 
/// 对应 Java 版本的 org.ofdrw.layout.element.Div
/// </summary>
/// <typeparam name="T">链式调用返回值，Div的子类</typeparam>
public class Div<T> : IElement where T : Div<T>
{
    /// <summary>
    /// 背景颜色 (R,G,B) 三色数组
    /// </summary>
    public int[]? BackgroundColor { get; set; }

    /// <summary>
    /// 边框颜色 (R,G,B) 三色数组
    /// </summary>
    public int[]? BorderColor { get; set; }

    /// <summary>
    /// 内容宽度
    /// 如果不设置，则为自适应。最大宽度不能大于页面版心宽度。
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// 内容高度
    /// 如果不设置则为自适应。
    /// 注意如果需要保证块完整，那么高度不能大于版心高度。
    /// </summary>
    public double? Height { get; set; }

    /// <summary>
    /// 内边距
    /// 数组中个元素意义：上、右、下、左
    /// </summary>
    public double[] Padding { get; set; } = { 0d, 0d, 0d, 0d };

    /// <summary>
    /// 边框宽度
    /// 数组中个元素意义：上、右、下、左
    /// </summary>
    public double[] Border { get; set; } = { 0d, 0d, 0d, 0d };

    /// <summary>
    /// 外边距
    /// 数组中个元素意义：上、右、下、左
    /// </summary>
    public double[] Margin { get; set; } = { 0d, 0d, 0d, 0d };

    /// <summary>
    /// 边框虚线样式
    /// 数组中个元素意义：[偏移量, 虚线长,空白长, 虚线长,空白长, ...]
    /// </summary>
    public double[]? BorderDash { get; set; }

    /// <summary>
    /// 固定布局的盒式模型左上角X坐标
    /// </summary>
    public double? X { get; set; }

    /// <summary>
    /// 固定布局的盒式模型左上角Y坐标
    /// </summary>
    public double? Y { get; set; }

    /// <summary>
    /// 对段的占用情况
    /// </summary>
    public Clear Clear { get; set; } = Clear.Both;

    /// <summary>
    /// 在段中的浮动方向
    /// </summary>
    public AFloat Float { get; set; } = AFloat.Left;

    /// <summary>
    /// 相对于段的左边界距离
    /// </summary>
    public double Left { get; set; } = 0d;

    /// <summary>
    /// 相对于段的右边界距离
    /// </summary>
    public double Right { get; set; } = 0d;

    /// <summary>
    /// 相对坐标的top
    /// </summary>
    public double Top { get; set; } = 0d;

    /// <summary>
    /// 元素整体透明度
    /// null 表示不透明，取值区间 [0,1]
    /// </summary>
    public double? Opacity { get; set; }

    /// <summary>
    /// 元素定位方式，默认为静态定位
    /// </summary>
    public Position Position { get; set; } = Position.Static;

    /// <summary>
    /// 当渲染空间不足时是否拆分元素
    /// true为不拆分，false为拆分。默认值为false
    /// </summary>
    public bool Integrity { get; set; } = false;

    /// <summary>
    /// 占位符，不参与渲染
    /// </summary>
    public bool Placeholder { get; set; } = false;

    /// <summary>
    /// 图层，默认为Body
    /// </summary>
    public LayerType Layer { get; set; } = LayerType.Body;

    /// <summary>
    /// 构造函数
    /// </summary>
    public Div()
    {
    }

    /// <summary>
    /// 使用宽高构造
    /// </summary>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public Div(double width, double height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// 创建绝对定位的Div对象
    /// </summary>
    /// <param name="x">固定布局的盒式模型左上角X坐标</param>
    /// <param name="y">固定布局的盒式模型左上角Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public Div(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Position = Position.Absolute;
    }

    /// <summary>
    /// 是否存在边框
    /// </summary>
    /// <returns>true 不存在；false 存在</returns>
    public bool IsNoBorder()
    {
        return GetBorderTop() == 0d &&
               GetBorderRight() == 0d &&
               GetBorderBottom() == 0d &&
               GetBorderLeft() == 0d;
    }

    /// <summary>
    /// 是否是块级元素
    /// </summary>
    /// <returns>true 是块级元素；false 不是</returns>
    public virtual bool IsBlockElement()
    {
        return true;
    }

    /// <summary>
    /// 获取上边框宽度
    /// </summary>
    /// <returns>上边框宽度</returns>
    public double GetBorderTop() => Border[0];

    /// <summary>
    /// 获取右边框宽度
    /// </summary>
    /// <returns>右边框宽度</returns>
    public double GetBorderRight() => Border[1];

    /// <summary>
    /// 获取下边框宽度
    /// </summary>
    /// <returns>下边框宽度</returns>
    public double GetBorderBottom() => Border[2];

    /// <summary>
    /// 获取左边框宽度
    /// </summary>
    /// <returns>左边框宽度</returns>
    public double GetBorderLeft() => Border[3];

    /// <summary>
    /// 设置宽度
    /// </summary>
    /// <param name="width">宽度</param>
    /// <returns>this</returns>
    public T SetWidth(double width)
    {
        Width = width;
        return (T)this;
    }

    /// <summary>
    /// 设置高度
    /// </summary>
    /// <param name="height">高度</param>
    /// <returns>this</returns>
    public T SetHeight(double height)
    {
        Height = height;
        return (T)this;
    }

    /// <summary>
    /// 设置位置
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>this</returns>
    public T SetPosition(double x, double y)
    {
        X = x;
        Y = y;
        Position = Position.Absolute;
        return (T)this;
    }

    /// <summary>
    /// 设置背景颜色
    /// </summary>
    /// <param name="r">红色分量</param>
    /// <param name="g">绿色分量</param>
    /// <param name="b">蓝色分量</param>
    /// <returns>this</returns>
    public T SetBackgroundColor(int r, int g, int b)
    {
        BackgroundColor = new[] { r, g, b };
        return (T)this;
    }

    /// <summary>
    /// 设置边框颜色
    /// </summary>
    /// <param name="r">红色分量</param>
    /// <param name="g">绿色分量</param>
    /// <param name="b">蓝色分量</param>
    /// <returns>this</returns>
    public T SetBorderColor(int r, int g, int b)
    {
        BorderColor = new[] { r, g, b };
        return (T)this;
    }

    /// <summary>
    /// 设置边框宽度
    /// </summary>
    /// <param name="width">边框宽度</param>
    /// <returns>this</returns>
    public T SetBorder(double width)
    {
        Border = new[] { width, width, width, width };
        return (T)this;
    }

    /// <summary>
    /// 设置内边距
    /// </summary>
    /// <param name="padding">内边距</param>
    /// <returns>this</returns>
    public T SetPadding(double padding)
    {
        Padding = new[] { padding, padding, padding, padding };
        return (T)this;
    }

    /// <summary>
    /// 设置外边距
    /// </summary>
    /// <param name="margin">外边距</param>
    /// <returns>this</returns>
    public T SetMargin(double margin)
    {
        Margin = new[] { margin, margin, margin, margin };
        return (T)this;
    }
}

/// <summary>
/// 基础Div类（非泛型）
/// </summary>
public class Div : Div<Div>
{
    public Div() { }
    public Div(double width, double height) : base(width, height) { }
    public Div(double x, double y, double width, double height) : base(x, y, width, height) { }
}

/// <summary>
/// 布局元素接口
/// </summary>
public interface IElement
{
    /// <summary>
    /// 是否是块级元素
    /// </summary>
    /// <returns>true 是块级元素；false 不是</returns>
    bool IsBlockElement();
}

