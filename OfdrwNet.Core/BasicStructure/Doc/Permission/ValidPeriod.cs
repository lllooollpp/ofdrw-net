using System;
using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Doc.Permission
{
    /// <summary>
    /// 有效期
    /// 
    /// 该文档允许访问的期限，其具体期限取决于开始日期和
    /// 结束日期，其中开始日期不能晚于结束日期，并且开始日期和结束
    /// 日期至少出现一个。当不设置开始日期时，代表不限定开始日期，
    /// 当不设置结束日期时代表不限定结束日期；当此不设置此节点时，
    /// 表示开始和结束日期均不受限
    /// 
    /// 7.5 图 9 文档权限声明结构
    /// 
    /// 作者：权观宇
    /// 时间：2019-10-07 05:21:06
    /// </summary>
    public class ValidPeriod : OfdElement
    {
        public ValidPeriod(XElement proxy) : base(proxy)
        {
        }

        public ValidPeriod() : base("ValidPeriod")
        {
        }

        public ValidPeriod(DateTime? startDate, DateTime? endDate) : this()
        {
            SetStartDate(startDate)
                .SetEndDate(endDate);
        }

        /// <summary>
        /// 【可选 属性】
        /// 设置有效期开始日期
        /// </summary>
        /// <param name="startDate">有效期开始日期</param>
        /// <returns>this</returns>
        public ValidPeriod SetStartDate(DateTime? startDate)
        {
            if (startDate == null)
            {
                RemoveAttribute("StartDate"); // 修复：使用RemoveAttribute代替RemoveAttr
                return this;
            }
            AddAttribute("StartDate", startDate.Value.ToString(Const.DateTimeFormat));
            return this;
        }

        /// <summary>
        /// 【可选 属性】
        /// 获取有效期开始日期
        /// </summary>
        /// <returns>有效期开始日期</returns>
        public DateTime? GetStartDate()
        {
            var str = GetAttributeValue("StartDate"); // 修复：使用GetAttributeValue代替AttributeValue
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }
            return DateTime.ParseExact(str, Const.DateTimeFormat, null);
        }

        /// <summary>
        /// 【可选 属性】
        /// 设置有效期结束日期
        /// </summary>
        /// <param name="endDate">有效期结束日期</param>
        /// <returns>this</returns>
        public ValidPeriod SetEndDate(DateTime? endDate)
        {
            if (endDate == null)
            {
                RemoveAttribute("EndDate"); // 修复：使用RemoveAttribute代替RemoveAttr
                return this;
            }
            AddAttribute("EndDate", endDate.Value.ToString(Const.DateTimeFormat));
            return this;
        }

        /// <summary>
        /// 【可选 属性】
        /// 获取有效期结束日期
        /// </summary>
        /// <returns>有效期结束日期</returns>
        public DateTime? GetEndDate()
        {
            var str = GetAttributeValue("EndDate"); // 修复：使用GetAttributeValue代替AttributeValue
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }
            return DateTime.ParseExact(str, Const.DateTimeFormat, null);
        }
    }
}