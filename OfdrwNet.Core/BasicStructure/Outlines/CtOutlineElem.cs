using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Outlines
{
    /// <summary>
    /// CtOutlineElem 最小实现（占位）
    /// </summary>
    public class CtOutlineElem : OfdElement
    {
        public CtOutlineElem(XElement element) : base(element)
        {
        }

        public CtOutlineElem() : base("CtOutlineElem")
        {
        }
    }
}