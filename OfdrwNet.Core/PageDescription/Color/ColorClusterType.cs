using System.Xml.Linq;

namespace OfdrwNet.Core.PageDescription.Color;

/// <summary>
/// 颜色族接口
/// 
/// 用于标识属于颜色的一种，颜色可以是基本颜色、底纹和渐变
/// 
/// 8.3.2 图 25 颜色结构
/// 
/// 对应 Java 版本的 org.ofdrw.core.pageDescription.color.color.ColorClusterType
/// </summary>
public interface IColorClusterType
{
    /// <summary>
    /// 获取XML元素
    /// </summary>
    XElement Element { get; }
    
    /// <summary>
    /// 获取限定名称
    /// </summary>
    string QualifiedName { get; }
}

/// <summary>
/// 颜色族工厂类
/// </summary>
public static class ColorClusterType
{
    /// <summary>
    /// 解析元素并获取对应的颜色族子类实例
    /// </summary>
    /// <param name="element">XML元素</param>
    /// <returns>子类实例，基本颜色、底纹和渐变</returns>
    /// <exception cref="ArgumentException">未知的元素类型不是ColorClusterType子类</exception>
    public static IColorClusterType GetInstance(XElement element)
    {
        var qName = element.Name.ToString();
        return qName switch
        {
            "ofd:Pattern" => new CtPattern(element),
            "ofd:AxialShd" => new CtAxialShd(element),
            "ofd:RadialShd" => new CtRadialShd(element),
            "ofd:GouraudShd" => new CtGouraudShd(element),
            "ofd:LaGouraudShd" => new CtLaGouraudShd(element),
            _ => throw new ArgumentException($"未知的元素类型不是颜色子类：{qName}")
        };
    }
}

/// <summary>
/// 底纹模式（占位符实现）
/// 待后续完善
/// </summary>
public class CtPattern : OfdElement, IColorClusterType
{
    public CtPattern(XElement element) : base(element) { }
    public CtPattern() : base("Pattern") { }
    public override string QualifiedName => "ofd:Pattern";
}

/// <summary>
/// 轴向渐变（占位符实现）
/// 待后续完善
/// </summary>
public class CtAxialShd : OfdElement, IColorClusterType
{
    public CtAxialShd(XElement element) : base(element) { }
    public CtAxialShd() : base("AxialShd") { }
    public override string QualifiedName => "ofd:AxialShd";
}

/// <summary>
/// 径向渐变（占位符实现）
/// 待后续完善
/// </summary>
public class CtRadialShd : OfdElement, IColorClusterType
{
    public CtRadialShd(XElement element) : base(element) { }
    public CtRadialShd() : base("RadialShd") { }
    public override string QualifiedName => "ofd:RadialShd";
}

/// <summary>
/// 高洛德渐变（占位符实现）
/// 待后续完善
/// </summary>
public class CtGouraudShd : OfdElement, IColorClusterType
{
    public CtGouraudShd(XElement element) : base(element) { }
    public CtGouraudShd() : base("GouraudShd") { }
    public override string QualifiedName => "ofd:GouraudShd";
}

/// <summary>
/// 拉格朗日-高洛德渐变（占位符实现）
/// 待后续完善
/// </summary>
public class CtLaGouraudShd : OfdElement, IColorClusterType
{
    public CtLaGouraudShd(XElement element) : base(element) { }
    public CtLaGouraudShd() : base("LaGouraudShd") { }
    public override string QualifiedName => "ofd:LaGouraudShd";
}