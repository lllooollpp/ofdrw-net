using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OfdrwNet.Converter.Interaction;

/// <summary>
/// 书签转换器。
/// </summary>
/// <remarks>
/// 将 PDF 书签（大纲/Outlines）转换为 OFD 书签结构。
/// FR-17: 书签映射与层级保留
/// </remarks>
public sealed class BookmarkConverter
{
    private readonly ILogger<BookmarkConverter> _logger;

    /// <summary>
    /// 初始化 BookmarkConverter 实例。
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public BookmarkConverter(ILogger<BookmarkConverter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 从 PDF 文档提取书签树。
    /// </summary>
    /// <param name="pdfDocument">PDF 文档对象（iText7 PdfDocument 或模拟对象）</param>
    /// <returns>书签根节点列表</returns>
    public IList<BookmarkNode> ConvertBookmarks(object pdfDocument)
    {
        if (pdfDocument == null)
        {
            throw new ArgumentNullException(nameof(pdfDocument));
        }

        try
        {
            var rootNodes = new List<BookmarkNode>();
            var outlines = GetOutlines(pdfDocument);

            if (outlines == null)
            {
                _logger.LogDebug("PDF document has no outlines/bookmarks");
                return rootNodes;
            }

            _logger.LogInformation("Converting PDF outlines to OFD bookmarks");

            // 获取顶层书签
            var topLevelBookmarks = GetAllBookmarks(outlines);

            foreach (var pdfBookmark in topLevelBookmarks)
            {
                try
                {
                    var node = ConvertBookmark(pdfBookmark);
                    if (node != null)
                    {
                        rootNodes.Add(node);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert bookmark");
                }
            }

            _logger.LogInformation("Converted {Count} top-level bookmarks", rootNodes.Count);
            return rootNodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert bookmarks from PDF document");
            return new List<BookmarkNode>();
        }
    }

    /// <summary>
    /// 获取 PDF 文档的大纲（Outlines）。
    /// </summary>
    private object? GetOutlines(object pdfDocument)
    {
        try
        {
            var type = pdfDocument.GetType();
            var getCatalogMethod = type.GetMethod("GetCatalog");

            if (getCatalogMethod != null)
            {
                var catalog = getCatalogMethod.Invoke(pdfDocument, null);
                if (catalog != null)
                {
                    var catalogType = catalog.GetType();
                    var getPdfObjectMethod = catalogType.GetMethod("GetPdfObject");

                    if (getPdfObjectMethod != null)
                    {
                        var pdfObject = getPdfObjectMethod.Invoke(catalog, null);
                        if (pdfObject != null)
                        {
                            var pdfObjectType = pdfObject.GetType();
                            var getAsDictionaryMethod = pdfObjectType.GetMethod("GetAsDictionary");

                            if (getAsDictionaryMethod != null)
                            {
                                var outlines = getAsDictionaryMethod.Invoke(pdfObject, new object[] { "Outlines" });
                                return outlines;
                            }
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get outlines from PDF");
            return null;
        }
    }

    /// <summary>
    /// 获取所有书签项。
    /// </summary>
    private IList<object> GetAllBookmarks(object outlines)
    {
        var bookmarks = new List<object>();

        try
        {
            // 占位实现：尝试获取第一个书签
            var type = outlines.GetType();
            var getFirstMethod = type.GetMethod("GetFirst") ?? type.GetMethod("First");

            if (getFirstMethod != null)
            {
                var firstBookmark = getFirstMethod.Invoke(outlines, null);
                if (firstBookmark != null)
                {
                    bookmarks.Add(firstBookmark);

                    // 获取后续兄弟节点
                    var currentBookmark = firstBookmark;
                    while (true)
                    {
                        var currentType = currentBookmark.GetType();
                        var getNextMethod = currentType.GetMethod("GetNext") ?? currentType.GetMethod("Next");

                        if (getNextMethod == null)
                        {
                            break;
                        }

                        var nextBookmark = getNextMethod.Invoke(currentBookmark, null);
                        if (nextBookmark == null)
                        {
                            break;
                        }

                        bookmarks.Add(nextBookmark);
                        currentBookmark = nextBookmark;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate bookmarks");
        }

        return bookmarks;
    }

    /// <summary>
    /// 转换单个书签节点。
    /// </summary>
    private BookmarkNode? ConvertBookmark(object pdfBookmark)
    {
        if (pdfBookmark == null)
        {
            return null;
        }

        try
        {
            var title = GetBookmarkTitle(pdfBookmark);
            if (string.IsNullOrWhiteSpace(title))
            {
                _logger.LogDebug("Bookmark has no title, skipping");
                return null;
            }

            var node = new BookmarkNode
            {
                Title = title,
                Destination = GetBookmarkDestination(pdfBookmark),
                IsOpen = IsBookmarkOpen(pdfBookmark),
                Children = new List<BookmarkNode>()
            };

            // 递归处理子书签
            var children = GetChildBookmarks(pdfBookmark);
            foreach (var childBookmark in children)
            {
                var childNode = ConvertBookmark(childBookmark);
                if (childNode != null)
                {
                    node.Children.Add(childNode);
                }
            }

            _logger.LogDebug(
                "Converted bookmark: {Title} ({ChildCount} children)",
                title, node.Children.Count);

            return node;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert bookmark");
            return null;
        }
    }

    /// <summary>
    /// 获取书签标题。
    /// </summary>
    private string? GetBookmarkTitle(object bookmark)
    {
        try
        {
            var type = bookmark.GetType();
            var getTitleMethod = type.GetMethod("GetTitle");

            if (getTitleMethod != null)
            {
                var title = getTitleMethod.Invoke(bookmark, null);
                return title?.ToString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取书签目标（页码或 URL）。
    /// </summary>
    private string? GetBookmarkDestination(object bookmark)
    {
        try
        {
            var type = bookmark.GetType();
            var getActionMethod = type.GetMethod("GetAction");

            if (getActionMethod != null)
            {
                var action = getActionMethod.Invoke(bookmark, null);
                if (action != null)
                {
                    // 尝试获取目标页码
                    var actionType = action.GetType();
                    var getDestinationMethod = actionType.GetMethod("GetDestination");

                    if (getDestinationMethod != null)
                    {
                        var destination = getDestinationMethod.Invoke(action, null);
                        if (destination != null)
                        {
                            return destination.ToString();
                        }
                    }

                    // 尝试获取 URI
                    var getUriMethod = actionType.GetMethod("GetUri");
                    if (getUriMethod != null)
                    {
                        var uri = getUriMethod.Invoke(action, null);
                        if (uri != null)
                        {
                            return uri.ToString();
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get bookmark destination");
            return null;
        }
    }

    /// <summary>
    /// 判断书签是否展开。
    /// </summary>
    private bool IsBookmarkOpen(object bookmark)
    {
        try
        {
            var type = bookmark.GetType();
            var isOpenMethod = type.GetMethod("IsOpen");

            if (isOpenMethod != null)
            {
                var isOpen = isOpenMethod.Invoke(bookmark, null);
                if (isOpen is bool b)
                {
                    return b;
                }
            }

            // 默认展开
            return true;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// 获取子书签列表。
    /// </summary>
    private IList<object> GetChildBookmarks(object bookmark)
    {
        var children = new List<object>();

        try
        {
            var type = bookmark.GetType();
            var getKidsMethod = type.GetMethod("GetKids") ?? type.GetMethod("GetChildren");

            if (getKidsMethod != null)
            {
                var kids = getKidsMethod.Invoke(bookmark, null);
                if (kids is System.Collections.IEnumerable enumerable)
                {
                    foreach (var kid in enumerable)
                    {
                        if (kid != null)
                        {
                            children.Add(kid);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get child bookmarks");
        }

        return children;
    }
}

/// <summary>
/// 书签节点。
/// </summary>
public sealed class BookmarkNode
{
    /// <summary>
    /// 书签标题。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 目标位置（页码、URL 或其他标识符）。
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// 是否展开显示子节点。
    /// </summary>
    public bool IsOpen { get; set; } = true;

    /// <summary>
    /// 子书签列表。
    /// </summary>
    public IList<BookmarkNode> Children { get; set; } = new List<BookmarkNode>();
}
