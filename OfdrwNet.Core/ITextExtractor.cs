using System.Xml.Linq;

namespace OfdrwNet.Core;

/// <summary>
/// 文本块接口，表示一段连续的文本及其位置信息
/// </summary>
public interface ITextBlock
{
    /// <summary>
    /// X坐标
    /// </summary>
    float X { get; }

    /// <summary>
    /// Y坐标
    /// </summary>
    float Y { get; }

    /// <summary>
    /// 宽度
    /// </summary>
    float Width { get; }

    /// <summary>
    /// 高度
    /// </summary>
    float Height { get; }

    /// <summary>
    /// 字体大小
    /// </summary>
    float FontSize { get; }

    /// <summary>
    /// 文本内容
    /// </summary>
    string Content { get; }
}

/// <summary>
/// 文本解析器接口，负责解析OFD文档中的文本对象
/// </summary>
public interface ITextParser
{
    /// <summary>
    /// 解析文本对象
    /// </summary>
    /// <param name="textObject">文本对象元素</param>
    /// <returns>文本块</returns>
    ITextBlock? ParseTextObject(XElement textObject);

    /// <summary>
    /// 解析边界框字符串
    /// </summary>
    /// <param name="boundaryStr">边界框字符串</param>
    /// <returns>边界框</returns>
    System.Drawing.RectangleF ParseBoundary(string boundaryStr);
}

/// <summary>
/// 文本合并器接口，负责将文本块合并为连续文本
/// </summary>
public interface ITextMerger
{
    /// <summary>
    /// 合并文本块为连续文本
    /// </summary>
    /// <param name="textBlocks">文本块列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>合并后的文本</returns>
    Task<string> MergeTextBlocksAsync(IEnumerable<ITextBlock> textBlocks, CancellationToken cancellationToken = default);
}

/// <summary>
/// 文本提取器接口，定义从文档中提取文本的标准方法
/// </summary>
public interface ITextExtractor
{
    /// <summary>
    /// 提取指定页面的文本块
    /// </summary>
    /// <param name="pageNum">页码（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>页面文本块列表</returns>
    Task<IEnumerable<ITextBlock>> ExtractPageTextBlocksAsync(int pageNum, CancellationToken cancellationToken = default);

    /// <summary>
    /// 提取指定页面的文本
    /// </summary>
    /// <param name="pageNum">页码（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>页面文本内容</returns>
    Task<string> ExtractPageTextAsync(int pageNum, CancellationToken cancellationToken = default);

    /// <summary>
    /// 提取所有页面的文本
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>全部文本内容</returns>
    Task<string> ExtractAllTextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 从图层中提取文本块
    /// </summary>
    /// <param name="layer">图层元素</param>
    /// <returns>文本块列表</returns>
    IEnumerable<ITextBlock> ExtractTextFromLayer(XElement layer);
}
