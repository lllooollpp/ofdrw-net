using OfdrwNet.Core.BasicStructure.PageObj;
using OfdrwNet.Core.BasicStructure.PageObj.Layer;
using OfdrwNet.Core.BasicStructure.PageObj.Layer.Block;
using OfdrwNet.Core.Text;
using System.Text;

namespace OfdrwNet.Reader;

/// <summary>
/// 内容提取器
/// 从OFD文档中提取文本内容
/// 对应Java版本的 org.ofdrw.reader.ContentExtractor
/// </summary>
public class ContentExtractor
{
    private readonly OfdReader _reader;

    /// <summary>
    /// 构造文本提取器
    /// </summary>
    /// <param name="reader">OFD读取器</param>
    public ContentExtractor(OfdReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    /// <summary>
    /// 提取指定页面的所有文本内容
    /// </summary>
    /// <param name="pageNum">页码，从1开始</param>
    /// <returns>页面的所有文本内容列表</returns>
    public List<string> GetPageContent(int pageNum)
    {
        var textContentList = new List<string>();
        
        try
        {
            var pageList = _reader.GetPageList();
            if (pageList == null || pageNum < 1 || pageNum > pageList.Count)
            {
                return textContentList;
            }

            var pageInfo = pageList[pageNum - 1];
            if (pageInfo?.Obj == null)
            {
                return textContentList;
            }

            // 解析页面内容
            var contentElements = pageInfo.Obj.Elements("Content");
            foreach (var contentElement in contentElements)
            {
                var layerElements = contentElement.Elements("Layer");
                foreach (var layerElement in layerElements)
                {
                    ExtractTextFromLayer(layerElement, textContentList);
                }
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出，返回已提取的内容
            System.Diagnostics.Debug.WriteLine($"提取页面{pageNum}文本时发生错误: {ex.Message}");
        }

        return textContentList;
    }

    /// <summary>
    /// 从层中提取文本
    /// </summary>
    /// <param name="layerElement">层元素</param>
    /// <param name="textList">文本列表</param>
    private void ExtractTextFromLayer(System.Xml.Linq.XElement layerElement, List<string> textList)
    {
        // 查找所有的TextObject元素
        var textObjects = layerElement.Descendants("TextObject");
        
        foreach (var textObject in textObjects)
        {
            ExtractTextFromTextObject(textObject, textList);
        }
    }

    /// <summary>
    /// 从TextObject中提取文本
    /// </summary>
    /// <param name="textObject">文本对象元素</param>
    /// <param name="textList">文本列表</param>
    private void ExtractTextFromTextObject(System.Xml.Linq.XElement textObject, List<string> textList)
    {
        var textCodes = textObject.Descendants("TextCode");
        
        foreach (var textCode in textCodes)
        {
            var content = textCode.Value?.Trim();
            if (!string.IsNullOrEmpty(content))
            {
                textList.Add(content);
            }
        }
    }

    /// <summary>
    /// 提取所有页面的文本内容
    /// </summary>
    /// <returns>所有文本内容列表</returns>
    public List<string> ExtractAll()
    {
        var allTextContent = new List<string>();
        
        try
        {
            var pageList = _reader.GetPageList();
            if (pageList != null)
            {
                for (int i = 1; i <= pageList.Count; i++)
                {
                    var pageContent = GetPageContent(i);
                    allTextContent.AddRange(pageContent);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"提取所有文本时发生错误: {ex.Message}");
        }

        return allTextContent;
    }

    /// <summary>
    /// 获取页面文本内容的简要摘要
    /// </summary>
    /// <param name="pageNum">页码</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>文本摘要</returns>
    public string GetPageTextSummary(int pageNum, int maxLength = 200)
    {
        var pageContent = GetPageContent(pageNum);
        if (pageContent.Count == 0)
        {
            return "（此页面无文本内容）";
        }

        var summary = string.Join(" ", pageContent);
        if (summary.Length > maxLength)
        {
            summary = summary.Substring(0, maxLength) + "...";
        }

        return summary;
    }

    /// <summary>
    /// 获取文档的总体统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public DocumentTextStatistics GetDocumentStatistics()
    {
        var stats = new DocumentTextStatistics();
        
        try
        {
            var pageList = _reader.GetPageList();
            if (pageList != null)
            {
                stats.TotalPages = pageList.Count;
                
                for (int i = 1; i <= pageList.Count; i++)
                {
                    var pageContent = GetPageContent(i);
                    stats.TotalTextObjects += pageContent.Count;
                    stats.TotalCharacters += pageContent.Sum(text => text.Length);
                    
                    if (pageContent.Count > 0)
                    {
                        stats.PagesWithText++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"统计文档信息时发生错误: {ex.Message}");
        }

        return stats;
    }
}

/// <summary>
/// 文档文本统计信息
/// </summary>
public class DocumentTextStatistics
{
    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// 包含文本的页面数
    /// </summary>
    public int PagesWithText { get; set; }

    /// <summary>
    /// 总文本对象数
    /// </summary>
    public int TotalTextObjects { get; set; }

    /// <summary>
    /// 总字符数
    /// </summary>
    public int TotalCharacters { get; set; }

    /// <summary>
    /// 获取统计信息摘要
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"页面: {TotalPages}页 | 含文本: {PagesWithText}页 | 文本对象: {TotalTextObjects}个 | 字符: {TotalCharacters}个";
    }
}
