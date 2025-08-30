using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OfdrwNet.Core;

namespace OfdrwNet.Core.BasicStructure.Doc.Bookmark
{
    /// <summary>
    /// 文档的书签集，包含一组书签
    /// 
    /// 对应 Java 的 org.ofdrw.core.basicStructure.doc.bookmark.Bookmarks
    /// 7.5 文档根节点 表 5 文档根节点属性
    /// </summary>
    /// <author>翻译自 权观宇 的 Java 实现</author>
    /// <since>2019-10-09 08:01:58</since>
    public class Bookmarks : OfdElement
    {
        /// <summary>
        /// 使用现有元素创建书签集
        /// </summary>
        /// <param name="element">XML元素</param>
        public Bookmarks(XElement element) : base(element)
        {
        }

        /// <summary>
        /// 创建新的书签集
        /// </summary>
        public Bookmarks() : base("Bookmarks")
        {
        }

        #region 书签管理

        /// <summary>
        /// 【必选】
        /// 增加 书签
        /// </summary>
        /// <param name="bookmark">书签</param>
        /// <returns>当前实例</returns>
        public Bookmarks AddBookmark(Bookmark bookmark)
        {
            if (bookmark == null)
                throw new ArgumentNullException(nameof(bookmark));
            
            Add(bookmark);
            return this;
        }

        /// <summary>
        /// 【必选】
        /// 获取 书签列表
        /// </summary>
        /// <returns>书签列表</returns>
        public List<Bookmark> GetBookmarks()
        {
            return GetOfdElements("Bookmark", element => new Bookmark(element));
        }

        /// <summary>
        /// 书签列表属性（便捷访问）
        /// </summary>
        public List<Bookmark> BookmarkList => GetBookmarks();

        /// <summary>
        /// 书签数量
        /// </summary>
        public int Count => GetBookmarks().Count;

        #endregion

        #region 书签操作

        /// <summary>
        /// 批量添加书签
        /// </summary>
        /// <param name="bookmarks">书签集合</param>
        /// <returns>当前实例</returns>
        public Bookmarks AddBookmarks(IEnumerable<Bookmark> bookmarks)
        {
            if (bookmarks == null)
                throw new ArgumentNullException(nameof(bookmarks));
            
            foreach (var bookmark in bookmarks)
            {
                AddBookmark(bookmark);
            }
            return this;
        }

        /// <summary>
        /// 根据名称查找书签
        /// </summary>
        /// <param name="name">书签名称</param>
        /// <returns>找到的书签或null</returns>
        public Bookmark? FindBookmarkByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            
            return GetBookmarks().FirstOrDefault(b => 
                string.Equals(b.GetBookmarkName(), name, StringComparison.Ordinal));
        }

        /// <summary>
        /// 根据名称查找所有匹配的书签
        /// </summary>
        /// <param name="name">书签名称</param>
        /// <returns>匹配的书签列表</returns>
        public List<Bookmark> FindBookmarksByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Bookmark>();
            
            return GetBookmarks().Where(b => 
                string.Equals(b.GetBookmarkName(), name, StringComparison.Ordinal)).ToList();
        }

        /// <summary>
        /// 根据名称模糊搜索书签
        /// </summary>
        /// <param name="pattern">搜索模式</param>
        /// <returns>匹配的书签列表</returns>
        public List<Bookmark> SearchBookmarks(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return new List<Bookmark>();
            
            return GetBookmarks().Where(b => 
            {
                var bookmarkName = b.GetBookmarkName();
                return !string.IsNullOrWhiteSpace(bookmarkName) && 
                       bookmarkName.Contains(pattern, StringComparison.OrdinalIgnoreCase);
            }).ToList();
        }

        /// <summary>
        /// 检查是否包含指定名称的书签
        /// </summary>
        /// <param name="name">书签名称</param>
        /// <returns>是否包含</returns>
        public bool ContainsBookmark(string name)
        {
            return FindBookmarkByName(name) != null;
        }

        /// <summary>
        /// 移除指定的书签
        /// </summary>
        /// <param name="bookmark">要移除的书签</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveBookmark(Bookmark bookmark)
        {
            if (bookmark == null)
                return false;
            
            return Remove(bookmark);
        }

        /// <summary>
        /// 根据名称移除书签
        /// </summary>
        /// <param name="name">书签名称</param>
        /// <returns>移除的书签数量</returns>
        public int RemoveBookmarksByName(string name)
        {
            var bookmarksToRemove = FindBookmarksByName(name);
            int removedCount = 0;
            
            foreach (var bookmark in bookmarksToRemove)
            {
                if (RemoveBookmark(bookmark))
                    removedCount++;
            }
            
            return removedCount;
        }

        /// <summary>
        /// 清空所有书签
        /// </summary>
        /// <returns>当前实例</returns>
        public Bookmarks ClearBookmarks()
        {
            var bookmarks = GetBookmarks();
            foreach (var bookmark in bookmarks)
            {
                Remove(bookmark);
            }
            return this;
        }

        #endregion

        #region 排序和整理

        /// <summary>
        /// 根据书签名称排序
        /// </summary>
        /// <param name="ascending">是否升序排序</param>
        /// <returns>当前实例</returns>
        public Bookmarks SortByName(bool ascending = true)
        {
            var bookmarks = GetBookmarks();
            
            // 移除所有现有书签
            ClearBookmarks();
            
            // 排序并重新添加
            var sortedBookmarks = ascending 
                ? bookmarks.OrderBy(b => b.GetBookmarkName()).ToList()
                : bookmarks.OrderByDescending(b => b.GetBookmarkName()).ToList();
            
            AddBookmarks(sortedBookmarks);
            return this;
        }

        /// <summary>
        /// 获取所有书签名称
        /// </summary>
        /// <returns>书签名称列表</returns>
        public List<string> GetBookmarkNames()
        {
            return GetBookmarks()
                .Select(b => b.GetBookmarkName())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList()!;
        }

        /// <summary>
        /// 获取重复的书签名称
        /// </summary>
        /// <returns>重复的书签名称列表</returns>
        public List<string> GetDuplicateNames()
        {
            return GetBookmarkNames()
                .GroupBy(name => name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查是否为空书签集
        /// </summary>
        /// <returns>是否为空</returns>
        public bool IsEmpty()
        {
            return Count == 0;
        }

        /// <summary>
        /// 检查是否有有效的书签
        /// </summary>
        /// <returns>是否有有效书签</returns>
        public bool HasValidBookmarks()
        {
            return GetBookmarks().Any(b => b.HasValidName());
        }

        /// <summary>
        /// 获取有效书签数量
        /// </summary>
        /// <returns>有效书签数量</returns>
        public int GetValidBookmarkCount()
        {
            return GetBookmarks().Count(b => b.HasValidName());
        }

        /// <summary>
        /// 获取书签集的摘要信息
        /// </summary>
        /// <returns>摘要信息</returns>
        public string GetSummary()
        {
            var totalCount = Count;
            var validCount = GetValidBookmarkCount();
            var duplicateCount = GetDuplicateNames().Count;
            
            var summary = $"书签集 [总数: {totalCount}, 有效: {validCount}";
            if (duplicateCount > 0)
                summary += $", 重复: {duplicateCount}";
            summary += "]";
            
            return summary;
        }

        /// <summary>
        /// 创建书签的副本
        /// </summary>
        /// <returns>新的书签集实例</returns>
        public Bookmarks CloneBookmarks()
        {
            var newBookmarks = new Bookmarks();
            var bookmarkList = GetBookmarks();
            
            foreach (var bookmark in bookmarkList)
            {
                var clonedBookmark = bookmark.CloneConfiguration();
                newBookmarks.AddBookmark(clonedBookmark);
            }
            
            return newBookmarks;
        }

        #endregion

        #region 验证

        /// <summary>
        /// 验证书签集的有效性
        /// </summary>
        /// <returns>验证结果</returns>
        public override ValidationResult Validate()
        {
            var result = base.Validate();

            var bookmarks = GetBookmarks();
            
            // 检查是否有书签
            if (bookmarks.Count == 0)
            {
                result.AddWarning("书签集为空");
            }
            
            // 验证每个书签
            for (int i = 0; i < bookmarks.Count; i++)
            {
                var bookmark = bookmarks[i];
                var bookmarkResult = bookmark.Validate();
                
                if (!bookmarkResult.IsValid)
                {
                    result.AddError($"书签 #{i + 1} 验证失败");
                    result.Errors.AddRange(bookmarkResult.Errors);
                    result.Warnings.AddRange(bookmarkResult.Warnings);
                }
            }
            
            // 检查重复名称
            var duplicateNames = GetDuplicateNames();
            foreach (var duplicateName in duplicateNames)
            {
                result.AddWarning($"发现重复的书签名称: {duplicateName}");
            }

            return result;
        }

        /// <summary>
        /// 快速验证书签集是否有效
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
            return "ofd:Bookmarks";
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
