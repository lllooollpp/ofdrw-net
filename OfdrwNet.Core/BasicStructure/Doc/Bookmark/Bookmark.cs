using System;
using System.Xml.Linq;
using OfdrwNet.Core;
using OfdrwNet.Core.Action.ActionType.ActionGoto;

namespace OfdrwNet.Core.BasicStructure.Doc.Bookmark
{
    /// <summary>
    /// 本标准支持书签，可以将常用位置定义为书签，
    /// 文档可以包含一组书签。
    /// 
    /// 对应 Java 的 org.ofdrw.core.basicStructure.doc.bookmark.Bookmark
    /// 7.5 图 11 书签结构
    /// </summary>
    /// <author>翻译自 权观宇 的 Java 实现</author>
    /// <since>2019-10-09 08:06:35</since>
    public class Bookmark : OfdElement
    {
        /// <summary>
        /// 使用现有元素创建书签
        /// </summary>
        /// <param name="element">XML元素</param>
        public Bookmark(XElement element) : base(element)
        {
        }

        /// <summary>
        /// 创建新的书签
        /// </summary>
        public Bookmark() : base("Bookmark")
        {
        }

        /// <summary>
        /// 创建书签
        /// </summary>
        /// <param name="name">书签名称</param>
        /// <param name="dest">书签对应的文档版位置</param>
        public Bookmark(string name, CtDest dest) : this()
        {
            SetBookmarkName(name);
            SetDest(dest);
        }

        #region BookmarkName - 书签名称

        /// <summary>
        /// 【必选 属性】
        /// 设置 书签名称
        /// </summary>
        /// <param name="name">书签名称</param>
        /// <returns>当前实例</returns>
        public Bookmark SetBookmarkName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("书签名称不能为空", nameof(name));
            
            AddAttribute("Name", name);
            return this;
        }

        /// <summary>
        /// 【必选 属性】
        /// 获取 书签名称
        /// </summary>
        /// <returns>书签名称</returns>
        public string? GetBookmarkName()
        {
            return GetAttributeValue("Name");
        }

        /// <summary>
        /// 书签名称属性（便捷访问）
        /// </summary>
        public string? BookmarkName
        {
            get => GetBookmarkName();
            set => SetBookmarkName(value!);
        }

        #endregion

        #region Dest - 文档版位置

        /// <summary>
        /// 【必选】
        /// 设置 书签对应的文档版位置
        /// 见表 54
        /// </summary>
        /// <param name="dest">书签对应的文档版位置</param>
        /// <returns>当前实例</returns>
        public Bookmark SetDest(CtDest dest)
        {
            if (dest == null)
                throw new ArgumentNullException(nameof(dest));
            
            // 先移除现有的目标位置元素
            var existingDest = Element.Element("Dest");
            existingDest?.Remove();
            
            // 添加新的目标位置元素
            Element.Add(dest.Element);
            return this;
        }

        /// <summary>
        /// 【必选】
        /// 获取 书签对应的文档版位置
        /// 见表 54
        /// </summary>
        /// <returns>书签对应的文档版位置</returns>
        public CtDest? GetDest()
        {
            var destElement = Element.Element("Dest");
            return destElement != null ? new CtDest(destElement) : null;
        }

        /// <summary>
        /// 书签对应的文档版位置属性（便捷访问）
        /// </summary>
        public CtDest? Dest
        {
            get => GetDest();
            set => SetDest(value ?? throw new ArgumentNullException(nameof(value)));
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查书签是否有有效的名称
        /// </summary>
        /// <returns>是否有有效的名称</returns>
        public bool HasValidName()
        {
            return !string.IsNullOrWhiteSpace(GetBookmarkName());
        }

        /// <summary>
        /// 检查书签是否有目标位置
        /// </summary>
        /// <returns>是否有目标位置</returns>
        public bool HasDest()
        {
            try
            {
                return GetDest() != null;
            }
            catch (NotImplementedException)
            {
                return false;
            }
        }

        /// <summary>
        /// 检查书签配置是否完整
        /// </summary>
        /// <returns>是否完整</returns>
        public bool IsComplete()
        {
            return HasValidName() && HasDest();
        }

        /// <summary>
        /// 获取书签的显示名称
        /// </summary>
        /// <returns>显示名称</returns>
        public string GetDisplayName()
        {
            var name = GetBookmarkName();
            return !string.IsNullOrWhiteSpace(name) ? $"书签: {name}" : "书签 (未命名)";
        }

        /// <summary>
        /// 复制书签配置（不包括子元素）
        /// </summary>
        /// <returns>新的书签实例</returns>
        public Bookmark CloneConfiguration()
        {
            var newBookmark = new Bookmark();
            
            var name = GetBookmarkName();
            if (!string.IsNullOrWhiteSpace(name))
                newBookmark.SetBookmarkName(name);
            
            // 复制目标位置
            var dest = GetDest();
            if (dest != null)
                newBookmark.SetDest(dest.Clone());
            
            return newBookmark;
        }

        /// <summary>
        /// 更新书签名称
        /// </summary>
        /// <param name="newName">新的书签名称</param>
        /// <returns>当前实例</returns>
        public Bookmark UpdateBookmarkName(string newName)
        {
            return SetBookmarkName(newName);
        }

        /// <summary>
        /// 比较两个书签是否相等（基于名称）
        /// </summary>
        /// <param name="other">另一个书签</param>
        /// <returns>是否相等</returns>
        public bool EqualsByName(Bookmark? other)
        {
            if (other == null) return false;
            
            var thisName = GetBookmarkName();
            var otherName = other.GetBookmarkName();
            
            return string.Equals(thisName, otherName, StringComparison.Ordinal);
        }

        /// <summary>
        /// 获取书签的简要信息
        /// </summary>
        /// <returns>书签简要信息</returns>
        public string GetSummary()
        {
            var name = GetBookmarkName() ?? "未命名";
            var hasDestStr = HasDest() ? "有目标" : "无目标";
            return $"书签 [{name}] - {hasDestStr}";
        }

        #endregion

        #region 验证

        /// <summary>
        /// 验证书签配置的有效性
        /// </summary>
        /// <returns>验证结果</returns>
        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // 验证必选属性Name
            var name = GetBookmarkName();
            if (string.IsNullOrWhiteSpace(name))
            {
                result.AddError("缺少必选属性: Name");
            }
            else
            {
                if (name.Length > 255)
                {
                    result.AddWarning("书签名称过长，可能影响显示");
                }
                
                if (name.Contains("\n") || name.Contains("\r"))
                {
                    result.AddWarning("书签名称包含换行符");
                }
            }

            // 验证必选元素Dest（等待类迁移后实现）
            try
            {
                var dest = GetDest();
                if (dest == null)
                {
                    result.AddError("缺少必选元素: Dest");
                }
            }
            catch (NotImplementedException)
            {
                result.AddWarning("Dest验证跳过：等待CT_Dest类迁移");
            }

            return result;
        }

        /// <summary>
        /// 快速验证书签是否有效
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
            return "ofd:Bookmark";
        }

        /// <summary>
        /// 转换为字符串表示
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return GetDisplayName();
        }

        /// <summary>
        /// 重写Equals方法
        /// </summary>
        /// <param name="obj">比较对象</param>
        /// <returns>是否相等</returns>
        public override bool Equals(object? obj)
        {
            if (obj is Bookmark other)
                return EqualsByName(other);
            
            return base.Equals(obj);
        }

        /// <summary>
        /// 重写GetHashCode方法
        /// </summary>
        /// <returns>哈希码</returns>
        public override int GetHashCode()
        {
            var name = GetBookmarkName();
            return name?.GetHashCode() ?? base.GetHashCode();
        }
    }
}
