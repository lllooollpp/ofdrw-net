using System.Text;

namespace OfdrwNet.Converter.Export;

/// <summary>
/// 文本导出器
/// 对应 Java 版本的 org.ofdrw.converter.export.TextExporter
/// 从OFD文档中提取纯文本内容
/// </summary>
public class TextExporter : OFDExporterBase
{
    private readonly Encoding _encoding;
    private readonly StringBuilder _textBuilder;
    private readonly string _lineBreak;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="ofdPath">OFD文件路径</param>
    /// <param name="outputPath">输出文本文件路径</param>
    /// <param name="encoding">文本编码，默认UTF-8</param>
    public TextExporter(string ofdPath, string outputPath, Encoding? encoding = null) 
        : base(ofdPath, Path.GetDirectoryName(outputPath) ?? ".")
    {
        _encoding = encoding ?? Encoding.UTF8;
        _textBuilder = new StringBuilder();
        _lineBreak = Environment.NewLine;
        
        // 设置输出文件路径
        _outputPaths.Add(outputPath);
    }

    /// <summary>
    /// 导出所有页面的文本
    /// </summary>
    public override async Task ExportAsync(CancellationToken cancellationToken = default)
    {
        Initialize();
        
        var pageCount = _reader!.GetNumberOfPages();
        _textBuilder.Clear();

        for (int pageNum = 0; pageNum < pageCount; pageNum++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (pageNum > 0)
            {
                _textBuilder.AppendLine();
                _textBuilder.AppendLine("--- 分页 ---");
                _textBuilder.AppendLine();
            }
            
            await ExtractPageText(pageNum, cancellationToken);
        }

        // 写入文件
        var outputPath = _outputPaths[0];
        await File.WriteAllTextAsync(outputPath, _textBuilder.ToString(), _encoding, cancellationToken);
        
        Console.WriteLine($"文本已导出到: {outputPath}");
    }

    /// <summary>
    /// 导出单个页面（内部实现）
    /// </summary>
    protected override async Task ExportPageAsync(int pageNum, CancellationToken cancellationToken)
    {
        _textBuilder.Clear();
        await ExtractPageText(pageNum, cancellationToken);
        
        var outputPath = GenerateOutputFileName(pageNum, ".txt");
        await File.WriteAllTextAsync(outputPath, _textBuilder.ToString(), _encoding, cancellationToken);
        
        // 更新输出路径列表
        if (!_outputPaths.Contains(outputPath))
        {
            _outputPaths.Add(outputPath);
        }
        
        Console.WriteLine($"页面 {pageNum + 1} 文本已导出到: {outputPath}");
    }

    /// <summary>
    /// 提取指定页面的文本
    /// </summary>
    /// <param name="pageNum">页码（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task ExtractPageText(int pageNum, CancellationToken cancellationToken)
    {
        var pageInfo = GetPageInfo(pageNum);
        
        // 获取页面的所有图层
        var layers = pageInfo.GetAllLayers();
        var textBlocks = new List<TextBlock>();

        foreach (var layer in layers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExtractTextFromLayer(layer, textBlocks);
        }

        // 按照Y坐标排序文本块（从上到下）
        textBlocks.Sort((a, b) => a.Y.CompareTo(b.Y));

        // 将文本块合并为段落
        await MergeTextBlocks(textBlocks, cancellationToken);
    }

    /// <summary>
    /// 从图层中提取文本
    /// </summary>
    /// <param name="layer">图层元素</param>
    /// <param name="textBlocks">文本块列表</param>
    private void ExtractTextFromLayer(System.Xml.Linq.XElement layer, List<TextBlock> textBlocks)
    {
        // 查找所有文本对象
        var textObjects = layer.Elements("TextObject");
        
        foreach (var textObject in textObjects)
        {
            var textBlock = ParseTextObject(textObject);
            if (textBlock != null)
            {
                textBlocks.Add(textBlock);
            }
        }
    }

    /// <summary>
    /// 解析文本对象
    /// </summary>
    /// <param name="textObject">文本对象元素</param>
    /// <returns>文本块</returns>
    private TextBlock? ParseTextObject(System.Xml.Linq.XElement textObject)
    {
        // 解析边界框
        var boundaryAttr = textObject.Attribute("Boundary")?.Value;
        if (string.IsNullOrEmpty(boundaryAttr))
            return null;

        var boundary = ParseBoundary(boundaryAttr);
        if (boundary.IsEmpty)
            return null;

        // 解析字体大小
        var fontSize = float.Parse(textObject.Attribute("Size")?.Value ?? "12");

        // 提取所有文本内容
        var textBuilder = new StringBuilder();
        var textCodeElements = textObject.Elements("TextCode");
        
        foreach (var textCode in textCodeElements)
        {
            var text = textCode.Value;
            if (!string.IsNullOrEmpty(text))
            {
                textBuilder.Append(text);
            }
        }

        var content = textBuilder.ToString();
        if (string.IsNullOrWhiteSpace(content))
            return null;

        return new TextBlock
        {
            X = boundary.Left,
            Y = boundary.Top,
            Width = boundary.Width,
            Height = boundary.Height,
            FontSize = fontSize,
            Content = content.Trim()
        };
    }

    /// <summary>
    /// 解析边界框字符串
    /// </summary>
    /// <param name="boundaryStr">边界框字符串</param>
    /// <returns>边界框</returns>
    private System.Drawing.RectangleF ParseBoundary(string boundaryStr)
    {
        var parts = boundaryStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4)
        {
            var x = float.Parse(parts[0]);
            var y = float.Parse(parts[1]);
            var width = float.Parse(parts[2]);
            var height = float.Parse(parts[3]);
            return new System.Drawing.RectangleF(x, y, width, height);
        }
        return System.Drawing.RectangleF.Empty;
    }

    /// <summary>
    /// 合并文本块为连续文本
    /// </summary>
    /// <param name="textBlocks">文本块列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task MergeTextBlocks(List<TextBlock> textBlocks, CancellationToken cancellationToken)
    {
        if (textBlocks.Count == 0)
            return;

        var currentY = textBlocks[0].Y;
        var lineHeight = textBlocks[0].FontSize * 1.2f; // 估算行高
        var currentLine = new StringBuilder();

        foreach (var block in textBlocks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 如果Y坐标变化超过行高，认为是新行
            if (Math.Abs(block.Y - currentY) > lineHeight / 2)
            {
                if (currentLine.Length > 0)
                {
                    _textBuilder.AppendLine(currentLine.ToString().Trim());
                    currentLine.Clear();
                }
                currentY = block.Y;
            }

            // 添加文本到当前行
            if (currentLine.Length > 0 && !block.Content.StartsWith(" "))
            {
                currentLine.Append(" ");
            }
            currentLine.Append(block.Content);
        }

        // 添加最后一行
        if (currentLine.Length > 0)
        {
            _textBuilder.AppendLine(currentLine.ToString().Trim());
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 文本块类
    /// </summary>
    private class TextBlock
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float FontSize { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}