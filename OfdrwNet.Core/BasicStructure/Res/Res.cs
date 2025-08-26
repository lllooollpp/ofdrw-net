using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.Res;

/// <summary>
/// 资源
/// 
/// 资源是绘制图元时所需数据（如绘制参数、颜色空间、字形、图像、音视频等）的集合。
/// 在页面中出现的资源数据内容都保存在容器的特定文件夹内，但其索引信息保存在资源文件中。
/// 一个文档可能包含一个或多个资源文件。资源根据作用范围分为公共资源和页资源，公共资源文件
/// 在文档根节点中进行指定，页资源文件在页对象中进行指定。
/// 
/// 7.9 资源 图 20
/// 
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.res.Res
/// </summary>
public class Res : OfdElement
{
    /// <summary>
    /// 从现有元素构造资源
    /// </summary>
    /// <param name="element">XML元素</param>
    public Res(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的资源元素
    /// </summary>
    public Res() : base("Res")
    {
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置此资源文件的通用数据存储路径。
    /// BaseLoc属性的意义在于明确资源文件存储位置，
    /// 比如 R1.xml 中可以指定 BaseLoc为"./Res"，
    /// 表明该资源文件中所有数据文件的默认存储位置在
    /// 当前路径的 Res 目录下。
    /// </summary>
    /// <param name="baseLoc">此资源文件的通用数据存储路径</param>
    /// <returns>this</returns>
    public Res SetBaseLoc(StLoc baseLoc)
    {
        AddAttribute("BaseLoc", baseLoc.ToString());
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取此资源文件的通用数据存储路径。
    /// BaseLoc属性的意义在于明确资源文件存储位置，
    /// 比如 R1.xml 中可以指定 BaseLoc为"./Res"，
    /// 表明该资源文件中所有数据文件的默认存储位置在
    /// 当前路径的 Res 目录下。
    /// </summary>
    /// <returns>此资源文件的通用数据存储路径</returns>
    public StLoc? GetBaseLoc()
    {
        var value = GetAttributeValue("BaseLoc");
        return string.IsNullOrEmpty(value) ? null : StLoc.Parse(value);
    }

    /// <summary>
    /// 【可选】
    /// 添加资源
    /// 一个资源文件可描述0到多个资源
    /// </summary>
    /// <param name="resource">资源</param>
    /// <returns>this</returns>
    public Res AddResource(OfdResource resource)
    {
        Add(resource);
        return this;
    }

    /// <summary>
    /// 获取字体资源文件
    /// </summary>
    /// <returns>字体资源列表</returns>
    public List<Fonts> GetFonts()
    {
        var fontsList = new List<Fonts>();
        foreach (var item in GetResources())
        {
            if (item is Fonts fonts)
            {
                fontsList.Add(fonts);
            }
        }
        return fontsList;
    }

    /// <summary>
    /// 【可选】
    /// 获取资源列表
    /// 一个资源文件可描述0到多个资源
    /// tip：可以使用 is 关键字判断是哪一种资源
    /// </summary>
    /// <returns>资源列表</returns>
    public List<OfdResource> GetResources()
    {
        var elements = Element.Elements().ToList();
        var resources = new List<OfdResource>(elements.Count);
        foreach (var element in elements)
        {
            var resource = OfdResource.GetInstance(element);
            resources.Add(resource);
        }
        return resources;
    }
}
