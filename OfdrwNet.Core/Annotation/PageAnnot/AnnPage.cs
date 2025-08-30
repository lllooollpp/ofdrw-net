using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Annotation.PageAnnot;

public class AnnPage : OfdrwNet.Core.OfdElement
{
    public AnnPage(XElement element) : base(element) { }
    public AnnPage() : base("Page") { }

    public AnnPage SetPageID(StRefId pageId)
    {
        if (pageId == null) throw new ArgumentNullException(nameof(pageId));
        AddAttribute("PageID", pageId.ToString());
        return this;
    }

    public AnnPage SetPageID(StId pageId)
    {
        return SetPageID(new StRefId(pageId.Value));
    }

    public StRefId? GetPageID()
    {
        var v = GetAttributeValue("PageID");
        return string.IsNullOrEmpty(v) ? null : StRefId.Parse(v);
    }

    public AnnPage SetFileLoc(StLoc loc)
    {
        if (loc == null) throw new ArgumentNullException(nameof(loc));
        SetOfdEntity("FileLoc", loc.ToString());
        return this;
    }

    public StLoc? GetFileLoc()
    {
        var e = GetOfdElement("FileLoc");
        return e == null ? null : StLoc.Parse(e.Value);
    }
}
