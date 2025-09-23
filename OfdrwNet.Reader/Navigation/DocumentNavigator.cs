using System;
using System.Collections.Generic;
using OfdrwNet.Reader.Model;

namespace OfdrwNet.Reader.Navigation
{
    /// <summary>
    /// 文档导航器
    /// 支持书签、目录、超链接等导航功能
    /// </summary>
    public class DocumentNavigator
    {
        private readonly OfdDocument _document;
        public List<Bookmark> Bookmarks { get; } = new();
        public List<OutlineItem> Outlines { get; } = new();

        public DocumentNavigator(OfdDocument document)
        {
            _document = document ?? throw new System.ArgumentNullException(nameof(document));
            // TODO: 初始化书签和目录结构
        }

        /// <summary>
        /// 跳转到书签
        /// </summary>
        public bool GoToBookmark(string name)
        {
            var bm = Bookmarks.Find(b => b.Name == name);
            if (bm != null)
            {
                // TODO: 跳转到书签对应页面
                return true;
            }
            return false;
        }

        /// <summary>
        /// 跳转到目录项
        /// </summary>
        public bool GoToOutline(string title)
        {
            var outline = Outlines.Find(o => o.Title == title);
            if (outline != null)
            {
                // TODO: 跳转到目录对应页面
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 书签
    /// </summary>
    public class Bookmark
    {
        public string Name { get; set; } = "";
        public int PageIndex { get; set; }
    }

    /// <summary>
    /// 目录项
    /// </summary>
    public class OutlineItem
    {
        public string Title { get; set; } = "";
        public int PageIndex { get; set; }
        public List<OutlineItem> Children { get; set; } = new();
    }
}
