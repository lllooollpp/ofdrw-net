using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Image;

/// <summary>
/// 图像对象
/// 
/// 图像对象表示页面上的图片内容，包括图片资源引用、位置、大小等属性。
/// 支持多种图像格式：JPEG、PNG、BMP、TIFF等。
/// 
/// 对应Java版本：org.ofdrw.core.image.ImageObject
/// 8.7 图像 图 34
/// </summary>
public class ImageObject : OfdElement
{
    /// <summary>
    /// 从现有元素构造图像对象
    /// </summary>
    /// <param name="element">XML元素</param>
    public ImageObject(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的图像对象元素
    /// </summary>
    public ImageObject() : base("ImageObject")
    {
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置对象标识
    /// </summary>
    /// <param name="id">对象标识</param>
    /// <returns>this</returns>
    public ImageObject SetID(StId id)
    {
        SetAttribute("ID", id.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取对象标识
    /// </summary>
    /// <returns>对象标识，可能为null</returns>
    public StId? GetID()
    {
        var value = GetAttributeValue("ID");
        return string.IsNullOrEmpty(value) ? null : StId.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置边界框
    /// 边界框确定了对象在页面中的显示位置和大小
    /// </summary>
    /// <param name="boundary">边界框</param>
    /// <returns>this</returns>
    public ImageObject SetBoundary(StBox boundary)
    {
        SetAttribute("Boundary", boundary.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取边界框
    /// </summary>
    /// <returns>边界框，可能为null</returns>
    public StBox? GetBoundary()
    {
        var value = GetAttributeValue("Boundary");
        return string.IsNullOrEmpty(value) ? null : StBox.Parse(value);
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置图像资源引用
    /// </summary>
    /// <param name="resourceID">图像资源标识</param>
    /// <returns>this</returns>
    public ImageObject SetResourceID(StRefId resourceID)
    {
        SetAttribute("ResourceID", resourceID.ToString());
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取图像资源引用
    /// </summary>
    /// <returns>图像资源标识</returns>
    /// <exception cref="InvalidOperationException">ResourceID未设置时抛出</exception>
    public StRefId GetResourceID()
    {
        var value = GetAttributeValue("ResourceID");
        return string.IsNullOrEmpty(value) 
            ? throw new InvalidOperationException("ImageObject ResourceID is required") 
            : StRefId.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置图像替代文本
    /// 用于无障碍访问和图片无法显示时的描述
    /// </summary>
    /// <param name="altText">替代文本</param>
    /// <returns>this</returns>
    public ImageObject SetAltText(string altText)
    {
        SetAttribute("AltText", altText);
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图像替代文本
    /// </summary>
    /// <returns>替代文本，可能为null</returns>
    public string? GetAltText()
    {
        return GetAttributeValue("AltText");
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置图像透明度
    /// 值域：[0, 255]，0表示完全透明，255表示完全不透明
    /// </summary>
    /// <param name="alpha">透明度值</param>
    /// <returns>this</returns>
    public ImageObject SetAlpha(int alpha)
    {
        if (alpha < 0 || alpha > 255)
            throw new ArgumentException("Alpha value must be between 0 and 255", nameof(alpha));
        
        SetAttribute("Alpha", alpha.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图像透明度
    /// </summary>
    /// <returns>透明度值，默认255（不透明）</returns>
    public int GetAlpha()
    {
        var value = GetAttributeValue("Alpha");
        return string.IsNullOrEmpty(value) ? 255 : int.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置坐标变换矩阵
    /// </summary>
    /// <param name="ctm">坐标变换矩阵</param>
    /// <returns>this</returns>
    public ImageObject SetCTM(StArray ctm)
    {
        SetAttribute("CTM", ctm.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取坐标变换矩阵
    /// </summary>
    /// <returns>坐标变换矩阵，可能为null</returns>
    public StArray? GetCTM()
    {
        var value = GetAttributeValue("CTM");
        return string.IsNullOrEmpty(value) ? null : StArray.Parse(value);
    }
}

/// <summary>
/// 边框定义
/// 用于定义图像对象的边框样式
/// </summary>
public class Border : OfdElement
{
    /// <summary>
    /// 从现有元素构造边框
    /// </summary>
    /// <param name="element">XML元素</param>
    public Border(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的边框元素
    /// </summary>
    public Border() : base("Border")
    {
    }

    /// <summary>
    /// 设置边框线宽
    /// </summary>
    /// <param name="lineWidth">线宽</param>
    /// <returns>this</returns>
    public Border SetLineWidth(double lineWidth)
    {
        SetAttribute("LineWidth", lineWidth.ToString());
        return this;
    }

    /// <summary>
    /// 获取边框线宽
    /// </summary>
    /// <returns>线宽，默认1.0</returns>
    public double GetLineWidth()
    {
        var value = GetAttributeValue("LineWidth");
        return string.IsNullOrEmpty(value) ? 1.0 : double.Parse(value);
    }

    /// <summary>
    /// 设置边框颜色
    /// </summary>
    /// <param name="color">颜色值（如"#FF0000"或"255 0 0"）</param>
    /// <returns>this</returns>
    public Border SetColor(string color)
    {
        SetAttribute("Color", color);
        return this;
    }

    /// <summary>
    /// 获取边框颜色
    /// </summary>
    /// <returns>颜色值</returns>
    public string? GetColor()
    {
        return GetAttributeValue("Color");
    }
}

/// <summary>
/// 图像信息
/// 描述图像的基本属性和格式信息
/// </summary>
public class ImageInfo : OfdElement
{
    /// <summary>
    /// 从现有元素构造图像信息
    /// </summary>
    /// <param name="element">XML元素</param>
    public ImageInfo(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的图像信息元素
    /// </summary>
    public ImageInfo() : base("ImageInfo")
    {
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置图像格式
    /// </summary>
    /// <param name="format">图像格式（如"JPEG"、"PNG"等）</param>
    /// <returns>this</returns>
    public ImageInfo SetFormat(string format)
    {
        SetAttribute("Format", format);
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图像格式
    /// </summary>
    /// <returns>图像格式</returns>
    public string? GetFormat()
    {
        return GetAttributeValue("Format");
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置图像宽度（像素）
    /// </summary>
    /// <param name="width">图像宽度</param>
    /// <returns>this</returns>
    public ImageInfo SetWidth(int width)
    {
        SetAttribute("Width", width.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图像宽度
    /// </summary>
    /// <returns>图像宽度</returns>
    public int? GetWidth()
    {
        var value = GetAttributeValue("Width");
        return string.IsNullOrEmpty(value) ? null : int.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置图像高度（像素）
    /// </summary>
    /// <param name="height">图像高度</param>
    /// <returns>this</returns>
    public ImageInfo SetHeight(int height)
    {
        SetAttribute("Height", height.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图像高度
    /// </summary>
    /// <returns>图像高度</returns>
    public int? GetHeight()
    {
        var value = GetAttributeValue("Height");
        return string.IsNullOrEmpty(value) ? null : int.Parse(value);
    }
}