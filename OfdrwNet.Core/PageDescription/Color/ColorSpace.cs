using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.PageDescription.Color;

/// <summary>
/// 颜色空间基类
/// 
/// 颜色空间定义了颜色的表示方法，包括RGB、CMYK、灰度等。
/// 提供了颜色空间的基本属性和方法。
/// 
/// 对应OFD标准中的ColorSpace定义
/// 8.3.2 颜色空间 图 26 表 44
/// </summary>
public abstract class ColorSpace : OfdElement
{
    /// <summary>
    /// 从现有元素构造颜色空间
    /// </summary>
    /// <param name="element">XML元素</param>
    protected ColorSpace(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的颜色空间元素
    /// </summary>
    /// <param name="elementName">元素名称</param>
    protected ColorSpace(string elementName) : base(elementName)
    {
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置颜色空间类型
    /// </summary>
    /// <param name="type">颜色空间类型</param>
    /// <returns>this</returns>
    public ColorSpace SetType(ColorSpaceType type)
    {
        SetAttribute("Type", type.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取颜色空间类型
    /// </summary>
    /// <returns>颜色空间类型</returns>
    public new ColorSpaceType GetType()
    {
        var value = GetAttributeValue("Type");
        return string.IsNullOrEmpty(value) 
            ? ColorSpaceType.RGB 
            : Enum.Parse<ColorSpaceType>(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置颜色空间的名称标识
    /// </summary>
    /// <param name="name">颜色空间名称</param>
    /// <returns>this</returns>
    public ColorSpace SetName(string name)
    {
        SetAttribute("Name", name);
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取颜色空间的名称标识
    /// </summary>
    /// <returns>颜色空间名称</returns>
    public string? GetName()
    {
        return GetAttributeValue("Name");
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置ICC配置文件的路径
    /// </summary>
    /// <param name="profile">ICC配置文件路径</param>
    /// <returns>this</returns>
    public ColorSpace SetProfile(StLoc profile)
    {
        SetAttribute("Profile", profile.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取ICC配置文件的路径
    /// </summary>
    /// <returns>ICC配置文件路径</returns>
    public StLoc? GetProfile()
    {
        var value = GetAttributeValue("Profile");
        return string.IsNullOrEmpty(value) ? null : new StLoc(value);
    }
}

/// <summary>
/// RGB颜色空间
/// 
/// RGB（红绿蓝）颜色空间，基于加色混合原理。
/// 适用于显示器和电子设备的颜色表示。
/// </summary>
public class RgbColorSpace : ColorSpace
{
    /// <summary>
    /// 从现有元素构造RGB颜色空间
    /// </summary>
    /// <param name="element">XML元素</param>
    public RgbColorSpace(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的RGB颜色空间
    /// </summary>
    public RgbColorSpace() : base("ColorSpace")
    {
        SetType(ColorSpaceType.RGB);
    }

    /// <summary>
    /// 构造RGB颜色空间并指定名称
    /// </summary>
    /// <param name="name">颜色空间名称</param>
    public RgbColorSpace(string name) : this()
    {
        SetName(name);
    }
}

/// <summary>
/// CMYK颜色空间
/// 
/// CMYK（青品黄黑）颜色空间，基于减色混合原理。
/// 适用于印刷和打印设备的颜色表示。
/// </summary>
public class CmykColorSpace : ColorSpace
{
    /// <summary>
    /// 从现有元素构造CMYK颜色空间
    /// </summary>
    /// <param name="element">XML元素</param>
    public CmykColorSpace(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的CMYK颜色空间
    /// </summary>
    public CmykColorSpace() : base("ColorSpace")
    {
        SetType(ColorSpaceType.CMYK);
    }

    /// <summary>
    /// 构造CMYK颜色空间并指定名称
    /// </summary>
    /// <param name="name">颜色空间名称</param>
    public CmykColorSpace(string name) : this()
    {
        SetName(name);
    }
}

/// <summary>
/// 灰度颜色空间
/// 
/// Gray（灰度）颜色空间，仅包含明度信息。
/// 适用于黑白或灰度图像和文档。
/// </summary>
public class GrayColorSpace : ColorSpace
{
    /// <summary>
    /// 从现有元素构造灰度颜色空间
    /// </summary>
    /// <param name="element">XML元素</param>
    public GrayColorSpace(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的灰度颜色空间
    /// </summary>
    public GrayColorSpace() : base("ColorSpace")
    {
        SetType(ColorSpaceType.Gray);
    }

    /// <summary>
    /// 构造灰度颜色空间并指定名称
    /// </summary>
    /// <param name="name">颜色空间名称</param>
    public GrayColorSpace(string name) : this()
    {
        SetName(name);
    }
}

/// <summary>
/// 颜色空间类型枚举
/// </summary>
public enum ColorSpaceType
{
    /// <summary>
    /// RGB颜色空间（红绿蓝）
    /// </summary>
    RGB,

    /// <summary>
    /// CMYK颜色空间（青品黄黑）
    /// </summary>
    CMYK,

    /// <summary>
    /// 灰度颜色空间
    /// </summary>
    Gray,

    /// <summary>
    /// Lab颜色空间
    /// </summary>
    Lab,

    /// <summary>
    /// 索引颜色空间
    /// </summary>
    Indexed
}

/// <summary>
/// 颜色空间集合
/// 
/// 包含文档中使用的所有颜色空间定义。
/// 用于资源管理和颜色空间引用。
/// </summary>
public class ColorSpaces : OfdElement
{
    /// <summary>
    /// 从现有元素构造颜色空间集合
    /// </summary>
    /// <param name="element">XML元素</param>
    public ColorSpaces(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的颜色空间集合
    /// </summary>
    public ColorSpaces() : base("ColorSpaces")
    {
    }

    /// <summary>
    /// 添加颜色空间
    /// </summary>
    /// <param name="colorSpace">颜色空间</param>
    /// <returns>this</returns>
    public ColorSpaces AddColorSpace(ColorSpace colorSpace)
    {
        Add(colorSpace);
        return this;
    }

    /// <summary>
    /// 添加RGB颜色空间
    /// </summary>
    /// <param name="name">颜色空间名称</param>
    /// <returns>this</returns>
    public ColorSpaces AddRgbColorSpace(string name)
    {
        return AddColorSpace(new RgbColorSpace(name));
    }

    /// <summary>
    /// 添加CMYK颜色空间
    /// </summary>
    /// <param name="name">颜色空间名称</param>
    /// <returns>this</returns>
    public ColorSpaces AddCmykColorSpace(string name)
    {
        return AddColorSpace(new CmykColorSpace(name));
    }

    /// <summary>
    /// 添加灰度颜色空间
    /// </summary>
    /// <param name="name">颜色空间名称</param>
    /// <returns>this</returns>
    public ColorSpaces AddGrayColorSpace(string name)
    {
        return AddColorSpace(new GrayColorSpace(name));
    }

    /// <summary>
    /// 获取所有颜色空间
    /// </summary>
    /// <returns>颜色空间列表</returns>
    public List<ColorSpace> GetColorSpaces()
    {
        var colorSpaces = new List<ColorSpace>();
        
        foreach (var element in Element.Elements())
        {
            var type = element.Attribute("Type")?.Value ?? "RGB";
            ColorSpace colorSpace = type switch
            {
                "RGB" => new RgbColorSpace(element),
                "CMYK" => new CmykColorSpace(element), 
                "Gray" => new GrayColorSpace(element),
                _ => new RgbColorSpace(element)
            };
            colorSpaces.Add(colorSpace);
        }
        
        return colorSpaces;
    }

    /// <summary>
    /// 根据名称查找颜色空间
    /// </summary>
    /// <param name="name">颜色空间名称</param>
    /// <returns>颜色空间或null</returns>
    public ColorSpace? FindColorSpace(string name)
    {
        return GetColorSpaces().FirstOrDefault(cs => cs.GetName() == name);
    }

    /// <summary>
    /// 获取颜色空间数量
    /// </summary>
    /// <returns>颜色空间数量</returns>
    public int GetColorSpaceCount()
    {
        return Element.Elements().Count();
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:ColorSpaces";
}
