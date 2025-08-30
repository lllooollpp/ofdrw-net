using System.Xml.Linq;

namespace OfdrwNet.Core.Action
{
    /// <summary>
    /// 动作基类
    /// 对应Java版本的org.ofdrw.core.action.Action
    /// </summary>
    public class Action : OfdElement
    {
        /// <summary>
        /// 从现有元素构造动作
        /// </summary>
        /// <param name="element">XML元素</param>
        public Action(XElement element) : base(element)
        {
        }

        /// <summary>
        /// 构造新的动作元素
        /// </summary>
        public Action() : base("Action")
        {
        }
    }
}