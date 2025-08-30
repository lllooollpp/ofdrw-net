using System.Xml.Linq;
using OfdrwNet.Core;

namespace OfdrwNet.Core.BasicStructure.PageObj
{
    /// <summary>
    /// 模板页结构
    /// 
    /// 对应 Java 的 org.ofdrw.core.basicStructure.pageObj.CT_TemplatePage
    /// 临时实现，将在后续完整迁移
    /// </summary>
    /// <author>翻译自 权观宇 的 Java 实现</author>
    public class CtTemplatePage : OfdElement
    {
        /// <summary>
        /// 使用现有元素创建模板页
        /// </summary>
        /// <param name="element">XML元素</param>
        public CtTemplatePage(XElement element) : base(element)
        {
        }

        /// <summary>
        /// 创建新的模板页
        /// </summary>
        public CtTemplatePage() : base("TemplatePage")
        {
        }

        /// <summary>
        /// 验证模板页的有效性
        /// </summary>
        /// <returns>验证结果</returns>
        public override ValidationResult Validate()
        {
            return base.Validate();
        }

        /// <summary>
        /// 获取限定名称
        /// </summary>
        /// <returns>限定名称</returns>
        public override string GetQualifiedName()
        {
            return "ofd:TemplatePage";
        }
    }
}