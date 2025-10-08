using OfdrwNet.Core;
using System.Text;
using System.Xml.Linq;

namespace OfdrwNet.Text;

/// <summary>
/// 增强的 OFD 文本提取器，整合多种文本提取策略
/// 合并原有的 ContentExtractor 和其他文本提取逻辑
/// </summary>
public class EnhancedTextExtractor : ITextExtractor
{
    private readonly ITextParser _textParser;
    private readonly ITextMerger _textMerger;
    private readonly Func<int, dynamic>? _getPageInfo;
    private readonly Func<int>? _getPageCount;
    private readonly TextExtractionOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="getPageInfo">获取页面信息的委托</param>
    /// <param name="getPageCount">获取页面总数的委托</param>
    /// <param name="textParser">文本解析器</param>
    /// <param name="textMerger">文本合并器</param>
    /// <param name="options">提取选项</param>
    public EnhancedTextExtractor(
        Func<int, dynamic>? getPageInfo = null,
        Func<int>? getPageCount = null,
        ITextParser? textParser = null,
        ITextMerger? textMerger = null,
        TextExtractionOptions? options = null)
    {
        _getPageInfo = getPageInfo;
        _getPageCount = getPageCount;
        _textParser = textParser ?? new EnhancedTextParser();
        _textMerger = textMerger ?? new SmartTextMerger();
        _options = options ?? new TextExtractionOptions();
    }

    /// <summary>
    /// 提取指定页面的文本块
    /// </summary>
    /// <param name="pageNum">页码（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>页面文本块列表</returns>
    public async Task<IEnumerable<ITextBlock>> ExtractPageTextBlocksAsync(int pageNum, CancellationToken cancellationToken = default)
    {
        if (_getPageInfo == null)
        {
            await Task.CompletedTask;
            return Enumerable.Empty<ITextBlock>();
        }

        try
        {
            var pageInfo = _getPageInfo(pageNum);
            var layers = GetAllLayersFromPageInfo(pageInfo);
            var textBlocks = new List<ITextBlock>();

            foreach (var layer in layers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var layerTextBlocks = ExtractTextFromLayer(layer);
                textBlocks.AddRange(layerTextBlocks);
            }

            // 根据选项排序
            var sortedBlocks = _options.SortByPosition
                ? textBlocks.OrderBy(b => b.Y).ThenBy(b => b.X)
                : textBlocks.OrderBy(b => b.Y);

            await Task.CompletedTask;
            return sortedBlocks;
        }
        catch (Exception ex)
        {
            if (_options.ThrowOnError)
                throw;

            System.Diagnostics.Debug.WriteLine($"[EnhancedTextExtractor] 提取页面 {pageNum} 文本失败: {ex.Message}");
            return Enumerable.Empty<ITextBlock>();
        }
    }

    /// <summary>
    /// 提取指定页面的文本
    /// </summary>
    /// <param name="pageNum">页码（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>页面文本内容</returns>
    public async Task<string> ExtractPageTextAsync(int pageNum, CancellationToken cancellationToken = default)
    {
        var textBlocks = await ExtractPageTextBlocksAsync(pageNum, cancellationToken);
        return await _textMerger.MergeTextBlocksAsync(textBlocks, cancellationToken);
    }

    /// <summary>
    /// 提取所有页面的文本
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>全部文本内容</returns>
    public async Task<string> ExtractAllTextAsync(CancellationToken cancellationToken = default)
    {
        if (_getPageCount == null)
        {
            return string.Empty;
        }

        var pageCount = _getPageCount();
        var textBuilder = new StringBuilder();

        for (int pageNum = 0; pageNum < pageCount; pageNum++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (pageNum > 0 && _options.IncludePageSeparator)
            {
                textBuilder.AppendLine();
                textBuilder.AppendLine($"--- {_options.PageSeparatorText} {pageNum + 1} ---");
                textBuilder.AppendLine();
            }

            var pageText = await ExtractPageTextAsync(pageNum, cancellationToken);
            if (!string.IsNullOrEmpty(pageText))
            {
                textBuilder.Append(pageText);
            }
        }

        return textBuilder.ToString();
    }

    /// <summary>
    /// 从图层中提取文本块
    /// </summary>
    /// <param name="layer">图层元素</param>
    /// <returns>文本块列表</returns>
    public IEnumerable<ITextBlock> ExtractTextFromLayer(XElement layer)
    {
        var textBlocks = new List<ITextBlock>();
        var textObjects = layer.Elements().Where(e =>
            string.Equals(e.Name.LocalName, "TextObject", StringComparison.OrdinalIgnoreCase));

        foreach (var textObject in textObjects)
        {
            try
            {
                var textBlock = _textParser.ParseTextObject(textObject);
                if (textBlock != null && !string.IsNullOrWhiteSpace(textBlock.Content))
                {
                    textBlocks.Add(textBlock);
                }
            }
            catch (Exception ex)
            {
                if (_options.ThrowOnError)
                    throw;

                System.Diagnostics.Debug.WriteLine($"[EnhancedTextExtractor] 解析文本对象失败: {ex.Message}");
            }
        }

        return textBlocks;
    }

    /// <summary>
    /// 从页面信息中获取所有图层
    /// </summary>
    /// <param name="pageInfo">页面信息</param>
    /// <returns>图层列表</returns>
    private IEnumerable<XElement> GetAllLayersFromPageInfo(dynamic pageInfo)
    {
        try
        {
            // 尝试调用 GetAllLayers 方法
            var layers = pageInfo.GetAllLayers();
            return layers as IEnumerable<XElement> ?? Enumerable.Empty<XElement>();
        }
        catch
        {
            // 如果失败，返回空列表
            return Enumerable.Empty<XElement>();
        }
    }

    /// <summary>
    /// 提取文档统计信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文档统计</returns>
    public async Task<DocumentTextStatistics> GetDocumentStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new DocumentTextStatistics();

        if (_getPageCount == null)
            return stats;

        var pageCount = _getPageCount();
        stats.PageCount = pageCount;

        for (int pageNum = 0; pageNum < pageCount; pageNum++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var textBlocks = await ExtractPageTextBlocksAsync(pageNum, cancellationToken);
            var pageText = await _textMerger.MergeTextBlocksAsync(textBlocks, cancellationToken);

            stats.TotalCharCount += pageText.Length;
            stats.TotalWordCount += CountWords(pageText);
            stats.TotalTextBlockCount += textBlocks.Count();

            if (textBlocks.Any())
            {
                stats.PagesWithText++;
            }
        }

        return stats;
    }

    /// <summary>
    /// 计算单词数量
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>单词数量</returns>
    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        // 简单的单词计数（按空白字符分割）
        var words = text.Split(new char[] { ' ', '\t', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries);

        return words.Length;
    }
}

/// <summary>
/// 文本提取选项
/// </summary>
public class TextExtractionOptions
{
    /// <summary>
    /// 是否按位置排序文本块
    /// </summary>
    public bool SortByPosition { get; set; } = true;

    /// <summary>
    /// 是否包含页面分隔符
    /// </summary>
    public bool IncludePageSeparator { get; set; } = true;

    /// <summary>
    /// 页面分隔符文本
    /// </summary>
    public string PageSeparatorText { get; set; } = "页码";

    /// <summary>
    /// 遇到错误是否抛出异常
    /// </summary>
    public bool ThrowOnError { get; set; } = false;

    /// <summary>
    /// 最小文本块字符数（过滤小文本块）
    /// </summary>
    public int MinTextBlockLength { get; set; } = 1;

    /// <summary>
    /// 是否保留空白文本块
    /// </summary>
    public bool KeepEmptyBlocks { get; set; } = false;
}

/// <summary>
/// 文档文本统计信息
/// </summary>
public class DocumentTextStatistics
{
    /// <summary>
    /// 总页数
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// 包含文本的页数
    /// </summary>
    public int PagesWithText { get; set; }

    /// <summary>
    /// 总字符数
    /// </summary>
    public int TotalCharCount { get; set; }

    /// <summary>
    /// 总单词数
    /// </summary>
    public int TotalWordCount { get; set; }

    /// <summary>
    /// 总文本块数
    /// </summary>
    public int TotalTextBlockCount { get; set; }

    /// <summary>
    /// 平均每页字符数
    /// </summary>
    public double AverageCharsPerPage => PageCount > 0 ? (double)TotalCharCount / PageCount : 0;

    /// <summary>
    /// 平均每页文本块数
    /// </summary>
    public double AverageBlocksPerPage => PageCount > 0 ? (double)TotalTextBlockCount / PageCount : 0;
}
