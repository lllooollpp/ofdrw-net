using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.PageDescription.Clips;

/// <summary>
/// 裁剪路径集合
/// 
/// 定义页面对象的裁剪区域，用于限制内容的可见范围。
/// 裁剪路径可以包含多个裁剪区域。
/// 
/// 对应OFD标准中的Clips定义
/// 8.5 裁剪 图 32 表 52
/// </summary>
public class Clips : OfdElement
{
    /// <summary>
    /// 从现有元素构造裁剪路径集合
    /// </summary>
    /// <param name="element">XML元素</param>
    public Clips(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的裁剪路径集合
    /// </summary>
    public Clips() : base("Clips")
    {
    }

    /// <summary>
    /// 添加裁剪区域
    /// </summary>
    /// <param name="clipArea">裁剪区域</param>
    /// <returns>this</returns>
    public Clips AddClipArea(ClipArea clipArea)
    {
        Add(clipArea);
        return this;
    }

    /// <summary>
    /// 获取所有裁剪区域
    /// </summary>
    /// <returns>裁剪区域列表</returns>
    public List<ClipArea> GetClipAreas()
    {
        return Element.Elements(Const.OfdNamespace + "Area")
            .Select(e => new ClipArea(e))
            .ToList();
    }

    /// <summary>
    /// 获取裁剪区域数量
    /// </summary>
    /// <returns>裁剪区域数量</returns>
    public int GetClipAreaCount()
    {
        return Element.Elements(Const.OfdNamespace + "Area").Count();
    }

    /// <summary>
    /// 清除所有裁剪区域
    /// </summary>
    /// <returns>this</returns>
    public Clips ClearClipAreas()
    {
        Element.Elements(Const.OfdNamespace + "Area").Remove();
        return this;
    }

    /// <summary>
    /// 是否有裁剪区域
    /// </summary>
    /// <returns>是否有裁剪区域</returns>
    public bool HasClipAreas()
    {
        return GetClipAreaCount() > 0;
    }

    /// <summary>
    /// 添加矩形裁剪区域
    /// </summary>
    /// <param name="boundary">矩形边界</param>
    /// <returns>this</returns>
    public Clips AddRectangleClip(StBox boundary)
    {
        var clipArea = new ClipArea();
        clipArea.SetBoundary(boundary);
        return AddClipArea(clipArea);
    }

    /// <summary>
    /// 添加路径裁剪区域
    /// </summary>
    /// <param name="pathData">路径数据</param>
    /// <returns>this</returns>
    public Clips AddPathClip(string pathData)
    {
        var clipArea = new ClipArea();
        clipArea.SetPath(pathData);
        return AddClipArea(clipArea);
    }
}

/// <summary>
/// 裁剪区域
/// 
/// 定义单个裁剪区域的具体形状和位置。
/// 可以是矩形区域或复杂的路径形状。
/// 
/// 对应OFD标准中的ClipArea定义
/// </summary>
public class ClipArea : OfdElement
{
    /// <summary>
    /// 从现有元素构造裁剪区域
    /// </summary>
    /// <param name="element">XML元素</param>
    public ClipArea(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的裁剪区域
    /// </summary>
    public ClipArea() : base("Area")
    {
    }

    /// <summary>
    /// 使用边界框构造裁剪区域
    /// </summary>
    /// <param name="boundary">边界框</param>
    public ClipArea(StBox boundary) : base("Area")
    {
        SetBoundary(boundary);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置裁剪区域类型
    /// </summary>
    /// <param name="type">裁剪类型</param>
    /// <returns>this</returns>
    public ClipArea SetType(ClipType type)
    {
        SetAttribute("Type", type.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取裁剪区域类型
    /// </summary>
    /// <returns>裁剪类型</returns>
    public new ClipType GetType()
    {
        var value = GetAttributeValue("Type");
        return Enum.TryParse<ClipType>(value, out var type) 
            ? type 
            : ClipType.Path; // 默认值
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置边界框
    /// </summary>
    /// <param name="boundary">边界框</param>
    /// <returns>this</returns>
    public ClipArea SetBoundary(StBox boundary)
    {
        SetAttribute("Boundary", boundary.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取边界框
    /// </summary>
    /// <returns>边界框</returns>
    public StBox? GetBoundary()
    {
        var value = GetAttributeValue("Boundary");
        return string.IsNullOrEmpty(value) ? null : StBox.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置裁剪规则
    /// </summary>
    /// <param name="rule">裁剪规则</param>
    /// <returns>this</returns>
    public ClipArea SetClipRule(ClipRule rule)
    {
        SetAttribute("Rule", rule.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取裁剪规则
    /// </summary>
    /// <returns>裁剪规则</returns>
    public ClipRule GetClipRule()
    {
        var value = GetAttributeValue("Rule");
        return Enum.TryParse<ClipRule>(value, out var rule) 
            ? rule 
            : ClipRule.NonZero; // 默认值
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置坐标变换矩阵
    /// </summary>
    /// <param name="ctm">坐标变换矩阵</param>
    /// <returns>this</returns>
    public ClipArea SetCTM(StArray ctm)
    {
        SetAttribute("CTM", ctm.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取坐标变换矩阵
    /// </summary>
    /// <returns>坐标变换矩阵</returns>
    public StArray? GetCTM()
    {
        var value = GetAttributeValue("CTM");
        return string.IsNullOrEmpty(value) ? null : StArray.Parse(value);
    }

    /// <summary>
    /// 设置路径数据
    /// </summary>
    /// <param name="pathData">路径数据</param>
    /// <returns>this</returns>
    public ClipArea SetPath(string pathData)
    {
        Element.Value = pathData;
        SetType(ClipType.Path);
        return this;
    }

    /// <summary>
    /// 获取路径数据
    /// </summary>
    /// <returns>路径数据</returns>
    public string? GetPath()
    {
        return Element.Value;
    }

    /// <summary>
    /// 是否为矩形裁剪
    /// </summary>
    /// <returns>是否为矩形裁剪</returns>
    public bool IsRectangle()
    {
        var boundary = GetBoundary();
        return GetType() == ClipType.Rectangle && boundary is not null;
    }

    /// <summary>
    /// 是否为路径裁剪
    /// </summary>
    /// <returns>是否为路径裁剪</returns>
    public bool IsPath()
    {
        return GetType() == ClipType.Path && !string.IsNullOrEmpty(GetPath());
    }

    /// <summary>
    /// 验证裁剪区域是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValid()
    {
        return IsRectangle() || IsPath();
    }
}

/// <summary>
/// 裁剪类型枚举
/// </summary>
public enum ClipType
{
    /// <summary>
    /// 路径裁剪
    /// </summary>
    Path,

    /// <summary>
    /// 矩形裁剪
    /// </summary>
    Rectangle
}

/// <summary>
/// 裁剪规则枚举
/// </summary>
public enum ClipRule
{
    /// <summary>
    /// 非零环绕规则
    /// </summary>
    NonZero,

    /// <summary>
    /// 奇偶规则
    /// </summary>
    EvenOdd
}

/// <summary>
/// 复合裁剪区域
/// 
/// 支持多个裁剪区域的组合操作，如并集、交集等。
/// 用于创建复杂的裁剪形状。
/// </summary>
public class CompositeClipArea : ClipArea
{
    /// <summary>
    /// 从现有元素构造复合裁剪区域
    /// </summary>
    /// <param name="element">XML元素</param>
    public CompositeClipArea(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的复合裁剪区域
    /// </summary>
    public CompositeClipArea() : base()
    {
        Element.Name = Const.OfdNamespace + "CompositeArea";
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置组合操作类型
    /// </summary>
    /// <param name="operation">组合操作类型</param>
    /// <returns>this</returns>
    public CompositeClipArea SetOperation(ClipOperation operation)
    {
        SetAttribute("Operation", operation.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取组合操作类型
    /// </summary>
    /// <returns>组合操作类型</returns>
    public ClipOperation GetOperation()
    {
        var value = GetAttributeValue("Operation");
        return Enum.TryParse<ClipOperation>(value, out var operation) 
            ? operation 
            : ClipOperation.Union; // 默认值
    }

    /// <summary>
    /// 添加子裁剪区域
    /// </summary>
    /// <param name="clipArea">子裁剪区域</param>
    /// <returns>this</returns>
    public CompositeClipArea AddChildClipArea(ClipArea clipArea)
    {
        Add(clipArea);
        return this;
    }

    /// <summary>
    /// 获取所有子裁剪区域
    /// </summary>
    /// <returns>子裁剪区域列表</returns>
    public List<ClipArea> GetChildClipAreas()
    {
        return Element.Elements(Const.OfdNamespace + "Area")
            .Select(e => new ClipArea(e))
            .ToList();
    }

    /// <summary>
    /// 获取子裁剪区域数量
    /// </summary>
    /// <returns>子裁剪区域数量</returns>
    public int GetChildClipAreaCount()
    {
        return Element.Elements(Const.OfdNamespace + "Area").Count();
    }
}

/// <summary>
/// 裁剪组合操作枚举
/// </summary>
public enum ClipOperation
{
    /// <summary>
    /// 并集
    /// </summary>
    Union,

    /// <summary>
    /// 交集
    /// </summary>
    Intersection,

    /// <summary>
    /// 差集
    /// </summary>
    Difference,

    /// <summary>
    /// 异或
    /// </summary>
    Xor
}
