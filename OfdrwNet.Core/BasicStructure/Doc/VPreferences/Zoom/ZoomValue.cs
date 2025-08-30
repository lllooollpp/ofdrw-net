using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.Doc.VPreferences.Zoom
{
    /// <summary>
    /// 文档的缩放率
    /// 
    /// 7.5 表 9 视图首选项
    /// 
    /// 作者：权观宇
    /// 时间：2019-10-07 09:52:29
    /// </summary>
    public class ZoomValue : ZoomScale
    {
        public ZoomValue(XElement proxy) : base(proxy)
        {
        }

        public ZoomValue(double value) : base("Zoom")
        {
            SetValue(value);
        }

        /// <summary>
        /// 设置文档的缩放率
        /// </summary>
        /// <param name="value">文档的缩放率</param>
        /// <returns>this</returns>
        public ZoomValue SetValue(double value)
        {
            Element.Value = StBase.Fmt(value); // 修复：使用Element.Value代替AddText
            return this;
        }

        /// <summary>
        /// 获取文档的缩放率
        /// </summary>
        /// <returns>文档的缩放率</returns>
        public double GetValue()
        {
            return double.Parse(GetText());
        }
    }
}