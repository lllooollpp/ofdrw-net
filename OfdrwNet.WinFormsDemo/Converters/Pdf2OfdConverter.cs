using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Layout;
using OfdrwNet.Layout.Element;
using Microsoft.Extensions.Logging;
using System.Text;

namespace OfdrwNet.WinFormsDemo.Converters;

/// <summary>
/// PDF文档转OFD转换器
/// 支持将PDF文件转换为OFD格式
/// </summary>
public class Pdf2OfdConverter : IDisposable
{
    private readonly ILogger<Pdf2OfdConverter> _logger;
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
    public Pdf2OfdConverter(ILogger<Pdf2OfdConverter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 异步转换PDF文档为OFD
    /// </summary>
    /// <param name="inputPdfPath">输入PDF文件路径</param>
    /// <param name="outputOfdPath">输出OFD文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>转换任务</returns>
    public async Task<ConversionResult> ConvertAsync(string inputPdfPath, string outputOfdPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(inputPdfPath))
            throw new ArgumentException("输入文件路径不能为空", nameof(inputPdfPath));
        
        if (string.IsNullOrEmpty(outputOfdPath))
            throw new ArgumentException("输出文件路径不能为空", nameof(outputOfdPath));

        if (!File.Exists(inputPdfPath))
            throw new FileNotFoundException("输入文件不存在", inputPdfPath);

        try
        {
            _logger.LogInformation("开始转换PDF文档: {InputPath} -> {OutputPath}", inputPdfPath, outputOfdPath);
            
            OnProgressChanged(0, "正在读取PDF文件...");
            
            var result = await Task.Run(async () =>
            {
                return await ConvertPdfToOfdInternal(inputPdfPath, outputOfdPath, cancellationToken);
            }, cancellationToken);

            OnConversionCompleted(result);
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("PDF转换操作被取消");
            var cancelledResult = new ConversionResult { Success = false, ErrorMessage = "转换操作被用户取消" };
            OnConversionCompleted(cancelledResult);
            return cancelledResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF转换过程中发生错误");
            var errorResult = new ConversionResult { Success = false, ErrorMessage = ex.Message };
            OnConversionCompleted(errorResult);
            return errorResult;
        }
    }

    /// <summary>
    /// 内部转换实现
    /// </summary>
    private async Task<ConversionResult> ConvertPdfToOfdInternal(string inputPath, string outputPath, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("开始解析PDF文件: {InputPath}", inputPath);
            
            using var pdfReader = new PdfReader(inputPath);
            int pageCount = pdfReader.NumberOfPages;
            
            _logger.LogInformation("PDF文件解析成功，共{PageCount}页", pageCount);
            
            OnProgressChanged(20, $"PDF共有 {pageCount} 页，正在解析...");
            
            // 创建OFD文档
            var ofdDoc = new OFDDoc(outputPath);
            ofdDoc.SetDefaultPageLayout(PageLayout.A4());
            
            _logger.LogInformation("创建OFD文档成功，输出路径: {OutputPath}", outputPath);
            
            // 添加文档头部信息
            var headerParagraph = new OfdrwNet.Layout.Element.Paragraph($"PDF转换文档 - 原文件: {System.IO.Path.GetFileName(inputPath)}")
            {
                FontSize = 5.0,
                FontName = "宋体",
                Color = "#0066cc",
                TextAlign = TextAlign.Center,
                LineHeight = 1.5
            };
            ofdDoc.Add(headerParagraph);
            
            var spaceParagraph = new OfdrwNet.Layout.Element.Paragraph("\n")
            {
                FontSize = 2.0
            };
            ofdDoc.Add(spaceParagraph);
            
            int totalParagraphsAdded = 2; // 已添加的头部信息
            
            // 逐页处理PDF内容
            for (int pageNum = 1; pageNum <= pageCount; pageNum++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                OnProgressChanged(20 + (int)((double)(pageNum - 1) / pageCount * 60), 
                    $"正在处理第 {pageNum}/{pageCount} 页...");
                
                _logger.LogInformation("开始处理PDF第{PageNumber}页", pageNum);
                
                await ProcessPdfPage(ofdDoc, pdfReader, pageNum);
                
                _logger.LogInformation("完成处理PDF第{PageNumber}页", pageNum);
            }
            
            _logger.LogInformation("所有PDF页面处理完成，开始生成OFD文件");
            
            OnProgressChanged(90, "正在生成OFD文件...");
            
            // 保存OFD文档
            await ofdDoc.CloseAsync();
            
            OnProgressChanged(100, "转换完成");
            
            var result = new ConversionResult 
            { 
                Success = true, 
                OutputPath = outputPath,
                InputFileSize = new FileInfo(inputPath).Length,
                OutputFileSize = new FileInfo(outputPath).Length,
                PageCount = pageCount
            };
            
            _logger.LogInformation("PDF转换完成：输入文件{InputSize}MB → 输出OFD{OutputSize}KB", 
                result.InputFileSize / (1024.0 * 1024.0), 
                result.OutputFileSize / 1024.0);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF转换过程中发生错误");
            return new ConversionResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// 处理PDF页面
    /// </summary>
    private async Task ProcessPdfPage(OFDDoc ofdDoc, PdfReader pdfReader, int pageNumber)
    {
        int linesAdded = 0;
        
        try
        {
            // 提取页面文本
            var pageText = PdfTextExtractor.GetTextFromPage(pdfReader, pageNumber);
            
            _logger.LogInformation("PDF第{Page}页原始文本长度: {Length}", pageNumber, pageText?.Length ?? 0);
            
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                // 如果是新页面（除了第一页），添加分页标记
                if (pageNumber > 1)
                {
                    var pageBreakParagraph = new OfdrwNet.Layout.Element.Paragraph($"\n=== 第 {pageNumber} 页 ===\n")
                    {
                        FontSize = 4.0,
                        FontName = "宋体",
                        Color = "#0066cc",
                        TextAlign = TextAlign.Center,
                        LineHeight = 1.0
                    };
                    ofdDoc.Add(pageBreakParagraph);
                    linesAdded++;
                }
                
                // 按行分割文本
                var lines = pageText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                _logger.LogInformation("PDF第{Page}页分割后行数: {LineCount}", pageNumber, lines.Length);
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        var paragraph = new OfdrwNet.Layout.Element.Paragraph(trimmedLine)
                        {
                            FontSize = 3.5, // 默认字体大小
                            FontName = "宋体",
                            Color = "#000000",
                            TextAlign = TextAlign.Left,
                            LineHeight = 1.2
                        };
                        ofdDoc.Add(paragraph);
                        linesAdded++;
                        
                        _logger.LogDebug("添加PDF文本行: {Text} (页面: {Page})", 
                            trimmedLine.Substring(0, Math.Min(50, trimmedLine.Length)), 
                            pageNumber);
                    }
                }
                
                // 添加一个空行分隔页面内容
                var spacerParagraph = new OfdrwNet.Layout.Element.Paragraph("\n")
                {
                    FontSize = 2.0,
                    FontName = "宋体",
                    Color = "#000000"
                };
                ofdDoc.Add(spacerParagraph);
                linesAdded++;
            }
            else
            {
                // 如果页面没有文本，添加提示信息
                var noTextParagraph = new OfdrwNet.Layout.Element.Paragraph($"[第{pageNumber}页：此页面可能包含图像或其他非文本内容]")
                {
                    FontSize = 3.0,
                    FontName = "宋体",
                    Color = "#888888",
                    TextAlign = TextAlign.Center
                };
                ofdDoc.Add(noTextParagraph);
                linesAdded++;
            }
            
            _logger.LogInformation("PDF第{Page}页处理完成，总共添加了{Count}个段落", pageNumber, linesAdded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理PDF第 {PageNumber} 页时发生错误", pageNumber);
            
            // 添加错误信息页面
            var errorParagraph = new OfdrwNet.Layout.Element.Paragraph($"[第{pageNumber}页内容解析失败: {ex.Message}]")
            {
                FontSize = 3.0,
                FontName = "宋体",
                Color = "#ff0000",
                TextAlign = TextAlign.Left
            };
            ofdDoc.Add(errorParagraph);
            linesAdded++;
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 获取PDF文档信息
    /// </summary>
    public static PdfDocumentInfo GetDocumentInfo(string pdfPath)
    {
        try
        {
            using var pdfReader = new PdfReader(pdfPath);
            var info = pdfReader.Info;
            
            return new PdfDocumentInfo
            {
                PageCount = pdfReader.NumberOfPages,
                Title = info.ContainsKey("Title") ? info["Title"] : null,
                Author = info.ContainsKey("Author") ? info["Author"] : null,
                Subject = info.ContainsKey("Subject") ? info["Subject"] : null,
                Creator = info.ContainsKey("Creator") ? info["Creator"] : null,
                Producer = info.ContainsKey("Producer") ? info["Producer"] : null
            };
        }
        catch (Exception)
        {
            return new PdfDocumentInfo { PageCount = 0 };
        }
    }

    /// <summary>
    /// 获取支持的文件扩展名
    /// </summary>
    public static string[] SupportedExtensions => new[] { ".pdf" };

    /// <summary>
    /// 验证输入文件是否支持
    /// </summary>
    public static bool IsSupported(string filePath)
    {
        var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Any(ext => ext == extension);
    }

    /// <summary>
    /// 验证PDF文件是否可读
    /// </summary>
    public static bool ValidatePdfFile(string pdfPath)
    {
        try
        {
            using var pdfReader = new PdfReader(pdfPath);
            return pdfReader.NumberOfPages > 0;
        }
        catch
        {
            return false;
        }
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

/// <summary>
/// PDF文档信息
/// </summary>
public class PdfDocumentInfo
{
    /// <summary>
    /// 页面数量
    /// </summary>
    public int PageCount { get; set; }
    
    /// <summary>
    /// 文档标题
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// 文档作者
    /// </summary>
    public string? Author { get; set; }
    
    /// <summary>
    /// 文档主题
    /// </summary>
    public string? Subject { get; set; }
    
    /// <summary>
    /// 创建者
    /// </summary>
    public string? Creator { get; set; }
    
    /// <summary>
    /// 生产者
    /// </summary>
    public string? Producer { get; set; }
}