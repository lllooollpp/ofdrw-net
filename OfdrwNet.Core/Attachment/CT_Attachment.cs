using System.Xml.Linq;

namespace OfdrwNet.Core.Attachment
{
    public class CT_Attachment : OfdrwNet.Core.OfdElement
    {
        public CT_Attachment() : base(new XElement("Attachment")) { }

        public CT_Attachment(XElement el) : base(el) { }

        // Use OfdElement.GetAttributeValue instead of non-existent GetAttribute
        public string? GetName() => GetAttributeValue("Name");

        public CT_Attachment SetName(string name)
        {
            AddAttribute("Name", name);
            return this;
        }

        // Use OfdElement.GetAttributeValue instead of non-existent GetAttribute
        public string? GetFileLoc() => GetAttributeValue("FileLocation");

        public CT_Attachment SetFileLoc(string loc)
        {
            AddAttribute("FileLocation", loc);
            return this;
        }
    }
}
