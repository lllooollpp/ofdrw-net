using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Doc.Permission
{
    /// <summary>
    /// 打印权限
    /// 
    /// 具体的权限和份数设置由其属性 Printable 及 Copies 控制。若不设置 Print节点，
    /// 则默认可以打印，并且打印份数不受限制
    /// 
    /// 7.5 图 9 文档权限声明结构
    /// 
    /// 作者：权观宇
    /// 时间：2019-10-07 05:07:02
    /// </summary>
    public class Print : OfdElement
    {
        public Print(XElement proxy) : base(proxy)
        {
        }

        public Print() : base("Print")
        {
        }

        public Print(bool printable, int copies) : this()
        {
            SetPrintable(printable)
                .SetCopies(copies);
        }

        /// <summary>
        /// 【可选 属性】
        /// 设置是否允许被打印
        /// 默认值为 true
        /// </summary>
        /// <param name="printable">true - 允许被打印；false - 不允许被打印</param>
        /// <returns>this</returns>
        public Print SetPrintable(bool printable)
        {
            AddAttribute("Printable", printable.ToString().ToLower());
            return this;
        }

        /// <summary>
        /// 【可选 属性】
        /// 获取是否允许被打印
        /// 默认值为 true
        /// </summary>
        /// <returns>true - 允许被打印；false - 不允许被打印</returns>
        public bool GetPrintable()
        {
            var str = GetAttributeValue("Printable"); // 修复：使用GetAttributeValue代替AttributeValue
            if (string.IsNullOrWhiteSpace(str))
            {
                return true;
            }
            return bool.Parse(str);
        }

        /// <summary>
        /// 【可选 属性】
        /// 设置打印份数
        /// 在 Printable 为 true 时有效，若 Printable 为 true
        /// 并且不设置 Copies 则打印份数不受限，若 Copies 的值为负值时，
        /// 打印份数不受限，当 Copies 的值为 0 时，不允许打印，当 Copies的值
        /// 大于 0 时，则代表实际可打印的份数值。
        /// 默认值为 -1
        /// </summary>
        /// <param name="copies">可打印的份数</param>
        /// <returns>this</returns>
        public Print SetCopies(int copies)
        {
            AddAttribute("Copies", copies.ToString());
            return this;
        }

        /// <summary>
        /// 【可选 属性】
        /// 获取打印份数
        /// 在 Printable 为 true 时有效，若 Printable 为 true
        /// 并且不设置 Copies 则打印份数不受限，若 Copies 的值为负值时，
        /// 打印份数不受限，当 Copies 的值为 0 时，不允许打印，当 Copies的值
        /// 大于 0 时，则代表实际可打印的份数值。
        /// 默认值为 -1
        /// </summary>
        /// <returns>可打印的份数</returns>
        public int GetCopies()
        {
            var str = GetAttributeValue("Copies"); // 修复：使用GetAttributeValue代替AttributeValue
            if (string.IsNullOrWhiteSpace(str))
            {
                return -1;
            }
            return int.Parse(str);
        }
    }
}