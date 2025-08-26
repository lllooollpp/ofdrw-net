using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Layout;
using OfdrwNet.Layout.Element;
using Microsoft.Extensions.Logging;

namespace OfdrwNet.WinFormsDemo.Converters;

/// <summary>
/// Word文档转OFD转换器
/// 支持将.docx格式的Word文档转换为OFD格式
/// </summary>
public class Word2OfdConverter : IDisposable
{
    private readonly ILogger<Word2OfdConverter> _logger;
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
    public Word2OfdConverter(ILogger<Word2OfdConverter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 异步转换Word文档为OFD
    /// </summary>
    /// <param name="inputWordPath">输入Word文件路径</param>
    /// <param name="outputOfdPath">输出OFD文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>转换任务</returns>
    public async Task<ConversionResult> ConvertAsync(string inputWordPath, string outputOfdPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(inputWordPath))
            throw new ArgumentException("输入文件路径不能为空", nameof(inputWordPath));
        
        if (string.IsNullOrEmpty(outputOfdPath))
            throw new ArgumentException("输出文件路径不能为空", nameof(outputOfdPath));

        if (!File.Exists(inputWordPath))
            throw new FileNotFoundException("输入文件不存在", inputWordPath);

        try
        {
            _logger.LogInformation("开始转换Word文档: {InputPath} -> {OutputPath}", inputWordPath, outputOfdPath);
            
            OnProgressChanged(0, "正在分析Word文档结构...");
            
            var result = await Task.Run(async () =>
            {
                return await ConvertWordToOfdInternal(inputWordPath, outputOfdPath, cancellationToken);
            }, cancellationToken);

            OnConversionCompleted(result);
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Word转换操作被取消");
            var cancelledResult = new ConversionResult { Success = false, ErrorMessage = "转换操作被用户取消" };
            OnConversionCompleted(cancelledResult);
            return cancelledResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Word转换过程中发生错误");
            var errorResult = new ConversionResult { Success = false, ErrorMessage = ex.Message };
            OnConversionCompleted(errorResult);
            return errorResult;
        }
    }

    /// <summary>
    /// 内部转换实现
    /// </summary>
    private async Task<ConversionResult> ConvertWordToOfdInternal(string inputPath, string outputPath, CancellationToken cancellationToken)
    {
        try
        {
            using var wordDocument = WordprocessingDocument.Open(inputPath, false);
            var body = wordDocument.MainDocumentPart?.Document.Body;
            
            if (body == null)
            {
                return new ConversionResult { Success = false, ErrorMessage = "无法读取Word文档内容" };
            }

            OnProgressChanged(20, "正在解析文档内容...");
            
            // 创建OFD文档
            var ofdDoc = new OFDDoc(outputPath);
            ofdDoc.SetDefaultPageLayout(PageLayout.A4());
            
            OnProgressChanged(40, "正在转换文本内容...");
            
            // 处理段落
            var paragraphs = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();
            int paragraphCount = paragraphs.Count();
            int processedCount = 0;

            foreach (var paragraph in paragraphs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                await ProcessParagraph(ofdDoc, paragraph);
                processedCount++;
                
                int progress = 40 + (int)((double)processedCount / paragraphCount * 40);
                OnProgressChanged(progress, $"正在处理段落 {processedCount}/{paragraphCount}...");
            }

            OnProgressChanged(90, "正在生成OFD文件...");
            
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
    /// 处理Word段落
    /// </summary>
    private async Task ProcessParagraph(OFDDoc ofdDoc, DocumentFormat.OpenXml.Wordprocessing.Paragraph wordParagraph)
    {
        var runs = wordParagraph.Elements<Run>();
        var paragraphText = string.Join("", runs.Select(run => run.InnerText));
        
        if (!string.IsNullOrWhiteSpace(paragraphText))
        {
            // 创建段落样式
            var paragraph = new OfdrwNet.Layout.Element.Paragraph(paragraphText)
            {
                FontSize = 3.5, // 默认字体大小 (毫米)
                FontName = "宋体",
                Color = "#000000",
                TextAlign = TextAlign.Left,
                LineHeight = 1.2
            };
            
            // 尝试解析Word段落的样式
            var paragraphProperties = wordParagraph.ParagraphProperties;
            if (paragraphProperties != null)
            {
                // 解析字体大小
                var runProperties = runs.FirstOrDefault()?.RunProperties;
                if (runProperties?.FontSize?.Val != null)
                {
                    // Word字体大小单位是半点(half-points)，转换为毫米
                    var fontSizeValue = runProperties.FontSize.Val.Value;
                    var wordFontSize = double.TryParse(fontSizeValue.ToString(), out var parsedValue) ? parsedValue / 2.0 : 12.0;
                    paragraph.FontSize = wordFontSize * 0.352778; // 点转毫米
                }
                
                // 解析字体名称
                if (runProperties?.RunFonts?.Ascii?.Value != null)
                {
                    paragraph.FontName = runProperties.RunFonts.Ascii.Value;
                }
                
                // 解析对齐方式
                if (paragraphProperties.Justification?.Val?.Value != null)
                {
                    var justificationValue = paragraphProperties.Justification.Val.Value;
                    if (justificationValue == DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Center)
                        paragraph.TextAlign = TextAlign.Center;
                    else if (justificationValue == DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Right)
                        paragraph.TextAlign = TextAlign.Right;
                    else if (justificationValue == DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Both)
                        paragraph.TextAlign = TextAlign.Justify;
                    else
                        paragraph.TextAlign = TextAlign.Left;
                }
            }
            
            // 添加段落到OFD文档
            ofdDoc.Add(paragraph);
            
            _logger.LogDebug("添加段落: {Text} (长度: {Length}, 字体: {Font}, 大小: {Size})", 
                paragraphText.Substring(0, Math.Min(50, paragraphText.Length)), 
                paragraphText.Length, 
                paragraph.FontName, 
                paragraph.FontSize);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 获取支持的文件扩展名
    /// </summary>
    public static string[] SupportedExtensions => new[] { ".docx" };

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