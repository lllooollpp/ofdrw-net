using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.PageDescription;
using OfdrwNet.Core.PageDescription.Color;

namespace OfdrwNet.Core.Graph;

/// <summary>
/// 路径图形对象基类
/// 
/// 图形对象具有一般图元的一切属性和行为特征。
/// 路径对象用于定义各种几何形状，包括直线、曲线、多边形等。
/// 
/// 对应Java版本：org.ofdrw.core.graph.pathObj.CT_Path
/// OFD标准 9.1 图形对象 图 46 表 35
/// </summary>
public class CtPath : CtGraphicUnit<CtPath>, IClipAble
{
    /// <summary>
    /// 从现有XML元素构造路径对象
    /// </summary>
    /// <param name="element">XML元素</param>
    public CtPath(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的路径对象
    /// </summary>
    public CtPath() : base("Path")
    {
    }

    /// <summary>
    /// 使用指定名称构造路径对象
    /// </summary>
    /// <param name="name">元素名称</param>
    protected CtPath(string name) : base(name)
    {
    }

    #region 绘制属性

    /// <summary>
    /// 【可选 属性】
    /// 设置图形是否被勾边
    /// </summary>
    /// <param name="stroke">true - 勾边；false - 不勾边</param>
    /// <returns>this</returns>
    public CtPath SetStroke(bool? stroke)
    {
        if (stroke == null)
        {
            RemoveAttribute("Stroke");
            return this;
        }
        SetAttribute("Stroke", stroke.Value.ToString().ToLower());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图形是否被勾边
    /// </summary>
    /// <returns>true - 勾边；false - 不勾边</returns>
    public bool? GetStroke()
    {
        var value = GetAttributeValue("Stroke");
        return string.IsNullOrEmpty(value) ? null : bool.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置图形是否被填充
    /// </summary>
    /// <param name="fill">true - 填充；false - 不填充</param>
    /// <returns>this</returns>
    public CtPath SetFill(bool? fill)
    {
        if (fill == null)
        {
            RemoveAttribute("Fill");
            return this;
        }
        SetAttribute("Fill", fill.Value.ToString().ToLower());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图形是否被填充
    /// </summary>
    /// <returns>true - 填充；false - 不填充</returns>
    public bool? GetFill()
    {
        var value = GetAttributeValue("Fill");
        return string.IsNullOrEmpty(value) ? null : bool.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置填充规则
    /// </summary>
    /// <param name="rule">填充规则</param>
    /// <returns>this</returns>
    public CtPath SetRule(FillRule rule)
    {
        SetAttribute("Rule", rule.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取填充规则
    /// </summary>
    /// <returns>填充规则，默认为NonZero</returns>
    public FillRule GetRule()
    {
        var value = GetAttributeValue("Rule");
        return string.IsNullOrEmpty(value) ? FillRule.NonZero : Enum.Parse<FillRule>(value);
    }

    #endregion

    #region 颜色设置

    /// <summary>
    /// 【可选】
    /// 设置勾边颜色
    /// </summary>
    /// <param name="strokeColor">勾边颜色，颜色对象将被复制</param>
    /// <returns>this</returns>
    public CtPath SetStrokeColor(CtColor? strokeColor)
    {
        if (strokeColor == null)
        {
            RemoveOfdElementsByNames("StrokeColor");
            return this;
        }

        var copy = new StrokeColor(new XElement(strokeColor.ToXElement()));
        RemoveOfdElementsByNames("StrokeColor");
        Set(copy);
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取勾边颜色
    /// 默认为黑色
    /// </summary>
    /// <returns>勾边颜色，null表示为黑色</returns>
    public StrokeColor? GetStrokeColor()
    {
        var element = GetOfdElement("StrokeColor");
        return element != null ? new StrokeColor(element) : null;
    }

    /// <summary>
    /// 【可选】
    /// 设置填充颜色
    /// 默认为透明色
    /// </summary>
    /// <param name="fillColor">填充颜色，颜色对象将被复制</param>
    /// <returns>this</returns>
    public CtPath SetFillColor(CtColor? fillColor)
    {
        if (fillColor == null)
        {
            RemoveOfdElementsByNames("FillColor");
            return this;
        }

        var copy = new FillColor(new XElement(fillColor.ToXElement()));
        RemoveOfdElementsByNames("FillColor");
        Set(copy);
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取填充颜色
    /// 默认为透明色
    /// </summary>
    /// <returns>填充颜色</returns>
    public FillColor? GetFillColor()
    {
        var element = GetOfdElement("FillColor");
        return element != null ? new FillColor(element) : null;
    }

    #endregion

    #region 路径数据

    /// <summary>
    /// 【可选】
    /// 设置路径缩写数据
    /// </summary>
    /// <param name="data">路径缩写数据</param>
    /// <returns>this</returns>
    public CtPath SetAbbreviatedData(string data)
    {
        RemoveOfdElementsByNames("AbbreviatedData");
        if (!string.IsNullOrEmpty(data))
        {
            AddOfdEntity("AbbreviatedData", data);
        }
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取路径缩写数据
    /// </summary>
    /// <returns>路径缩写数据</returns>
    public string? GetAbbreviatedData()
    {
        return GetOfdElement("AbbreviatedData")?.Value;
    }

    #endregion

    #region 转换和验证

    /// <summary>
    /// 验证路径对象是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public override bool IsValid()
    {
        // 路径对象至少需要有路径数据或边界框
        var boundary = GetBoundary();
        return !string.IsNullOrEmpty(GetAbbreviatedData()) || 
               (boundary != null && boundary.Width > 0 && boundary.Height > 0);
    }

    /// <summary>
    /// 创建路径对象副本
    /// </summary>
    /// <returns>路径对象副本</returns>
    public new CtPath Clone()
    {
        return new CtPath(new XElement(Element));
    }

    #endregion
}

/// <summary>
/// 填充规则枚举
/// </summary>
public enum FillRule
{
    /// <summary>
    /// 非零规则（默认）
    /// </summary>
    NonZero,
    
    /// <summary>
    /// 奇偶规则
    /// </summary>
    EvenOdd
}
