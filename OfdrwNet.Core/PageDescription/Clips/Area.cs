using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.PageDescription.Clips;

/// <summary>
/// 裁剪区域
/// 
/// 用一个图形或文字对象来描述裁剪区的一个组成部分，
/// 最终裁剪区是这些区域的并集。
/// 
/// 8.4 裁剪区 表 33
/// 
/// 对应 Java 版本的 org.ofdrw.core.pageDescription.clips.Area
/// </summary>
public class Area : OfdElement
{
    /// <summary>
    /// 从现有XML元素构造裁剪区域
    /// </summary>
    /// <param name="element">XML元素</param>
    public Area(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的裁剪区域
    /// </summary>
    public Area() : base("Area")
    {
    }

    /// <summary>
    /// 【可选 属性】设置引用资源文件中的绘制参数的标识
    /// 
    /// 线宽、结合点和端点样式等绘制特性对裁剪效果会产生影响，
    /// 有关绘制参数的描述见 8.2
    /// </summary>
    /// <param name="drawParam">引用资源文件中的绘制参数的标识</param>
    /// <returns>this</returns>
    public Area SetDrawParam(StRefId drawParam)
    {
        AddAttribute("DrawParam", drawParam.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】获取引用资源文件中的绘制参数的标识
    /// 
    /// 线宽、结合点和端点样式等绘制特性对裁剪效果会产生影响，
    /// 有关绘制参数的描述见 8.2
    /// </summary>
    /// <returns>引用资源文件中的绘制参数的标识</returns>
    public StRefId? GetDrawParam()
    {
        var value = GetAttributeValue("DrawParam");
        return string.IsNullOrEmpty(value) ? null : StRefId.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】设置变换矩阵
    /// 
    /// 针对对象坐标系，对Area下包含的 Path 和 Text 进行进一步的变换
    /// </summary>
    /// <param name="ctm">变换矩阵</param>
    /// <returns>this</returns>
    public Area SetCTM(StArray ctm)
    {
        AddAttribute("CTM", ctm.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】获取变换矩阵
    /// 
    /// 针对对象坐标系，对Area下包含的 Path 和 Text 进行进一步的变换
    /// </summary>
    /// <returns>变换矩阵</returns>
    public StArray? GetCTM()
    {
        var value = GetAttributeValue("CTM");
        return string.IsNullOrEmpty(value) ? null : StArray.Parse(value);
    }

    /// <summary>
    /// 【必选】设置裁剪对象
    /// 
    /// 裁剪对象可以是 CT_Text、CT_Path
    /// </summary>
    /// <param name="clipObj">裁剪对象</param>
    /// <returns>this</returns>
    public Area SetClipObj(IClipAble clipObj)
    {
        Add(new OfdElement(clipObj.Element));
        return this;
    }

    /// <summary>
    /// 【必选】获取裁剪对象
    /// 
    /// 裁剪对象可以是 CT_Text、CT_Path
    /// </summary>
    /// <returns>裁剪对象</returns>
    public IClipAble? GetClipObj()
    {
        var elements = Element.Elements().ToList();
        if (elements.Count == 0)
        {
            return null;
        }
        
        var firstElement = elements[0];
        return ClipAble.GetInstance(firstElement);
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:Area";
}

/// <summary>
/// 可裁剪对象接口
/// </summary>
public interface IClipAble
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
/// 可裁剪对象工厂类
/// </summary>
public static class ClipAble
{
    /// <summary>
    /// 解析元素并获取对应的可裁剪对象实例
    /// </summary>
    /// <param name="element">XML元素</param>
    /// <returns>可裁剪对象实例</returns>
    /// <exception cref="ArgumentException">未知的元素类型</exception>
    public static IClipAble GetInstance(XElement element)
    {
        var qName = element.Name.ToString();
        return qName switch
        {
            "ofd:Path" => new ClipPath(element),
            "ofd:Text" => new ClipText(element),
            _ => throw new ArgumentException($"未知的可裁剪对象类型：{qName}")
        };
    }
}

/// <summary>
/// 可裁剪路径对象（占位符实现）
/// 待后续完善
/// </summary>
public class ClipPath : OfdElement, IClipAble
{
    public ClipPath(XElement element) : base(element) { }
    public ClipPath() : base("Path") { }
    public override string QualifiedName => "ofd:Path";
}

/// <summary>
/// 可裁剪文本对象（占位符实现）
/// 待后续完善
/// </summary>
public class ClipText : OfdElement, IClipAble
{
    public ClipText(XElement element) : base(element) { }
    public ClipText() : base("Text") { }
    public override string QualifiedName => "ofd:Text";
}