using System.Xml.Linq;
using System.Collections.Generic;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.Res;

/// <summary>
/// 资源索引文件 Res.xml 的表示（对应 Java 的 Res）
/// </summary>
public class Res : OfdrwNet.Core.OfdElement
{
    public Res(XElement element) : base(element)
    {
    }

    public Res() : base("Res")
    {
    }

    public Res SetBaseLoc(StLoc baseLoc)
    {
        SetAttribute("BaseLoc", baseLoc.ToString());
        return this;
    }

    public StLoc? GetBaseLoc()
    {
        var value = GetAttributeValue("BaseLoc");
        return string.IsNullOrEmpty(value) ? null : new StLoc(value);
    }

    public Res AddResource(OfdResource resource)
    {
        Add(resource);
        return this;
    }

    public List<OfdResource> GetResources()
    {
        var elems = GetOfdElements("Resource");
        var list = new List<OfdResource>();
        foreach (var e in elems)
        {
            list.Add(OfdResource.GetInstance(e));
        }
        return list;
    }

    public List<Fonts> GetFonts()
    {
        var list = new List<Fonts>();
        foreach (var r in GetResources())
        {
            if (r is Fonts f)
                list.Add(f);
        }
        return list;
    }
}
