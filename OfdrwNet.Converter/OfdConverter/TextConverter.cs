using System.Text;

namespace OfdrwNet.Converter.OfdConverter;

/// <summary>
/// 文本转换器
/// 对应 Java 版本的 org.ofdrw.converter.ofdconverter.TextConverter
/// 将纯文本文件转换为OFD文档
/// </summary>
public class TextConverter : OFDConverterBase
{
    private double _fontSize = 12.0; // 字体大小（毫米）
    private string _fontFamily = "SimSun"; // 字体族
    private double _lineSpacing = 1.5; // 行间距倍数
    private double _margin = 20.0; // 页边距（毫米）
    private Encoding _encoding = Encoding.UTF8; // 文本编码

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="outputPath">输出OFD文件路径</param>
    public TextConverter(string outputPath) : base(outputPath)
    {
    }

    /// <summary>
    /// 设置字体大小
    /// </summary>
    /// <param name="fontSize">字体大小（毫米）</param>
    /// <returns>this</returns>
    public TextConverter SetFontSize(double fontSize)
    {
        _fontSize = Math.Max(1.0, fontSize);
        return this;
    }

    /// <summary>
    /// 设置字体族
    /// </summary>
    /// <param name="fontFamily">字体族名称</param>
    /// <returns>this</returns>
    public TextConverter SetFontFamily(string fontFamily)
    {
        _fontFamily = fontFamily ?? "SimSun";
        return this;
    }

    /// <summary>
    /// 设置行间距
    /// </summary>
    /// <param name="lineSpacing">行间距倍数</param>
    /// <returns>this</returns>
    public TextConverter SetLineSpacing(double lineSpacing)
    {
        _lineSpacing = Math.Max(1.0, lineSpacing);
        return this;
    }

    /// <summary>
    /// 设置页边距
    /// </summary>
    /// <param name="margin">页边距（毫米）</param>
    /// <returns>this</returns>
    public TextConverter SetMargin(double margin)
    {
        _margin = Math.Max(0, margin);
        return this;
    }

    /// <summary>
    /// 设置文本编码
    /// </summary>
    /// <param name="encoding">文本编码</param>
    /// <returns>this</returns>
    public TextConverter SetEncoding(Encoding encoding)
    {
        _encoding = encoding ?? Encoding.UTF8;
        return this;
    }

    /// <summary>
    /// 转换文本文件为OFD
    /// </summary>
    public override async Task ConvertAsync(string inputPath, CancellationToken cancellationToken = default)
    {
        ValidateInputFile(inputPath);

        try
        {
            Console.WriteLine($"开始转换文本文件: {inputPath}");

            // 读取文本内容
            var content = await File.ReadAllTextAsync(inputPath, _encoding, cancellationToken);
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // 计算页面布局参数
            var contentWidth = _pageLayout.Width - 2 * _margin;
            var contentHeight = _pageLayout.Height - 2 * _margin;
            var lineHeight = _fontSize * _lineSpacing;
            var linesPerPage = (int)(contentHeight / lineHeight);

            if (linesPerPage <= 0)
            {
                throw new GeneralConvertException("页面太小，无法容纳任何文本行");
            }

            // 分页转换
            var totalLines = lines.Length;
            var currentPageLines = 0;
            Graphics.OFDPageGraphics? currentPage = null;

            for (int i = 0; i < totalLines; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 创建新页面
                if (currentPage == null || currentPageLines >= linesPerPage)
                {
                    currentPage = CreateNewPage();
                    currentPageLines = 0;
                }

                // 绘制文本行
                var line = lines[i];
                var y = _margin + currentPageLines * lineHeight + _fontSize; // 基线位置
                
                if (!string.IsNullOrEmpty(line))
                {
                    // 处理长行换行
                    var wrappedLines = WrapText(line, contentWidth);
                    foreach (var wrappedLine in wrappedLines)
                    {
                        if (currentPageLines >= linesPerPage)
                        {
                            // 需要新页面
                            currentPage = CreateNewPage();
                            currentPageLines = 0;
                            y = _margin + _fontSize;
                        }

                        currentPage.DrawText(wrappedLine, _margin, y, _fontSize);
                        y += lineHeight;
                        currentPageLines++;
                    }
                }
                else
                {
                    // 空行
                    currentPageLines++;
                }
            }

            // 完成转换
            await FinalizeConversion();
            
            Console.WriteLine($"文本转换完成，共 {GetPageCount(totalLines, linesPerPage)} 页");
        }
        catch (Exception ex)
        {
            throw new GeneralConvertException($"文本转换失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 文本换行处理
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <param name="maxWidth">最大宽度</param>
    /// <returns>换行后的文本行</returns>
    private List<string> WrapText(string text, double maxWidth)
    {
        var result = new List<string>();
        
        if (string.IsNullOrEmpty(text))
        {
            result.Add(string.Empty);
            return result;
        }

        // 估算每个字符的平均宽度（简化处理）
        var avgCharWidth = _fontSize * 0.6; // 大致估算
        var maxCharsPerLine = (int)(maxWidth / avgCharWidth);

        if (maxCharsPerLine <= 0)
        {
            result.Add(text);
            return result;
        }

        // 按字符数简单分行
        for (int i = 0; i < text.Length; i += maxCharsPerLine)
        {
            var length = Math.Min(maxCharsPerLine, text.Length - i);
            var line = text.Substring(i, length);
            result.Add(line);
        }

        return result;
    }

    /// <summary>
    /// 计算页面数量
    /// </summary>
    /// <param name="totalLines">总行数</param>
    /// <param name="linesPerPage">每页行数</param>
    /// <returns>页面数量</returns>
    private int GetPageCount(int totalLines, int linesPerPage)
    {
        return (totalLines + linesPerPage - 1) / linesPerPage;
    }
}