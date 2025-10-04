using OfdrwNet.Core;
using System.Text;
using System.Xml.Linq;

namespace OfdrwNet.Text;

/// <summary>
/// OFD 文本提取器实现，包含完整的文本提取逻辑
/// </summary>
public class TextExtractor : ITextExtractor
{
    private readonly ITextParser _textParser;
    private readonly ITextMerger _textMerger;
    private readonly Func<int, dynamic>? _getPageInfo;
    private readonly Func<int>? _getPageCount;

    /// <summary>
    /// 构造函数，用于依赖注入模式
    /// </summary>
    /// <param name="getPageInfo">获取页面信息的委托</param>
    /// <param name="getPageCount">获取页面总数的委托</param>
    /// <param name="textParser">文本解析器</param>
    /// <param name="textMerger">文本合并器</param>
    public TextExtractor(
        Func<int, dynamic>? getPageInfo = null,
        Func<int>? getPageCount = null,
        ITextParser? textParser = null,
        ITextMerger? textMerger = null)
    {
        _getPageInfo = getPageInfo;
        _getPageCount = getPageCount;
        _textParser = textParser ?? new TextParser();
        _textMerger = textMerger ?? new TextMerger();
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

        var pageInfo = _getPageInfo(pageNum);
        var layers = pageInfo.GetAllLayers();
        var textBlocks = new List<ITextBlock>();

        foreach (var layer in layers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var layerTextBlocks = ExtractTextFromLayer(layer);
            textBlocks.AddRange(layerTextBlocks);
        }

        await Task.CompletedTask;
        return textBlocks.OrderBy(b => b.Y);
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

            if (pageNum > 0)
            {
                textBuilder.AppendLine();
                textBuilder.AppendLine("--- 分页 ---");
                textBuilder.AppendLine();
            }

            var pageText = await ExtractPageTextAsync(pageNum, cancellationToken);
            textBuilder.Append(pageText);
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
        var textObjects = layer.Elements("TextObject");

        foreach (var textObject in textObjects)
        {
            var textBlock = _textParser.ParseTextObject(textObject);
            if (textBlock != null)
            {
                textBlocks.Add(textBlock);
            }
        }

        return textBlocks;
    }
}
