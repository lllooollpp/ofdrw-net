using System.Xml.Linq;
using OfdrwNet.Core.BasicStructure.PageObj.Layer;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.PageObj
{
    /// <summary>
    /// 页面使用的模板页
    /// 
    /// 模板页的内容和结构与普通页相同，定义在 CommonData
    /// 指定的 XML 文件中。一个页可以使用多个模板页。该节点
    /// 使用是通过 TemplateID 来引用具体模板，并通过 ZOrder
    /// 属性来控制模板在页面中的显示顺序。
    /// 
    /// 注：在模板页的内容描述中该属性无效。
    /// 
    /// 作者：权观宇
    /// 时间：2019-10-09 09:44:37
    /// </summary>
    public class Template : OfdElement
    {
        public Template(XElement proxy) : base(proxy)
        {
        }

        public Template() : base("Template")
        {
        }

        /// <summary>
        /// 【必选 属性】
        /// 设置引用在文档共用数据（CommonData）中定义的模板标识符
        /// </summary>
        /// <param name="templateId">引用在文档共用数据（CommonData）中定义的模板标识符</param>
        /// <returns>this</returns>
        public Template SetTemplateId(StRefId templateId)
        {
            AddAttribute("TemplateID", templateId.ToString());
            return this;
        }

        /// <summary>
        /// 【必选 属性】
        /// 获取引用在文档共用数据（CommonData）中定义的模板标识符
        /// </summary>
        /// <returns>引用在文档共用数据（CommonData）中定义的模板标识符</returns>
        public StRefId? GetTemplateId()
        {
            var attrValue = GetAttributeValue("TemplateID"); // 修复：使用GetAttributeValue代替AttributeValue
            return string.IsNullOrEmpty(attrValue) ? null : StRefId.Parse(attrValue); // 修复：使用StRefId.Parse代替StRefId.GetInstance
        }

        /// <summary>
        /// 【可选 属性】
        /// 设置模板在页面中的呈现顺序
        /// 控制模板在页面中的呈现顺序，其类型描述和呈现顺序与Layer中Type的描述和处理一致。
        /// 如果多个图层的此属性相同，则应根据其出现的顺序来显示，先出现者先绘制
        /// 默认值为 Background
        /// </summary>
        /// <param name="zOrder">模板在页面中的呈现顺序</param>
        /// <returns>this</returns>
        public Template SetZOrder(LayerType? zOrder)
        {
            if (zOrder == null)
            {
                RemoveAttribute("ZOrder"); // 修复：使用RemoveAttribute代替RemoveAttr
                return this;
            }
            AddAttribute("ZOrder", zOrder.ToString());
            return this;
        }

        /// <summary>
        /// 【可选 属性】
        /// 获取模板在页面中的呈现顺序
        /// 控制模板在页面中的呈现顺序，其类型描述和呈现顺序与Layer中Type的描述和处理一致。
        /// 如果多个图层的此属性相同，则应根据其出现的顺序来显示，先出现者先绘制
        /// 默认值为 Background
        /// </summary>
        /// <returns>模板在页面中的呈现顺序</returns>
        public LayerType GetZOrder()
        {
            var typeStr = GetAttributeValue("ZOrder"); // 修复：使用GetAttributeValue代替AttributeValue
            if (string.IsNullOrWhiteSpace(typeStr))
            {
                return LayerType.Background;
            }
            // 修复：直接使用Enum.TryParse代替LayerTypeExtensions.GetInstance
            return Enum.TryParse<LayerType>(typeStr, true, out var type) ? type : LayerType.Background;
        }
    }
}