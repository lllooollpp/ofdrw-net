using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Text.Font;
using OfdrwNet.Core.PageDescription.Color;
using OfdrwNet.Core.PageDescription.DrawParam;

namespace OfdrwNet.Core.BasicStructure.Res;

/// <summary>
/// 资源集合
/// 
/// 管理OFD文档中的各种资源，包括字体、颜色空间、绘制参数、多媒体资源等。
/// 提供统一的资源管理接口。
/// 
/// 对应OFD标准中的Resources定义
/// 7.2 资源 图 12 表 25
/// </summary>
public class Resources : OfdElement
{
    /// <summary>
    /// 从现有元素构造资源集合
    /// </summary>
    /// <param name="element">XML元素</param>
    public Resources(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的资源集合
    /// </summary>
    public Resources() : base("Resources")
    {
    }

    /// <summary>
    /// 【可选】设置基础资源路径
    /// </summary>
    /// <param name="baseLoc">基础资源路径</param>
    /// <returns>this</returns>
    public Resources SetBaseLoc(StLoc baseLoc)
    {
        SetAttribute("BaseLoc", baseLoc.ToString());
        return this;
    }

    /// <summary>
    /// 【可选】获取基础资源路径
    /// </summary>
    /// <returns>基础资源路径</returns>
    public StLoc? GetBaseLoc()
    {
        var value = GetAttributeValue("BaseLoc");
        return string.IsNullOrEmpty(value) ? null : new StLoc(value);
    }

    #region 字体资源管理

    /// <summary>
    /// 设置字体集合
    /// </summary>
    /// <param name="fonts">字体集合</param>
    /// <returns>this</returns>
    public Resources SetFonts(Fonts fonts)
    {
        Set(fonts);
        return this;
    }

    /// <summary>
    /// 获取字体集合
    /// </summary>
    /// <returns>字体集合</returns>
    public Fonts? GetFonts()
    {
        var element = GetOfdElement("Fonts");
        return element == null ? null : new Fonts(element);
    }

    /// <summary>
    /// 添加字体资源
    /// </summary>
    /// <param name="fontInfo">字体信息</param>
    /// <returns>this</returns>
    public Resources AddFont(FontInfo fontInfo)
    {
        var fonts = GetFonts();
        if (fonts == null)
        {
            fonts = new Fonts();
            Add(fonts);
        }
        fonts.AddFont(fontInfo);
        return this;
    }

    /// <summary>
    /// 根据ID查找字体
    /// </summary>
    /// <param name="fontId">字体ID</param>
    /// <returns>字体信息或null</returns>
    public FontInfo? FindFont(StId fontId)
    {
        return GetFonts()?.FindFont(fontId);
    }

    #endregion

    #region 颜色空间资源管理

    /// <summary>
    /// 设置颜色空间集合
    /// </summary>
    /// <param name="colorSpaces">颜色空间集合</param>
    /// <returns>this</returns>
    public Resources SetColorSpaces(ColorSpaces colorSpaces)
    {
        Set(colorSpaces);
        return this;
    }

    /// <summary>
    /// 获取颜色空间集合
    /// </summary>
    /// <returns>颜色空间集合</returns>
    public ColorSpaces? GetColorSpaces()
    {
        var element = GetOfdElement("ColorSpaces");
        return element == null ? null : new ColorSpaces(element);
    }

    /// <summary>
    /// 添加颜色空间
    /// </summary>
    /// <param name="colorSpace">颜色空间</param>
    /// <returns>this</returns>
    public Resources AddColorSpace(ColorSpace colorSpace)
    {
        var colorSpaces = GetColorSpaces();
        if (colorSpaces == null)
        {
            colorSpaces = new ColorSpaces();
            Add(colorSpaces);
        }
        colorSpaces.AddColorSpace(colorSpace);
        return this;
    }

    /// <summary>
    /// 根据名称查找颜色空间
    /// </summary>
    /// <param name="name">颜色空间名称</param>
    /// <returns>颜色空间或null</returns>
    public ColorSpace? FindColorSpace(string name)
    {
        return GetColorSpaces()?.FindColorSpace(name);
    }

    #endregion

    #region 绘制参数资源管理

    /// <summary>
    /// 设置绘制参数集合
    /// </summary>
    /// <param name="drawParams">绘制参数集合</param>
    /// <returns>this</returns>
    public Resources SetDrawParams(DrawParams drawParams)
    {
        Set(drawParams);
        return this;
    }

    /// <summary>
    /// 获取绘制参数集合
    /// </summary>
    /// <returns>绘制参数集合</returns>
    public DrawParams? GetDrawParams()
    {
        var element = GetOfdElement("DrawParams");
        return element == null ? null : new DrawParams(element);
    }

    /// <summary>
    /// 添加绘制参数
    /// </summary>
    /// <param name="drawParam">绘制参数</param>
    /// <returns>this</returns>
    public Resources AddDrawParam(CtDrawParam drawParam)
    {
        var drawParams = GetDrawParams();
        if (drawParams == null)
        {
            drawParams = new DrawParams();
            Add(drawParams);
        }
        drawParams.AddDrawParam(drawParam);
        return this;
    }

    #endregion

    #region 多媒体资源管理

    /// <summary>
    /// 设置多媒体资源集合
    /// </summary>
    /// <param name="multiMedias">多媒体资源集合</param>
    /// <returns>this</returns>
    public Resources SetMultiMedias(MultiMedias multiMedias)
    {
        Set(multiMedias);
        return this;
    }

    /// <summary>
    /// 获取多媒体资源集合
    /// </summary>
    /// <returns>多媒体资源集合</returns>
    public MultiMedias? GetMultiMedias()
    {
        var element = GetOfdElement("MultiMedias");
        return element == null ? null : new MultiMedias(element);
    }

    /// <summary>
    /// 添加多媒体资源
    /// </summary>
    /// <param name="multiMedia">多媒体资源</param>
    /// <returns>this</returns>
    public Resources AddMultiMedia(MultiMedia multiMedia)
    {
        var multiMedias = GetMultiMedias();
        if (multiMedias == null)
        {
            multiMedias = new MultiMedias();
            Add(multiMedias);
        }
        multiMedias.AddMultiMedia(multiMedia);
        return this;
    }

    #endregion

    #region 复合图形单元管理

    /// <summary>
    /// 设置复合图形单元集合
    /// </summary>
    /// <param name="compositeGraphicUnits">复合图形单元集合</param>
    /// <returns>this</returns>
    public Resources SetCompositeGraphicUnits(CompositeGraphicUnits compositeGraphicUnits)
    {
        Set(compositeGraphicUnits);
        return this;
    }

    /// <summary>
    /// 获取复合图形单元集合
    /// </summary>
    /// <returns>复合图形单元集合</returns>
    public CompositeGraphicUnits? GetCompositeGraphicUnits()
    {
        var element = GetOfdElement("CompositeGraphicUnits");
        return element == null ? null : new CompositeGraphicUnits(element);
    }

    /// <summary>
    /// 添加复合图形单元
    /// </summary>
    /// <param name="compositeGraphicUnit">复合图形单元</param>
    /// <returns>this</returns>
    public Resources AddCompositeGraphicUnit(CompositeGraphicUnit compositeGraphicUnit)
    {
        var compositeGraphicUnits = GetCompositeGraphicUnits();
        if (compositeGraphicUnits == null)
        {
            compositeGraphicUnits = new CompositeGraphicUnits();
            Add(compositeGraphicUnits);
        }
        compositeGraphicUnits.AddCompositeGraphicUnit(compositeGraphicUnit);
        return this;
    }

    #endregion

    /// <summary>
    /// 获取所有资源的数量统计
    /// </summary>
    /// <returns>资源数量统计</returns>
    public ResourceStats GetResourceStats()
    {
        return new ResourceStats
        {
            FontCount = GetFonts()?.GetFontCount() ?? 0,
            ColorSpaceCount = GetColorSpaces()?.GetColorSpaceCount() ?? 0,
            DrawParamCount = GetDrawParams()?.GetDrawParamCount() ?? 0,
            MultiMediaCount = GetMultiMedias()?.GetMultiMediaCount() ?? 0,
            CompositeGraphicUnitCount = GetCompositeGraphicUnits()?.GetCompositeGraphicUnitCount() ?? 0
        };
    }

    /// <summary>
    /// 检查资源是否为空
    /// </summary>
    /// <returns>是否为空</returns>
    public bool IsEmpty()
    {
        var stats = GetResourceStats();
        return stats.FontCount == 0 && 
               stats.ColorSpaceCount == 0 && 
               stats.DrawParamCount == 0 && 
               stats.MultiMediaCount == 0 && 
               stats.CompositeGraphicUnitCount == 0;
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:Resources";
}

/// <summary>
/// 字体资源集合
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.res.resources.Fonts
/// </summary>
public class Fonts : OfdResource
{
    /// <summary>
    /// 从现有元素构造字体资源
    /// </summary>
    /// <param name="element">XML元素</param>
    public Fonts(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的字体资源元素
    /// </summary>
    public Fonts() : base("Fonts")
    {
    }

    /// <summary>
    /// 添加字体
    /// </summary>
    /// <param name="fontInfo">字体信息</param>
    /// <returns>this</returns>
    public Fonts AddFont(FontInfo fontInfo)
    {
        Add(fontInfo);
        return this;
    }

    /// <summary>
    /// 获取所有字体
    /// </summary>
    /// <returns>字体列表</returns>
    public List<FontInfo> GetFonts()
    {
        return Element.Elements(Const.OfdNamespace + "Font")
            .Select(e => new FontInfo(e))
            .ToList();
    }

    /// <summary>
    /// 根据ID查找字体
    /// </summary>
    /// <param name="id">字体ID</param>
    /// <returns>字体信息或null</returns>
    public FontInfo? FindFont(StId id)
    {
        return GetFonts().FirstOrDefault(f => f.GetID()?.Equals(id) == true);
    }

    /// <summary>
    /// 获取字体数量
    /// </summary>
    /// <returns>字体数量</returns>
    public int GetFontCount()
    {
        return Element.Elements(Const.OfdNamespace + "Font").Count();
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:Fonts";
}

/// <summary>
/// 颜色空间资源集合
/// </summary>
public class ColorSpaces : OfdResource
{
    /// <summary>
    /// 从现有元素构造颜色空间资源集合
    /// </summary>
    /// <param name="element">XML元素</param>
    public ColorSpaces(XElement element) : base(element) { }
    
    /// <summary>
    /// 构造新的颜色空间资源集合
    /// </summary>
    public ColorSpaces() : base("ColorSpaces") { }

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
    /// 获取所有颜色空间
    /// </summary>
    /// <returns>颜色空间列表</returns>
    public List<ColorSpace> GetColorSpaces()
    {
        var colorSpaces = new List<ColorSpace>();
        
        // 处理RGB颜色空间
        foreach (var element in Element.Elements(Const.OfdNamespace + "RGB"))
        {
            colorSpaces.Add(new RgbColorSpace(element));
        }
        
        // 处理CMYK颜色空间
        foreach (var element in Element.Elements(Const.OfdNamespace + "CMYK"))
        {
            colorSpaces.Add(new CmykColorSpace(element));
        }
        
        // 处理Gray颜色空间
        foreach (var element in Element.Elements(Const.OfdNamespace + "Gray"))
        {
            colorSpaces.Add(new GrayColorSpace(element));
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
        return GetColorSpaces().Count;
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:ColorSpaces";
}

/// <summary>
/// 绘制参数资源集合
/// </summary>
public class DrawParams : OfdResource
{
    /// <summary>
    /// 从现有元素构造绘制参数资源集合
    /// </summary>
    /// <param name="element">XML元素</param>
    public DrawParams(XElement element) : base(element) { }
    
    /// <summary>
    /// 构造新的绘制参数资源集合
    /// </summary>
    public DrawParams() : base("DrawParams") { }

    /// <summary>
    /// 添加绘制参数
    /// </summary>
    /// <param name="drawParam">绘制参数</param>
    /// <returns>this</returns>
    public DrawParams AddDrawParam(CtDrawParam drawParam)
    {
        Add(drawParam);
        return this;
    }

    /// <summary>
    /// 获取所有绘制参数
    /// </summary>
    /// <returns>绘制参数列表</returns>
    public List<CtDrawParam> GetDrawParams()
    {
        return Element.Elements(Const.OfdNamespace + "DrawParam")
            .Select(e => new CtDrawParam(e))
            .ToList();
    }

    /// <summary>
    /// 根据ID查找绘制参数
    /// </summary>
    /// <param name="id">绘制参数ID</param>
    /// <returns>绘制参数或null</returns>
    public CtDrawParam? FindDrawParam(StId id)
    {
        return GetDrawParams().FirstOrDefault(dp => dp.GetId()?.Equals(id) == true);
    }

    /// <summary>
    /// 获取绘制参数数量
    /// </summary>
    /// <returns>绘制参数数量</returns>
    public int GetDrawParamCount()
    {
        return Element.Elements(Const.OfdNamespace + "DrawParam").Count();
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:DrawParams";
}

/// <summary>
/// 多媒体资源集合
/// </summary>
public class MultiMedias : OfdResource
{
    /// <summary>
    /// 从现有元素构造多媒体资源集合
    /// </summary>
    /// <param name="element">XML元素</param>
    public MultiMedias(XElement element) : base(element) { }
    
    /// <summary>
    /// 构造新的多媒体资源集合
    /// </summary>
    public MultiMedias() : base("MultiMedias") { }

    /// <summary>
    /// 添加多媒体资源
    /// </summary>
    /// <param name="multiMedia">多媒体资源</param>
    /// <returns>this</returns>
    public MultiMedias AddMultiMedia(MultiMedia multiMedia)
    {
        Add(multiMedia);
        return this;
    }

    /// <summary>
    /// 获取所有多媒体资源
    /// </summary>
    /// <returns>多媒体资源列表</returns>
    public List<MultiMedia> GetMultiMedias()
    {
        return Element.Elements(Const.OfdNamespace + "MultiMedia")
            .Select(e => new MultiMedia(e))
            .ToList();
    }

    /// <summary>
    /// 获取多媒体资源数量
    /// </summary>
    /// <returns>多媒体资源数量</returns>
    public int GetMultiMediaCount()
    {
        return Element.Elements(Const.OfdNamespace + "MultiMedia").Count();
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:MultiMedias";
}

/// <summary>
/// 多媒体资源（占位符实现）
/// </summary>
public class MultiMedia : OfdResource
{
    /// <summary>
    /// 从现有元素构造多媒体资源
    /// </summary>
    /// <param name="element">XML元素</param>
    public MultiMedia(XElement element) : base(element) { }
    
    /// <summary>
    /// 构造新的多媒体资源
    /// </summary>
    public MultiMedia() : base("MultiMedia") { }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:MultiMedia";
}

/// <summary>
/// 复合图形单元集合
/// </summary>
public class CompositeGraphicUnits : OfdResource
{
    /// <summary>
    /// 从现有元素构造复合图形单元集合
    /// </summary>
    /// <param name="element">XML元素</param>
    public CompositeGraphicUnits(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的复合图形单元集合
    /// </summary>
    public CompositeGraphicUnits() : base("CompositeGraphicUnits")
    {
    }

    /// <summary>
    /// 添加复合图形单元
    /// </summary>
    /// <param name="compositeGraphicUnit">复合图形单元</param>
    /// <returns>this</returns>
    public CompositeGraphicUnits AddCompositeGraphicUnit(CompositeGraphicUnit compositeGraphicUnit)
    {
        Add(compositeGraphicUnit);
        return this;
    }

    /// <summary>
    /// 获取所有复合图形单元
    /// </summary>
    /// <returns>复合图形单元列表</returns>
    public List<CompositeGraphicUnit> GetCompositeGraphicUnits()
    {
        return Element.Elements(Const.OfdNamespace + "CompositeGraphicUnit")
            .Select(e => new CompositeGraphicUnit(e))
            .ToList();
    }

    /// <summary>
    /// 获取复合图形单元数量
    /// </summary>
    /// <returns>复合图形单元数量</returns>
    public int GetCompositeGraphicUnitCount()
    {
        return Element.Elements(Const.OfdNamespace + "CompositeGraphicUnit").Count();
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:CompositeGraphicUnits";
}

/// <summary>
/// 复合图形单元（占位符实现）
/// </summary>
public class CompositeGraphicUnit : OfdResource
{
    /// <summary>
    /// 从现有元素构造复合图形单元
    /// </summary>
    /// <param name="element">XML元素</param>
    public CompositeGraphicUnit(XElement element) : base(element) { }
    
    /// <summary>
    /// 构造新的复合图形单元
    /// </summary>
    public CompositeGraphicUnit() : base("CompositeGraphicUnit") { }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:CompositeGraphicUnit";
}

/// <summary>
/// 资源统计信息
/// </summary>
public class ResourceStats
{
    /// <summary>
    /// 字体数量
    /// </summary>
    public int FontCount { get; set; }

    /// <summary>
    /// 颜色空间数量
    /// </summary>
    public int ColorSpaceCount { get; set; }

    /// <summary>
    /// 绘制参数数量
    /// </summary>
    public int DrawParamCount { get; set; }

    /// <summary>
    /// 多媒体资源数量
    /// </summary>
    public int MultiMediaCount { get; set; }

    /// <summary>
    /// 复合图形单元数量
    /// </summary>
    public int CompositeGraphicUnitCount { get; set; }

    /// <summary>
    /// 总资源数量
    /// </summary>
    public int TotalCount => FontCount + ColorSpaceCount + DrawParamCount + MultiMediaCount + CompositeGraphicUnitCount;
}
