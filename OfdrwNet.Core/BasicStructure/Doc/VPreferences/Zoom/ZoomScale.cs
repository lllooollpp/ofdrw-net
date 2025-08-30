using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Doc.VPreferences.Zoom
{
    /// <summary>
    /// 缩放比例选择对象基类
    /// </summary>
    public abstract class ZoomScale : OfdElement
    {
        protected ZoomScale(XElement proxy) : base(proxy)
        {
        }

        protected ZoomScale(string name) : base(name)
        {
        }
    }
}
