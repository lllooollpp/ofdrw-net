using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Doc.Permission
{
    /// <summary>
    /// 本标准支持设置文档权限声明（Permission）节点，以达到文档防扩散等应用目的。
    /// 文档权限声明结构如 图 9 所示。
    /// 
    /// 7.5 小节 CT_Permission
    /// 
    /// 作者：权观宇
    /// 时间：2019-10-06 08:09:21
    /// </summary>
    public class CtPermission : OfdElement
    {
        public CtPermission(XElement proxy) : base(proxy)
        {
        }

        public CtPermission() : base("Permissions")
        {
        }

        /// <summary>
        /// 【可选】
        /// 设置是否允许编辑
        /// 默认值为 true
        /// </summary>
        /// <param name="edit">true - 允许编辑；false - 不允许编辑</param>
        /// <returns>this</returns>
        public CtPermission SetEdit(bool edit)
        {
            SetOfdEntity("Edit", edit.ToString().ToLower());
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取是否允许编辑
        /// 默认值为 true
        /// </summary>
        /// <returns>true - 允许编辑；false - 不允许编辑</returns>
        public bool GetEdit()
        {
            var str = GetOfdElementText("Edit");
            if (string.IsNullOrWhiteSpace(str))
            {
                return true;
            }
            return bool.Parse(str);
        }

        /// <summary>
        /// 【可选】
        /// 设置是否允许添加或修改标注
        /// 默认值为 true
        /// </summary>
        /// <param name="annot">true - 允许添加或修改标注；false - 不允许添加或修改标注</param>
        /// <returns>this</returns>
        public CtPermission SetAnnot(bool annot)
        {
            SetOfdEntity("Annot", annot.ToString().ToLower());
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取是否允许添加或修改标注
        /// 默认值为 true
        /// </summary>
        /// <returns>true - 允许添加或修改标注；false - 不允许添加或修改标注</returns>
        public bool GetAnnot()
        {
            var str = GetOfdElementText("Annot");
            if (string.IsNullOrWhiteSpace(str))
            {
                return true;
            }
            return bool.Parse(str);
        }

        /// <summary>
        /// 【可选】
        /// 设置是否允许导出
        /// 默认值为 true
        /// </summary>
        /// <param name="export">true - 允许导出；false - 不允许导出</param>
        /// <returns>this</returns>
        public CtPermission SetExport(bool export)
        {
            SetOfdEntity("Export", export.ToString().ToLower());
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取是否允许导出
        /// 默认值为 true
        /// </summary>
        /// <returns>true - 允许导出；false - 不允许导出</returns>
        public bool GetExport()
        {
            var str = GetOfdElementText("Export");
            if (string.IsNullOrWhiteSpace(str))
            {
                return true;
            }
            return bool.Parse(str);
        }

        /// <summary>
        /// 【可选】
        /// 设置是否允许进行数字签名
        /// 默认值为 true
        /// </summary>
        /// <param name="signature">true - 允许进行数字签名；false - 不允许进行数字签名</param>
        /// <returns>this</returns>
        public CtPermission SetSignature(bool signature)
        {
            SetOfdEntity("Signature", signature.ToString().ToLower());
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取是否允许进行数字签名
        /// 默认值为 true
        /// </summary>
        /// <returns>true - 允许进行数字签名；false - 不允许进行数字签名</returns>
        public bool GetSignature()
        {
            var str = GetOfdElementText("Signature");
            if (string.IsNullOrWhiteSpace(str))
            {
                return true;
            }
            return bool.Parse(str);
        }

        /// <summary>
        /// 【可选】
        /// 设置是否允许添加水印
        /// 默认值为 true
        /// </summary>
        /// <param name="watermark">true - 允许添加水印；false - 不允许添加水印</param>
        /// <returns>this</returns>
        public CtPermission SetWatermark(bool watermark)
        {
            SetOfdEntity("Watermark", watermark.ToString().ToLower());
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取是否允许添加水印
        /// 默认值为 true
        /// </summary>
        /// <returns>true - 允许添加水印；false - 不允许添加水印</returns>
        public bool GetWatermark()
        {
            var str = GetOfdElementText("Watermark");
            if (string.IsNullOrWhiteSpace(str))
            {
                return true;
            }
            return bool.Parse(str);
        }

        /// <summary>
        /// 【可选】
        /// 设置是否允许截屏
        /// 默认值为 true
        /// </summary>
        /// <param name="printScreen">true - 允许截屏；false - 不允许截屏</param>
        /// <returns>this</returns>
        public CtPermission SetPrintScreen(bool printScreen)
        {
            SetOfdEntity("PrintScreen", printScreen.ToString().ToLower());
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取是否允许截屏
        /// 默认值为 true
        /// </summary>
        /// <returns>true - 允许截屏；false - 不允许截屏</returns>
        public bool GetPrintScreen()
        {
            var str = GetOfdElementText("PrintScreen");
            if (string.IsNullOrWhiteSpace(str))
            {
                return true;
            }
            return bool.Parse(str);
        }

        /// <summary>
        /// 【可选】
        /// 设置打印权限
        /// 具体的权限和份数设置由其属性 Printable 及 Copies 控制。若不设置 Print节点，
        /// 则默认可以打印，并且打印份数不受限制
        /// </summary>
        /// <param name="print">打印权限</param>
        /// <returns>this</returns>
        public CtPermission SetPrint(Print print)
        {
            Set(print);
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取打印权限
        /// 具体的权限和份数设置由其属性 Printable 及 Copies 控制。若不设置 Print节点，
        /// 则默认可以打印，并且打印份数不受限制
        /// </summary>
        /// <returns>打印权限</returns>
        public Print? GetPrint()
        {
            var e = GetOfdElement("Print");
            return e == null ? null : new Print(e);
        }

        /// <summary>
        /// 【可选】
        /// 设置有效期
        /// 该文档允许访问的期限，其具体期限取决于开始日期和
        /// 结束日期，其中开始日期不能晚于结束日期，并且开始日期和结束
        /// 日期至少出现一个。当不设置开始日期时，代表不限定开始日期，
        /// 当不设置结束日期时代表不限定结束日期；当此不设置此节点时，
        /// 表示开始和结束日期均不受限
        /// </summary>
        /// <param name="validPeriod">有效期</param>
        /// <returns>this</returns>
        public CtPermission SetValidPeriod(ValidPeriod validPeriod)
        {
            Set(validPeriod);
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取有效期
        /// 该文档允许访问的期限，其具体期限取决于开始日期和
        /// 结束日期，其中开始日期不能晚于结束日期，并且开始日期和结束
        /// 日期至少出现一个。当不设置开始日期时，代表不限定开始日期，
        /// 当不设置结束日期时代表不限定结束日期；当此不设置此节点时，
        /// 表示开始和结束日期均不受限
        /// </summary>
        /// <returns>有效期</returns>
        public ValidPeriod? GetValidPeriod()
        {
            var e = GetOfdElement("ValidPeriod");
            return e == null ? null : new ValidPeriod(e);
        }
    }
}
