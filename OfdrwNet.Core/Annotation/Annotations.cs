using System.Collections.Generic;
using System.Xml.Linq;
using OfdrwNet.Core;
using OfdrwNet.Core.Annotation.PageAnnot;

namespace OfdrwNet.Core.Annotation;

/// <summary>
/// 注释入口文件（Annotations.xml）
/// 对应 Java: org.ofdrw.core.annotation.Annotations
/// </summary>
public class Annotations : OfdElement
{
    public Annotations(XElement element) : base(element)
    {
    }

    public Annotations() : base("Annotations")
    {
    }

    public Annotations AddPage(AnnPage page)
    {
        if (page == null)
            return this;
        Add(page);
        return this;
    }

    public AnnPage? GetByPageId(string id)
    {
        var pages = GetPages();
        foreach (var p in pages)
        {
            var pid = p.GetPageID();
            if (pid != null && pid.ToString() == id)
                return p;
        }
        return null;
    }

    public List<AnnPage> GetPages()
    {
        return GetOfdElements("Page", el => new AnnPage(el));
    }
}
