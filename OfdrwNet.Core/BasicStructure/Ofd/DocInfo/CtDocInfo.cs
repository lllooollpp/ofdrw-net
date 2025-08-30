using System;
using System.Xml.Linq;
using OfdrwNet.Core;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.Ofd.DocInfo
{
    /// <summary>
    /// 文档分类
    /// 
    /// 对应 Java 的 org.ofdrw.core.basicStructure.ofd.docInfo.DocUsage
    /// </summary>
    /// <author>翻译自 权观宇 的 Java 实现</author>
    /// <since>2019-10-01 05:22:41</since>
    public enum DocUsage
    {
        /// <summary>
        /// 普通文档
        /// </summary>
        Normal,

        /// <summary>
        /// 电子书
        /// </summary>
        EBook,

        /// <summary>
        /// 电子报纸
        /// </summary>
        ENewsPaper,

        /// <summary>
        /// 电子期刊
        /// </summary>
        EMagzine
    }

    /// <summary>
    /// DocUsage枚举的扩展方法
    /// </summary>
    public static class DocUsageExtensions
    {
        /// <summary>
        /// 获取文档分类实例
        /// 默认值：Normal
        /// </summary>
        /// <param name="usage">文档分类值</param>
        /// <returns>实例</returns>
        public static DocUsage GetInstance(string? usage)
        {
            usage = usage?.Trim() ?? "";
            return usage switch
            {
                "Normal" => DocUsage.Normal,
                "EBook" => DocUsage.EBook,
                "ENewsPaper" => DocUsage.ENewsPaper,
                "EMagzine" => DocUsage.EMagzine,
                _ => DocUsage.Normal
            };
        }
    }

    /// <summary>
    /// 文档元数据信息描述
    /// 
    /// 对应 Java 的 org.ofdrw.core.basicStructure.ofd.docInfo.CT_DocInfo
    /// </summary>
    /// <author>翻译自 权观宇 的 Java 实现</author>
    /// <since>2019-09-29 10:14:41</since>
    public class CtDocInfo : OfdElement
    {
        /// <summary>
        /// 使用现有元素创建文档元数据信息
        /// </summary>
        /// <param name="element">XML元素</param>
        public CtDocInfo(XElement element) : base(element)
        {
        }

        /// <summary>
        /// 创建新的文档元数据信息
        /// </summary>
        public CtDocInfo() : base("DocInfo")
        {
        }

        #region DocID - 文档标识符

        /// <summary>
        /// 【必选】
        /// 设置文件标识符，标识符应该是一个UUID
        /// </summary>
        /// <param name="docId">UUID文件标识</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetDocID(Guid docId)
        {
            SetOfdEntity("DocID", docId.ToString("N"));
            return this;
        }

        /// <summary>
        /// 随机产生一个UUID作为文件标识符
        /// </summary>
        /// <returns>当前实例</returns>
        public CtDocInfo RandomDocID()
        {
            return SetDocID(Guid.NewGuid());
        }

        /// <summary>
        /// 【必选】
        /// 采用UUID算法生成的由32个字符组成的文件标识。每个DocID在
        /// 文件创建或生成的时候进行分配。
        /// </summary>
        /// <returns>文件标识符</returns>
        public string? GetDocID()
        {
            return GetOfdElementText("DocID");
        }

        /// <summary>
        /// 文档ID属性（便捷访问）
        /// </summary>
        public string? DocID
        {
            get => GetDocID();
            set => SetDocID(Guid.Parse(value ?? throw new ArgumentNullException(nameof(value))));
        }

        #endregion

        #region Title - 文档标题

        /// <summary>
        /// 【可选】
        /// 设置文档标题。标题可以与文件名不同
        /// </summary>
        /// <param name="title">标题</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetTitle(string title)
        {
            SetOfdEntity("Title", title);
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取文档标题。标题可以与文件名不同
        /// </summary>
        /// <returns>文档标题</returns>
        public string? GetTitle()
        {
            return GetOfdElementText("Title");
        }

        /// <summary>
        /// 文档标题属性（便捷访问）
        /// </summary>
        public string? Title
        {
            get => GetTitle();
            set => SetTitle(value!);
        }

        #endregion

        #region Author - 文档作者

        /// <summary>
        /// 【可选】
        /// 设置文档作者
        /// </summary>
        /// <param name="author">文档作者</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetAuthor(string author)
        {
            SetOfdEntity("Author", author);
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取文档作者
        /// </summary>
        /// <returns>文档作者</returns>
        public string? GetAuthor()
        {
            return GetOfdElementText("Author");
        }

        /// <summary>
        /// 文档作者属性（便捷访问）
        /// </summary>
        public string? Author
        {
            get => GetAuthor();
            set => SetAuthor(value!);
        }

        #endregion

        #region Subject - 文档主题

        /// <summary>
        /// 【可选】
        /// 设置文档主题
        /// </summary>
        /// <param name="subject">文档主题</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetSubject(string subject)
        {
            SetOfdEntity("Subject", subject);
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取文档主题
        /// </summary>
        /// <returns>文档主题</returns>
        public string? GetSubject()
        {
            return GetOfdElementText("Subject");
        }

        /// <summary>
        /// 文档主题属性（便捷访问）
        /// </summary>
        public string? Subject
        {
            get => GetSubject();
            set => SetSubject(value!);
        }

        #endregion

        #region Abstract - 文档摘要

        /// <summary>
        /// 【可选】
        /// 设置文档摘要与注释
        /// </summary>
        /// <param name="abstractText">文档摘要与注释</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetAbstract(string abstractText)
        {
            SetOfdEntity("Abstract", abstractText);
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取文档摘要与注释
        /// </summary>
        /// <returns>文档摘要与注释</returns>
        public string? GetAbstract()
        {
            return GetOfdElementText("Abstract");
        }

        /// <summary>
        /// 文档摘要属性（便捷访问）
        /// </summary>
        public string? Abstract
        {
            get => GetAbstract();
            set => SetAbstract(value!);
        }

        #endregion

        #region CreationDate - 创建日期

        /// <summary>
        /// 【可选】
        /// 设置文件创建日期
        /// </summary>
        /// <param name="creationDate">文件创建日期</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetCreationDate(DateTime creationDate)
        {
            SetOfdEntity("CreationDate", creationDate.ToString("yyyy-MM-dd"));
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取文件创建日期
        /// </summary>
        /// <returns>创建日期</returns>
        public DateTime? GetCreationDate()
        {
            var dateStr = GetOfdElementText("CreationDate");
            if (string.IsNullOrWhiteSpace(dateStr))
                return null;
            
            return DateTime.TryParseExact(dateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var date) 
                ? date : null;
        }

        /// <summary>
        /// 文件创建日期属性（便捷访问）
        /// </summary>
        public DateTime? CreationDate
        {
            get => GetCreationDate();
            set => SetCreationDate(value ?? throw new ArgumentNullException(nameof(value)));
        }

        #endregion

        #region ModDate - 修改日期

        /// <summary>
        /// 【可选】
        /// 设置文档最近修改日期
        /// </summary>
        /// <param name="modDate">文档最近修改日期</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetModDate(DateTime modDate)
        {
            SetOfdEntity("ModDate", modDate.ToString("yyyy-MM-dd"));
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取文档最近修改日期
        /// </summary>
        /// <returns>文档最近修改日期</returns>
        public DateTime? GetModDate()
        {
            var dateStr = GetOfdElementText("ModDate");
            if (string.IsNullOrWhiteSpace(dateStr))
                return null;
            
            return DateTime.TryParseExact(dateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var date) 
                ? date : null;
        }

        /// <summary>
        /// 文档最近修改日期属性（便捷访问）
        /// </summary>
        public DateTime? ModDate
        {
            get => GetModDate();
            set => SetModDate(value ?? throw new ArgumentNullException(nameof(value)));
        }

        #endregion

        #region DocUsage - 文档分类

        /// <summary>
        /// 【可选】
        /// 设置文档分类，可取值如下：
        /// Normal——普通文档
        /// EBook——电子书
        /// ENewsPaper——电子报纸
        /// EMagzine——电子期刊
        /// 默认值为 Normal
        /// </summary>
        /// <param name="docUsage">文档分类</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetDocUsage(DocUsage docUsage)
        {
            SetOfdEntity("DocUsage", docUsage.ToString());
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取文档分类
        /// 默认值为 Normal
        /// </summary>
        /// <returns>文档分类</returns>
        public DocUsage GetDocUsage()
        {
            var usageStr = GetOfdElementText("DocUsage");
            return DocUsageExtensions.GetInstance(usageStr);
        }

        /// <summary>
        /// 文档分类属性（便捷访问）
        /// </summary>
        public DocUsage DocUsage
        {
            get => GetDocUsage();
            set => SetDocUsage(value);
        }

        #endregion

        #region Cover - 文档封面

        /// <summary>
        /// 【可选】
        /// 设置文档封面，此路径指向一个图片文件
        /// </summary>
        /// <param name="cover">文档封面路径</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetCover(StLoc cover)
        {
            if (cover == null)
                throw new ArgumentNullException(nameof(cover));
            
            var coverElement = new XElement("Cover", cover.ToString());
            Set(new OfdElement(coverElement));
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 设置文档封面路径
        /// </summary>
        /// <param name="cover">文档封面路径</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetCover(string cover)
        {
            var loc = new StLoc(cover); // 修复：使用构造函数代替GetInstance方法
            if (loc == null)
                throw new ArgumentException("无效的封面路径", nameof(cover));
            
            return SetCover(loc);
        }

        /// <summary>
        /// 【可选】
        /// 获取文档封面，此路径指向一个图片文件
        /// </summary>
        /// <returns>文档封面路径</returns>
        public StLoc? GetCover()
        {
            var locStr = GetOfdElementText("Cover");
            if (string.IsNullOrWhiteSpace(locStr))
                return null;
            
            return new StLoc(locStr);
        }

        /// <summary>
        /// 文档封面路径属性（便捷访问）
        /// </summary>
        public StLoc? Cover
        {
            get => GetCover();
            set => SetCover(value!);
        }

        #endregion

        #region Keywords - 关键词

        /// <summary>
        /// 【可选】
        /// 设置关键词集合
        /// 每一个关键词用一个"Keyword"子节点来表达
        /// </summary>
        /// <param name="keywords">关键词集合</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetKeywords(object keywords) // TODO: 等待Keywords类迁移后替换为正确类型
        {
            if (keywords == null)
                throw new ArgumentNullException(nameof(keywords));
            
            throw new NotImplementedException("等待Keywords类迁移");
        }

        /// <summary>
        /// 添加关键词
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <returns>当前实例</returns>
        public CtDocInfo AddKeyword(string keyword)
        {
            // TODO: 实现关键词添加逻辑
            throw new NotImplementedException("等待Keywords类迁移");
        }

        /// <summary>
        /// 【可选】
        /// 获取关键词集合
        /// </summary>
        /// <returns>关键词集合或null</returns>
        public object? GetKeywords() // TODO: 等待Keywords类迁移后替换为正确类型
        {
            throw new NotImplementedException("等待Keywords类迁移");
        }

        #endregion

        #region Creator - 创建应用程序

        /// <summary>
        /// 【可选】
        /// 设置创建文档的应用程序
        /// </summary>
        /// <param name="creator">创建文档的应用程序</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetCreator(string creator)
        {
            SetOfdEntity("Creator", creator);
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取创建文档的应用程序
        /// </summary>
        /// <returns>创建文档的应用程序或null</returns>
        public string? GetCreator()
        {
            return GetOfdElementText("Creator");
        }

        /// <summary>
        /// 创建文档的应用程序属性（便捷访问）
        /// </summary>
        public string? Creator
        {
            get => GetCreator();
            set => SetCreator(value!);
        }

        #endregion

        #region CreatorVersion - 创建应用程序版本

        /// <summary>
        /// 【可选】
        /// 设置创建文档的应用程序版本信息
        /// </summary>
        /// <param name="creatorVersion">创建文档的应用程序版本信息</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetCreatorVersion(string creatorVersion)
        {
            SetOfdEntity("CreatorVersion", creatorVersion);
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取创建文档的应用程序版本信息
        /// </summary>
        /// <returns>创建文档的应用程序版本信息或null</returns>
        public string? GetCreatorVersion()
        {
            return GetOfdElementText("CreatorVersion");
        }

        /// <summary>
        /// 创建文档的应用程序版本属性（便捷访问）
        /// </summary>
        public string? CreatorVersion
        {
            get => GetCreatorVersion();
            set => SetCreatorVersion(value!);
        }

        #endregion

        #region CustomDatas - 自定义元数据

        /// <summary>
        /// 【可选】
        /// 设置用户自定义元数据集合。其子节点为 CustomData
        /// </summary>
        /// <param name="customDatas">用户自定义元数据集合</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetCustomDatas(object customDatas) // TODO: 等待CustomDatas类迁移后替换为正确类型
        {
            if (customDatas == null)
                throw new ArgumentNullException(nameof(customDatas));
            
            throw new NotImplementedException("等待CustomDatas类迁移");
        }

        /// <summary>
        /// 【可选】
        /// 获取用户自定义元数据集合。其子节点为 CustomData
        /// </summary>
        /// <returns>用户自定义元数据集合</returns>
        public object? GetCustomDatas() // TODO: 等待CustomDatas类迁移后替换为正确类型
        {
            throw new NotImplementedException("等待CustomDatas类迁移");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查是否有必选的DocID
        /// </summary>
        /// <returns>是否有DocID</returns>
        public bool HasDocID()
        {
            return !string.IsNullOrWhiteSpace(GetDocID());
        }

        /// <summary>
        /// 检查是否有基本信息（标题、作者）
        /// </summary>
        /// <returns>是否有基本信息</returns>
        public bool HasBasicInfo()
        {
            return !string.IsNullOrWhiteSpace(GetTitle()) || !string.IsNullOrWhiteSpace(GetAuthor());
        }

        /// <summary>
        /// 检查是否有日期信息
        /// </summary>
        /// <returns>是否有日期信息</returns>
        public bool HasDateInfo()
        {
            return GetCreationDate().HasValue || GetModDate().HasValue;
        }

        /// <summary>
        /// 获取文档信息摘要
        /// </summary>
        /// <returns>文档信息摘要</returns>
        public string GetSummary()
        {
            var title = GetTitle() ?? "未命名文档";
            var author = GetAuthor();
            var usage = GetDocUsage();
            
            var summary = $"{title} [{usage}]";
            if (!string.IsNullOrWhiteSpace(author))
                summary += $" - {author}";
            
            return summary;
        }

        /// <summary>
        /// 设置基本文档信息
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="author">作者</param>
        /// <param name="usage">文档分类</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetBasicInfo(string title, string? author = null, DocUsage usage = DocUsage.Normal)
        {
            SetTitle(title);
            if (!string.IsNullOrWhiteSpace(author))
                SetAuthor(author);
            SetDocUsage(usage);
            return this;
        }

        /// <summary>
        /// 设置创建信息
        /// </summary>
        /// <param name="creator">创建应用程序</param>
        /// <param name="version">版本信息</param>
        /// <returns>当前实例</returns>
        public CtDocInfo SetCreationInfo(string creator, string? version = null)
        {
            SetCreator(creator);
            if (!string.IsNullOrWhiteSpace(version))
                SetCreatorVersion(version);
            SetCreationDate(DateTime.Now);
            return this;
        }

        #endregion

        #region 验证

        /// <summary>
        /// 验证文档信息的有效性
        /// </summary>
        /// <returns>验证结果</returns>
        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // 验证必选的DocID
            if (!HasDocID())
            {
                result.AddError("缺少必选元素: DocID");
            }
            else
            {
                var docId = GetDocID();
                if (docId!.Length != 32)
                {
                    result.AddWarning("DocID长度不是标准的32位UUID格式");
                }
            }

            // 验证日期格式
            var creationDate = GetCreationDate();
            var modDate = GetModDate();
            
            if (creationDate.HasValue && modDate.HasValue && modDate < creationDate)
            {
                result.AddWarning("修改日期早于创建日期");
            }

            return result;
        }

        /// <summary>
        /// 快速验证文档信息是否有效
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValid()
        {
            return Validate().IsValid;
        }

        #endregion

        /// <summary>
        /// 获取限定名称
        /// </summary>
        /// <returns>限定名称</returns>
        public override string GetQualifiedName()
        {
            return "ofd:DocInfo";
        }

        /// <summary>
        /// 转换为字符串表示
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return GetSummary();
        }
    }
}
