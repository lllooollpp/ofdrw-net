using HtmlAgilityPack;
using AngleSharp;
using AngleSharp.Html.Dom;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Layout;
using OfdrwNet.Layout.Element;
using Microsoft.Extensions.Logging;
using System.Text;

namespace OfdrwNet.WinFormsDemo.Converters;

/// <summary>
/// HTML文档转OFD转换器
/// 支持将HTML文件转换为OFD格式
/// </summary>
public class Html2OfdConverter : IDisposable
{
    private readonly ILogger<Html2OfdConverter> _logger;
    private bool _disposed = false;

    /// <summary>
    /// 转换进度事件
    /// </summary>
    public event EventHandler<ConversionProgressEventArgs>? ProgressChanged;

    /// <summary>
    /// 转换完成事件
    /// </summary>
    public event EventHandler<ConversionCompletedEventArgs>? ConversionCompleted;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public Html2OfdConverter(ILogger<Html2OfdConverter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 异步转换HTML文档为OFD
    /// </summary>
    /// <param name="inputHtmlPath">输入HTML文件路径</param>
    /// <param name="outputOfdPath">输出OFD文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>转换任务</returns>
    public async Task<ConversionResult> ConvertAsync(string inputHtmlPath, string outputOfdPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(inputHtmlPath))
            throw new ArgumentException("输入文件路径不能为空", nameof(inputHtmlPath));
        
        if (string.IsNullOrEmpty(outputOfdPath))
            throw new ArgumentException("输出文件路径不能为空", nameof(outputOfdPath));

        if (!File.Exists(inputHtmlPath))
            throw new FileNotFoundException("输入文件不存在", inputHtmlPath);

        try
        {
            _logger.LogInformation("开始转换HTML文档: {InputPath} -> {OutputPath}", inputHtmlPath, outputOfdPath);
            
            OnProgressChanged(0, "正在读取HTML文件...");
            
            var result = await Task.Run(async () =>
            {
                return await ConvertHtmlToOfdInternal(inputHtmlPath, outputOfdPath, cancellationToken);
            }, cancellationToken);

            OnConversionCompleted(result);
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("HTML转换操作被取消");
            var cancelledResult = new ConversionResult { Success = false, ErrorMessage = "转换操作被用户取消" };
            OnConversionCompleted(cancelledResult);
            return cancelledResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTML转换过程中发生错误");
            var errorResult = new ConversionResult { Success = false, ErrorMessage = ex.Message };
            OnConversionCompleted(errorResult);
            return errorResult;
        }
    }

    /// <summary>
    /// 内部转换实现
    /// </summary>
    private async Task<ConversionResult> ConvertHtmlToOfdInternal(string inputPath, string outputPath, CancellationToken cancellationToken)
    {
        try
        {
            // 读取HTML文件
            var htmlContent = await File.ReadAllTextAsync(inputPath, Encoding.UTF8, cancellationToken);
            
            OnProgressChanged(20, "正在解析HTML结构...");
            
            // 使用HtmlAgilityPack解析HTML
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);
            
            OnProgressChanged(40, "正在提取内容...");
            
            // 创建OFD文档
            var ofdDoc = new OFDDoc(outputPath);
            ofdDoc.SetDefaultPageLayout(PageLayout.A4());
            
            // 处理HTML内容
            await ProcessHtmlContent(ofdDoc, htmlDoc, cancellationToken);
            
            OnProgressChanged(80, "正在生成OFD文件...");
            
            // 保存OFD文档
            await ofdDoc.CloseAsync();
            
            OnProgressChanged(100, "转换完成");
            
            return new ConversionResult 
            { 
                Success = true, 
                OutputPath = outputPath,
                InputFileSize = new FileInfo(inputPath).Length,
                OutputFileSize = new FileInfo(outputPath).Length
            };
        }
        catch (Exception ex)
        {
            return new ConversionResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// 处理HTML内容
    /// </summary>
    private async Task ProcessHtmlContent(OFDDoc ofdDoc, HtmlAgilityPack.HtmlDocument htmlDoc, CancellationToken cancellationToken)
    {
        // 获取body节点
        var bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body") ?? htmlDoc.DocumentNode;
        
        // 提取标题
        var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title");
        if (titleNode != null)
        {
            var titleParagraph = new OfdrwNet.Layout.Element.Paragraph(titleNode.InnerText)
            {
                FontSize = 6.35, // 18pt 转毫米
                FontName = "宋体",
                Color = "#000000",
                TextAlign = TextAlign.Center
            };
            ofdDoc.Add(titleParagraph);
        }

        // 处理段落
        var paragraphNodes = bodyNode.SelectNodes(".//p | .//h1 | .//h2 | .//h3 | .//h4 | .//h5 | .//h6 | .//div");
        if (paragraphNodes != null)
        {
            int totalNodes = paragraphNodes.Count;
            int processedNodes = 0;

            foreach (var node in paragraphNodes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var text = ExtractTextFromNode(node);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var fontSize = GetFontSizeFromNodeName(node.Name);
                    var paragraph = new OfdrwNet.Layout.Element.Paragraph(text)
                    {
                        FontSize = fontSize,
                        FontName = "宋体",
                        Color = "#000000",
                        TextAlign = GetTextAlignFromNode(node),
                        LineHeight = 1.2
                    };
                    ofdDoc.Add(paragraph);
                    
                    _logger.LogDebug("添加HTML段落: {Text} (标签: {Tag}, 字体大小: {FontSize})", 
                        text.Substring(0, Math.Min(50, text.Length)), 
                        node.Name, 
                        fontSize);
                }
                
                processedNodes++;
                int progress = 40 + (int)((double)processedNodes / totalNodes * 30);
                OnProgressChanged(progress, $"正在处理内容 {processedNodes}/{totalNodes}...");
            }
        }
    }

    /// <summary>
    /// 从HTML节点提取纯文本
    /// </summary>
    private string ExtractTextFromNode(HtmlNode node)
    {
        return node.InnerText?.Trim().Replace("\n", " ").Replace("\r", "") ?? string.Empty;
    }

    /// <summary>
    /// 根据HTML标签名获取字体大小(毫米)
    /// </summary>
    private double GetFontSizeFromNodeName(string nodeName)
    {
        return nodeName.ToLowerInvariant() switch
        {
            "h1" => 8.47, // 24pt
            "h2" => 7.06, // 20pt
            "h3" => 5.64, // 16pt
            "h4" => 4.94, // 14pt
            "h5" => 4.23, // 12pt
            "h6" => 3.53, // 10pt
            _ => 4.23      // 12pt
        };
    }
    
    /// <summary>
    /// 根据HTML节点获取文本对齐方式
    /// </summary>
    private TextAlign GetTextAlignFromNode(HtmlNode node)
    {
        var style = node.GetAttributeValue("style", "");
        var align = node.GetAttributeValue("align", "");
        
        if (style.Contains("text-align: center") || align.Equals("center", StringComparison.OrdinalIgnoreCase))
            return TextAlign.Center;
        if (style.Contains("text-align: right") || align.Equals("right", StringComparison.OrdinalIgnoreCase))
            return TextAlign.Right;
        if (style.Contains("text-align: justify") || align.Equals("justify", StringComparison.OrdinalIgnoreCase))
            return TextAlign.Justify;
            
        return TextAlign.Left;
    }

    /// <summary>
    /// 获取支持的文件扩展名
    /// </summary>
    public static string[] SupportedExtensions => new[] { ".html", ".htm" };

    /// <summary>
    /// 验证输入文件是否支持
    /// </summary>
    public static bool IsSupported(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Any(ext => ext == extension);
    }

    /// <summary>
    /// 触发进度变化事件
    /// </summary>
    protected virtual void OnProgressChanged(int percentage, string message)
    {
        ProgressChanged?.Invoke(this, new ConversionProgressEventArgs { Percentage = percentage, Message = message });
    }

    /// <summary>
    /// 触发转换完成事件
    /// </summary>
    protected virtual void OnConversionCompleted(ConversionResult result)
    {
        ConversionCompleted?.Invoke(this, new ConversionCompletedEventArgs { Result = result });
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}