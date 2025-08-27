using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.PageDescription.DrawParam;

/// <summary>
/// 填充颜色
/// 
/// 定义图形对象的填充颜色，支持纯色填充、渐变填充等。
/// 填充颜色是绘制参数的重要组成部分。
/// 
/// 对应OFD标准中的FillColor定义
/// 8.3.3 颜色 图 27 表 45
/// </summary>
public class FillColor : OfdElement
{
    /// <summary>
    /// 从现有元素构造填充颜色
    /// </summary>
    /// <param name="element">XML元素</param>
    public FillColor(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的填充颜色元素
    /// </summary>
    public FillColor() : base("FillColor")
    {
    }

    /// <summary>
    /// 使用颜色值构造填充颜色
    /// </summary>
    /// <param name="color">颜色值</param>
    public FillColor(StArray color) : base("FillColor")
    {
        SetColor(color);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置颜色值
    /// </summary>
    /// <param name="color">颜色值</param>
    /// <returns>this</returns>
    public FillColor SetColor(StArray color)
    {
        SetAttribute("Value", color.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取颜色值
    /// </summary>
    /// <returns>颜色值</returns>
    public StArray? GetColor()
    {
        var value = GetAttributeValue("Value");
        return string.IsNullOrEmpty(value) ? null : StArray.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置颜色空间引用
    /// </summary>
    /// <param name="colorSpace">颜色空间引用</param>
    /// <returns>this</returns>
    public FillColor SetColorSpace(StRefId colorSpace)
    {
        SetAttribute("ColorSpace", colorSpace.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取颜色空间引用
    /// </summary>
    /// <returns>颜色空间引用</returns>
    public StRefId? GetColorSpace()
    {
        var value = GetAttributeValue("ColorSpace");
        return string.IsNullOrEmpty(value) ? null : StRefId.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置透明度
    /// </summary>
    /// <param name="alpha">透明度（0-255）</param>
    /// <returns>this</returns>
    public FillColor SetAlpha(int alpha)
    {
        if (alpha < 0 || alpha > 255)
            throw new ArgumentException("Alpha value must be between 0 and 255", nameof(alpha));
        
        SetAttribute("Alpha", alpha.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取透明度
    /// </summary>
    /// <returns>透明度，默认255（完全不透明）</returns>
    public int GetAlpha()
    {
        var value = GetAttributeValue("Alpha");
        return string.IsNullOrEmpty(value) ? 255 : int.Parse(value);
    }
}

/// <summary>
/// 描边颜色
/// 
/// 定义图形对象的描边颜色，用于绘制轮廓线条。
/// 描边颜色与填充颜色配合使用，完成图形的完整绘制。
/// 
/// 对应OFD标准中的StrokeColor定义
/// </summary>
public class StrokeColor : OfdElement
{
    /// <summary>
    /// 从现有元素构造描边颜色
    /// </summary>
    /// <param name="element">XML元素</param>
    public StrokeColor(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的描边颜色元素
    /// </summary>
    public StrokeColor() : base("StrokeColor")
    {
    }

    /// <summary>
    /// 使用颜色值构造描边颜色
    /// </summary>
    /// <param name="color">颜色值</param>
    public StrokeColor(StArray color) : base("StrokeColor")
    {
        SetColor(color);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置颜色值
    /// </summary>
    /// <param name="color">颜色值</param>
    /// <returns>this</returns>
    public StrokeColor SetColor(StArray color)
    {
        SetAttribute("Value", color.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取颜色值
    /// </summary>
    /// <returns>颜色值</returns>
    public StArray? GetColor()
    {
        var value = GetAttributeValue("Value");
        return string.IsNullOrEmpty(value) ? null : StArray.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置颜色空间引用
    /// </summary>
    /// <param name="colorSpace">颜色空间引用</param>
    /// <returns>this</returns>
    public StrokeColor SetColorSpace(StRefId colorSpace)
    {
        SetAttribute("ColorSpace", colorSpace.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取颜色空间引用
    /// </summary>
    /// <returns>颜色空间引用</returns>
    public StRefId? GetColorSpace()
    {
        var value = GetAttributeValue("ColorSpace");
        return string.IsNullOrEmpty(value) ? null : StRefId.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置透明度
    /// </summary>
    /// <param name="alpha">透明度（0-255）</param>
    /// <returns>this</returns>
    public StrokeColor SetAlpha(int alpha)
    {
        if (alpha < 0 || alpha > 255)
            throw new ArgumentException("Alpha value must be between 0 and 255", nameof(alpha));
        
        SetAttribute("Alpha", alpha.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取透明度
    /// </summary>
    /// <returns>透明度，默认255（完全不透明）</returns>
    public int GetAlpha()
    {
        var value = GetAttributeValue("Alpha");
        return string.IsNullOrEmpty(value) ? 255 : int.Parse(value);
    }
}

/// <summary>
/// 线宽
/// 
/// 定义图形对象的线条宽度，单位通常为毫米。
/// 线宽影响描边的粗细程度。
/// 
/// 对应OFD标准中的LineWidth定义
/// </summary>
public class LineWidth : OfdElement
{
    /// <summary>
    /// 从现有元素构造线宽
    /// </summary>
    /// <param name="element">XML元素</param>
    public LineWidth(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的线宽元素
    /// </summary>
    public LineWidth() : base("LineWidth")
    {
    }

    /// <summary>
    /// 使用宽度值构造线宽
    /// </summary>
    /// <param name="width">线宽值</param>
    public LineWidth(double width) : base("LineWidth")
    {
        SetWidth(width);
    }

    /// <summary>
    /// 设置线宽值
    /// </summary>
    /// <param name="width">线宽值</param>
    /// <returns>this</returns>
    public LineWidth SetWidth(double width)
    {
        if (width < 0)
            throw new ArgumentException("Line width must be non-negative", nameof(width));
        
        Element.Value = width.ToString();
        return this;
    }

    /// <summary>
    /// 获取线宽值
    /// </summary>
    /// <returns>线宽值</returns>
    public double GetWidth()
    {
        var value = Element.Value;
        return string.IsNullOrEmpty(value) ? 1.0 : double.Parse(value);
    }
}

/// <summary>
/// 虚线模式
/// 
/// 定义虚线的绘制模式，包括虚线段长度、间隔等。
/// 用于绘制虚线、点线等特殊线型。
/// 
/// 对应OFD标准中的DashPattern定义
/// </summary>
public class DashPattern : OfdElement
{
    /// <summary>
    /// 从现有元素构造虚线模式
    /// </summary>
    /// <param name="element">XML元素</param>
    public DashPattern(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的虚线模式元素
    /// </summary>
    public DashPattern() : base("DashPattern")
    {
    }

    /// <summary>
    /// 使用模式数组构造虚线模式
    /// </summary>
    /// <param name="pattern">虚线模式数组</param>
    public DashPattern(double[] pattern) : base("DashPattern")
    {
        SetPattern(pattern);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置虚线偏移
    /// </summary>
    /// <param name="offset">虚线偏移值</param>
    /// <returns>this</returns>
    public DashPattern SetOffset(double offset)
    {
        SetAttribute("Offset", offset.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取虚线偏移
    /// </summary>
    /// <returns>虚线偏移值</returns>
    public double GetOffset()
    {
        var value = GetAttributeValue("Offset");
        return string.IsNullOrEmpty(value) ? 0.0 : double.Parse(value);
    }

    /// <summary>
    /// 设置虚线模式数组
    /// </summary>
    /// <param name="pattern">虚线模式数组</param>
    /// <returns>this</returns>
    public DashPattern SetPattern(double[] pattern)
    {
        if (pattern == null || pattern.Length == 0)
            throw new ArgumentException("Dash pattern cannot be null or empty", nameof(pattern));
        
        var patternStr = string.Join(" ", pattern.Select(p => p.ToString()));
        Element.Value = patternStr;
        return this;
    }

    /// <summary>
    /// 获取虚线模式数组
    /// </summary>
    /// <returns>虚线模式数组</returns>
    public double[] GetPattern()
    {
        var value = Element.Value;
        if (string.IsNullOrEmpty(value))
            return new double[0];
        
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                   .Select(double.Parse)
                   .ToArray();
    }

    /// <summary>
    /// 添加虚线段长度
    /// </summary>
    /// <param name="length">虚线段长度</param>
    /// <returns>this</returns>
    public DashPattern AddDash(double length)
    {
        var pattern = GetPattern();
        var newPattern = new double[pattern.Length + 1];
        Array.Copy(pattern, newPattern, pattern.Length);
        newPattern[pattern.Length] = length;
        return SetPattern(newPattern);
    }

    /// <summary>
    /// 是否为实线模式
    /// </summary>
    /// <returns>是否为实线</returns>
    public bool IsSolid()
    {
        var pattern = GetPattern();
        return pattern.Length == 0;
    }
}

/// <summary>
/// 连接方式
/// 
/// 定义线条连接的方式，如尖角连接、圆角连接等。
/// 影响路径中线段连接处的外观。
/// 
/// 对应OFD标准中的Join定义
/// </summary>
public class Join : OfdElement
{
    /// <summary>
    /// 从现有元素构造连接方式
    /// </summary>
    /// <param name="element">XML元素</param>
    public Join(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的连接方式元素
    /// </summary>
    public Join() : base("Join")
    {
    }

    /// <summary>
    /// 使用连接类型构造连接方式
    /// </summary>
    /// <param name="joinType">连接类型</param>
    public Join(LineJoinType joinType) : base("Join")
    {
        SetJoinType(joinType);
    }

    /// <summary>
    /// 设置连接类型
    /// </summary>
    /// <param name="joinType">连接类型</param>
    /// <returns>this</returns>
    public Join SetJoinType(LineJoinType joinType)
    {
        Element.Value = joinType.ToString();
        return this;
    }

    /// <summary>
    /// 获取连接类型
    /// </summary>
    /// <returns>连接类型</returns>
    public LineJoinType GetJoinType()
    {
        var value = Element.Value;
        return Enum.TryParse<LineJoinType>(value, out var joinType) 
            ? joinType 
            : LineJoinType.Miter; // 默认值
    }
}

/// <summary>
/// 端点样式
/// 
/// 定义线条端点的样式，如平头、圆头、方头等。
/// 影响开放路径两端的外观。
/// 
/// 对应OFD标准中的Cap定义
/// </summary>
public class Cap : OfdElement
{
    /// <summary>
    /// 从现有元素构造端点样式
    /// </summary>
    /// <param name="element">XML元素</param>
    public Cap(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的端点样式元素
    /// </summary>
    public Cap() : base("Cap")
    {
    }

    /// <summary>
    /// 使用端点类型构造端点样式
    /// </summary>
    /// <param name="capType">端点类型</param>
    public Cap(LineCapType capType) : base("Cap")
    {
        SetCapType(capType);
    }

    /// <summary>
    /// 设置端点类型
    /// </summary>
    /// <param name="capType">端点类型</param>
    /// <returns>this</returns>
    public Cap SetCapType(LineCapType capType)
    {
        Element.Value = capType.ToString();
        return this;
    }

    /// <summary>
    /// 获取端点类型
    /// </summary>
    /// <returns>端点类型</returns>
    public LineCapType GetCapType()
    {
        var value = Element.Value;
        return Enum.TryParse<LineCapType>(value, out var capType) 
            ? capType 
            : LineCapType.Butt; // 默认值
    }
}

/// <summary>
/// 斜接限制
/// 
/// 定义斜接连接的长度限制，防止尖角过长。
/// 当连接角度很小时，斜接长度可能变得很大。
/// 
/// 对应OFD标准中的MiterLimit定义
/// </summary>
public class MiterLimit : OfdElement
{
    /// <summary>
    /// 从现有元素构造斜接限制
    /// </summary>
    /// <param name="element">XML元素</param>
    public MiterLimit(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的斜接限制元素
    /// </summary>
    public MiterLimit() : base("MiterLimit")
    {
    }

    /// <summary>
    /// 使用限制值构造斜接限制
    /// </summary>
    /// <param name="limit">斜接限制值</param>
    public MiterLimit(double limit) : base("MiterLimit")
    {
        SetLimit(limit);
    }

    /// <summary>
    /// 设置斜接限制值
    /// </summary>
    /// <param name="limit">斜接限制值</param>
    /// <returns>this</returns>
    public MiterLimit SetLimit(double limit)
    {
        if (limit < 1.0)
            throw new ArgumentException("Miter limit must be >= 1.0", nameof(limit));
        
        Element.Value = limit.ToString();
        return this;
    }

    /// <summary>
    /// 获取斜接限制值
    /// </summary>
    /// <returns>斜接限制值</returns>
    public double GetLimit()
    {
        var value = Element.Value;
        return string.IsNullOrEmpty(value) ? 10.0 : double.Parse(value); // 默认值10.0
    }
}
