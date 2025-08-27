using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.PageObj;

/// <summary>
/// 物理边界框
/// 
/// 定义页面的物理范围，即页面在设备上的实际尺寸边界。
/// 物理边界框通常对应于纸张或显示屏的物理尺寸。
/// 
/// 对应OFD标准中的PhysicalBox定义
/// </summary>
public class PhysicalBox : OfdElement
{
    /// <summary>
    /// 从现有元素构造物理边界框
    /// </summary>
    /// <param name="element">XML元素</param>
    public PhysicalBox(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的物理边界框元素
    /// </summary>
    public PhysicalBox() : base("PhysicalBox")
    {
    }

    /// <summary>
    /// 使用边界框坐标构造物理边界框
    /// </summary>
    /// <param name="x">左上角X坐标</param>
    /// <param name="y">左上角Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public PhysicalBox(double x, double y, double width, double height) : base("PhysicalBox")
    {
        SetBoundary(new StBox(x, y, width, height));
    }

    /// <summary>
    /// 使用StBox构造物理边界框
    /// </summary>
    /// <param name="boundary">边界框</param>
    public PhysicalBox(StBox boundary) : base("PhysicalBox")
    {
        SetBoundary(boundary);
    }

    /// <summary>
    /// 设置边界框
    /// </summary>
    /// <param name="boundary">边界框坐标</param>
    /// <returns>this</returns>
    public PhysicalBox SetBoundary(StBox boundary)
    {
        Element.Value = boundary.ToString();
        return this;
    }

    /// <summary>
    /// 获取边界框
    /// </summary>
    /// <returns>边界框坐标</returns>
    public StBox GetBoundary()
    {
        var value = Element.Value;
        return string.IsNullOrEmpty(value) 
            ? new StBox(0, 0, 210, 297) // 默认A4尺寸
            : StBox.Parse(value);
    }

    /// <summary>
    /// 设置边界框坐标
    /// </summary>
    /// <param name="x">左上角X坐标</param>
    /// <param name="y">左上角Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>this</returns>
    public PhysicalBox SetBoundary(double x, double y, double width, double height)
    {
        return SetBoundary(new StBox(x, y, width, height));
    }

    /// <summary>
    /// 获取宽度
    /// </summary>
    /// <returns>宽度</returns>
    public double GetWidth()
    {
        return GetBoundary().Width;
    }

    /// <summary>
    /// 获取高度
    /// </summary>
    /// <returns>高度</returns>
    public double GetHeight()
    {
        return GetBoundary().Height;
    }
}

/// <summary>
/// 应用边界框
/// 
/// 定义页面内容的应用范围，即实际可用于放置内容的区域。
/// 应用边界框通常小于或等于物理边界框。
/// 
/// 对应OFD标准中的ApplicationBox定义
/// </summary>
public class ApplicationBox : OfdElement
{
    /// <summary>
    /// 从现有元素构造应用边界框
    /// </summary>
    /// <param name="element">XML元素</param>
    public ApplicationBox(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的应用边界框元素
    /// </summary>
    public ApplicationBox() : base("ApplicationBox")
    {
    }

    /// <summary>
    /// 使用边界框坐标构造应用边界框
    /// </summary>
    /// <param name="x">左上角X坐标</param>
    /// <param name="y">左上角Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public ApplicationBox(double x, double y, double width, double height) : base("ApplicationBox")
    {
        SetBoundary(new StBox(x, y, width, height));
    }

    /// <summary>
    /// 使用StBox构造应用边界框
    /// </summary>
    /// <param name="boundary">边界框</param>
    public ApplicationBox(StBox boundary) : base("ApplicationBox")
    {
        SetBoundary(boundary);
    }

    /// <summary>
    /// 设置边界框
    /// </summary>
    /// <param name="boundary">边界框坐标</param>
    /// <returns>this</returns>
    public ApplicationBox SetBoundary(StBox boundary)
    {
        Element.Value = boundary.ToString();
        return this;
    }

    /// <summary>
    /// 获取边界框
    /// </summary>
    /// <returns>边界框坐标</returns>
    public StBox GetBoundary()
    {
        var value = Element.Value;
        return string.IsNullOrEmpty(value) 
            ? new StBox(0, 0, 210, 297) // 默认A4尺寸
            : StBox.Parse(value);
    }

    /// <summary>
    /// 设置边界框坐标
    /// </summary>
    /// <param name="x">左上角X坐标</param>
    /// <param name="y">左上角Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>this</returns>
    public ApplicationBox SetBoundary(double x, double y, double width, double height)
    {
        return SetBoundary(new StBox(x, y, width, height));
    }
}

/// <summary>
/// 内容边界框
/// 
/// 定义页面实际内容的边界范围，用于确定内容的可见区域。
/// 内容边界框通常用于裁剪和显示控制。
/// 
/// 对应OFD标准中的ContentBox定义
/// </summary>
public class ContentBox : OfdElement
{
    /// <summary>
    /// 从现有元素构造内容边界框
    /// </summary>
    /// <param name="element">XML元素</param>
    public ContentBox(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的内容边界框元素
    /// </summary>
    public ContentBox() : base("ContentBox")
    {
    }

    /// <summary>
    /// 使用边界框坐标构造内容边界框
    /// </summary>
    /// <param name="x">左上角X坐标</param>
    /// <param name="y">左上角Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public ContentBox(double x, double y, double width, double height) : base("ContentBox")
    {
        SetBoundary(new StBox(x, y, width, height));
    }

    /// <summary>
    /// 使用StBox构造内容边界框
    /// </summary>
    /// <param name="boundary">边界框</param>
    public ContentBox(StBox boundary) : base("ContentBox")
    {
        SetBoundary(boundary);
    }

    /// <summary>
    /// 设置边界框
    /// </summary>
    /// <param name="boundary">边界框坐标</param>
    /// <returns>this</returns>
    public ContentBox SetBoundary(StBox boundary)
    {
        Element.Value = boundary.ToString();
        return this;
    }

    /// <summary>
    /// 获取边界框
    /// </summary>
    /// <returns>边界框坐标</returns>
    public StBox GetBoundary()
    {
        var value = Element.Value;
        return string.IsNullOrEmpty(value) 
            ? new StBox(0, 0, 210, 297) // 默认A4尺寸
            : StBox.Parse(value);
    }

    /// <summary>
    /// 设置边界框坐标
    /// </summary>
    /// <param name="x">左上角X坐标</param>
    /// <param name="y">左上角Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>this</returns>
    public ContentBox SetBoundary(double x, double y, double width, double height)
    {
        return SetBoundary(new StBox(x, y, width, height));
    }
}

/// <summary>
/// 出血边界框
/// 
/// 定义页面出血区域的边界范围，用于印刷时的出血处理。
/// 出血边界框通常大于物理边界框，用于确保印刷时没有白边。
/// 
/// 对应OFD标准中的BleedBox定义
/// </summary>
public class BleedBox : OfdElement
{
    /// <summary>
    /// 从现有元素构造出血边界框
    /// </summary>
    /// <param name="element">XML元素</param>
    public BleedBox(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的出血边界框元素
    /// </summary>
    public BleedBox() : base("BleedBox")
    {
    }

    /// <summary>
    /// 使用边界框坐标构造出血边界框
    /// </summary>
    /// <param name="x">左上角X坐标</param>
    /// <param name="y">左上角Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public BleedBox(double x, double y, double width, double height) : base("BleedBox")
    {
        SetBoundary(new StBox(x, y, width, height));
    }

    /// <summary>
    /// 使用StBox构造出血边界框
    /// </summary>
    /// <param name="boundary">边界框</param>
    public BleedBox(StBox boundary) : base("BleedBox")
    {
        SetBoundary(boundary);
    }

    /// <summary>
    /// 设置边界框
    /// </summary>
    /// <param name="boundary">边界框坐标</param>
    /// <returns>this</returns>
    public BleedBox SetBoundary(StBox boundary)
    {
        Element.Value = boundary.ToString();
        return this;
    }

    /// <summary>
    /// 获取边界框
    /// </summary>
    /// <returns>边界框坐标</returns>
    public StBox GetBoundary()
    {
        var value = Element.Value;
        return string.IsNullOrEmpty(value) 
            ? new StBox(0, 0, 210, 297) // 默认A4尺寸
            : StBox.Parse(value);
    }

    /// <summary>
    /// 设置边界框坐标
    /// </summary>
    /// <param name="x">左上角X坐标</param>
    /// <param name="y">左上角Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>this</returns>
    public BleedBox SetBoundary(double x, double y, double width, double height)
    {
        return SetBoundary(new StBox(x, y, width, height));
    }
}
