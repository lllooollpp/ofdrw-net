using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.PageDescription.DrawParam;

namespace OfdrwNet.Core.Graph
{
    /// <summary>
    /// 路径对象 (简化版本)
    /// 
    /// 路径对象用于在页面上绘制路径图形，包括直线、曲线、多边形等。
    /// 
    /// 对应Java版本：org.ofdrw.core.graph.pathObj.PathObject
    /// </summary>
    public class PathObject : OfdElement
    {
        /// <summary>
        /// 创建一个新的路径对象实例
        /// </summary>
        public PathObject() : base("PathObject")
        {
        }

        /// <summary>
        /// 设置对象ID
        /// </summary>
        /// <param name="id">对象ID</param>
        /// <returns>this</returns>
        public PathObject SetID(StId id)
        {
            SetAttribute("ID", id.ToString());
            return this;
        }

        /// <summary>
        /// 获取对象ID
        /// </summary>
        /// <returns>对象ID</returns>
        public StId? GetID()
        {
            var value = GetAttributeValue("ID");
            return string.IsNullOrEmpty(value) ? null : StId.Parse(value);
        }

        /// <summary>
        /// 设置边界框
        /// </summary>
        /// <param name="boundary">边界框</param>
        /// <returns>this</returns>
        public PathObject SetBoundary(StBox? boundary)
        {
            if (boundary is not null)
            {
                SetAttribute("Boundary", boundary.ToString());
            }
            else
            {
                SetAttribute("Boundary", "");
            }
            return this;
        }

        /// <summary>
        /// 获取边界框
        /// </summary>
        /// <returns>边界框</returns>
        public StBox? GetBoundary()
        {
            var value = GetAttributeValue("Boundary");
            return string.IsNullOrEmpty(value) ? null : StBox.Parse(value);
        }

        /// <summary>
        /// 设置路径数据
        /// </summary>
        /// <param name="pathData">路径数据</param>
        /// <returns>this</returns>
        public PathObject SetAbbreviatedData(string pathData)
        {
            if (string.IsNullOrEmpty(pathData))
            {
                RemoveOfdElementsByNames("AbbreviatedData");
                return this;
            }
            
            SetOfdEntity("AbbreviatedData", pathData);
            return this;
        }

        /// <summary>
        /// 获取路径数据
        /// </summary>
        /// <returns>路径数据</returns>
        public string? GetAbbreviatedData()
        {
            return GetOfdElementText("AbbreviatedData");
        }

        /// <summary>
        /// 设置是否描边
        /// </summary>
        /// <param name="stroke">是否描边</param>
        /// <returns>this</returns>
        public PathObject SetStroke(bool stroke)
        {
            SetAttribute("Stroke", stroke.ToString().ToLowerInvariant());
            return this;
        }

        /// <summary>
        /// 获取是否描边
        /// </summary>
        /// <returns>是否描边</returns>
        public bool GetStroke()
        {
            var value = GetAttributeValue("Stroke");
            return !string.IsNullOrEmpty(value) && bool.Parse(value);
        }

        /// <summary>
        /// 设置是否填充
        /// </summary>
        /// <param name="fill">是否填充</param>
        /// <returns>this</returns>
        public PathObject SetFill(bool fill)
        {
            SetAttribute("Fill", fill.ToString().ToLowerInvariant());
            return this;
        }

        /// <summary>
        /// 获取是否填充
        /// </summary>
        /// <returns>是否填充</returns>
        public bool GetFill()
        {
            var value = GetAttributeValue("Fill");
            return !string.IsNullOrEmpty(value) && bool.Parse(value);
        }
    }
}
