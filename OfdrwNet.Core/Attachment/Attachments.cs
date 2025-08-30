using System.Xml.Linq;
using System.Collections.Generic;

namespace OfdrwNet.Core.Attachment
{
    public class Attachments : OfdrwNet.Core.OfdElement
    {
        public Attachments() : base(new XElement("Attachments")) { }
        public Attachments(XElement el) : base(el) { }

        public Attachments AddAttachment(CT_Attachment att)
        {
            // Use underlying XElement via Element property
            this.Element.Add(att.ToXElement());
            return this;
        }

        public List<CT_Attachment> GetAttachments()
        {
            var list = new List<CT_Attachment>();
            foreach (var el in Element.Elements())
            {
                list.Add(new CT_Attachment(el));
            }
            return list;
        }
    }
}
