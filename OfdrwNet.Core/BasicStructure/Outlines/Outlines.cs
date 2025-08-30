using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Outlines
{
    /// <summary>
    /// 文档大纲（最小实现，满足编译依赖）
    /// </summary>
    public class Outlines : OfdElement
    {
        public Outlines(XElement element) : base(element)
        {
        }

        public Outlines() : base("Outlines")
        {
        }

        public override string GetQualifiedName()
        {
            return "ofd:Outlines";
        }
    }
}