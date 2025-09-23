using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions;
using OfdrwNet;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OfdrwNet.Converter;

/// <summary>
/// 基于 Playwright + PDF.js 的 PDF 到 OFD 转换器
/// 使用浏览器技术栈高精度提取 PDF 内容（图片 + 文本结构化数据）
/// </summary>
public class PlaywrightPdfConverter : IDisposable
{
    private readonly ILogger? _logger;
    private IBrowser? _browser;
    private IPage? _page;
    private bool _disposed = false;

    public PlaywrightPdfConverter(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 初始化 Playwright 浏览器环境
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger?.LogInformation("[PlaywrightPdfConverter] 初始化 Playwright 浏览器环境");
        
        var playwright = await Playwright.CreateAsync();
        _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-dev-shm-usage", "--disable-gpu" }
        });
        
        _page = await _browser.NewPageAsync();
        _logger?.LogInformation("[PlaywrightPdfConverter] 浏览器环境初始化完成");
    }

    /// <summary>
    /// 转换 PDF 文件到 OFD 格式
    /// </summary>
    /// <param name="pdfPath">输入 PDF 文件路径</param>
    /// <param name="ofdOutputPath">输出 OFD 文件路径</param>
    /// <param name="options">转换选项</param>
    public async Task ConvertPdfToOfdAsync(string pdfPath, string ofdOutputPath, PlaywrightConvertOptions? options = null)
    {
        options ??= new PlaywrightConvertOptions();
        
        if (_page == null)
            throw new InvalidOperationException("请先调用 InitializeAsync() 初始化浏览器环境");

        _logger?.LogInformation("[PlaywrightPdfConverter] 开始转换 PDF: {PdfPath} -> {OfdPath}", pdfPath, ofdOutputPath);

        // 1. 加载 PDF.js 处理页面
        await LoadPdfJsPageAsync();

        // 2. 加载 PDF 文件并提取数据
        var extractedData = await ExtractPdfDataAsync(pdfPath, options);

        // 3. 转换为 OFD 格式
        await ConvertToOfdAsync(extractedData, ofdOutputPath, options);

        _logger?.LogInformation("[PlaywrightPdfConverter] PDF 转换完成");
    }

    /// <summary>
    /// 加载包含 PDF.js 的 HTML 页面
    /// </summary>
    private async Task LoadPdfJsPageAsync()
    {
        if (_page == null) return;

        var htmlContent = GetPdfJsHtmlTemplate();
        await _page.SetContentAsync(htmlContent);
        
        // 等待 PDF.js 库加载完成
        await _page.WaitForFunctionAsync("() => window.pdfjsLib !== undefined");
        _logger?.LogDebug("[PlaywrightPdfConverter] PDF.js 库加载完成");
    }

    /// <summary>
    /// 提取 PDF 数据（页面图片 + 文本结构化信息）
    /// </summary>
    private async Task<PdfExtractedData> ExtractPdfDataAsync(string pdfPath, PlaywrightConvertOptions options)
    {
        if (_page == null) throw new InvalidOperationException("页面未初始化");

        // 读取 PDF 文件为 ArrayBuffer
        var pdfBytes = await File.ReadAllBytesAsync(pdfPath);
        var base64Pdf = Convert.ToBase64String(pdfBytes);

        _logger?.LogDebug("[PlaywrightPdfConverter] 开始在浏览器中处理 PDF，大小: {Size} bytes", pdfBytes.Length);

        // 执行 PDF 数据提取脚本
        var result = await _page.EvaluateAsync<string>(@"
            async (params) => {
                const {base64Pdf, options} = params;
                const pdfData = Uint8Array.from(atob(base64Pdf), c => c.charCodeAt(0));
                const pdf = await pdfjsLib.getDocument({data: pdfData}).promise;
                
                const pages = [];
                const maxPages = Math.min(pdf.numPages, 3); // 只处理前3页进行调试
                for (let pageNum = 1; pageNum <= maxPages; pageNum++) {
                    const page = await pdf.getPage(pageNum);
                    const viewport = page.getViewport({scale: options.renderScale});
                    
                    // 渲染页面为图片
                    const canvas = document.createElement('canvas');
                    const context = canvas.getContext('2d');
                    canvas.height = viewport.height;
                    canvas.width = viewport.width;
                    
                    await page.render({
                        canvasContext: context,
                        viewport: viewport
                    }).promise;
                    
                    const imageData = canvas.toDataURL('image/png');
                    
                    // 提取文本内容
                    const textContent = await page.getTextContent();
                    const texts = textContent.items.map(item => ({
                        text: item.str,
                        x: item.transform[4],
                        y: item.transform[5],
                        width: item.width,
                        height: item.height,
                        fontName: item.fontName,
                        fontSize: Math.abs(item.transform[0]),
                        transform: item.transform
                    }));
                    
                    pages.push({
                        pageNumber: pageNum,
                        width: viewport.width,
                        height: viewport.height,
                        imageData: imageData,
                        texts: texts
                    });
                }
                
                return JSON.stringify({
                    pageCount: maxPages,
                    pages: pages
                });
            }
        ", new { base64Pdf, options = new { renderScale = options.RenderScale } });

        var extractedData = JsonSerializer.Deserialize<PdfExtractedData>(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger?.LogInformation("[PlaywrightPdfConverter] PDF 数据提取完成，共 {PageCount} 页", extractedData?.PageCount ?? 0);
        return extractedData ?? new PdfExtractedData();
    }

    /// <summary>
    /// 将提取的数据转换为 OFD 格式
    /// </summary>
    private async Task ConvertToOfdAsync(PdfExtractedData data, string ofdOutputPath, PlaywrightConvertOptions options)
    {
        var outputDir = Path.GetDirectoryName(ofdOutputPath) ?? Directory.GetCurrentDirectory();
        var ofdDir = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(ofdOutputPath) + "_ofd");
        
        _logger?.LogDebug("[PlaywrightPdfConverter] 创建 OFD 文档，输出目录: {OfdDir}", ofdDir);

        using var ofdWriter = new OfdWriter(ofdOutputPath, _logger);

        foreach (var page in data.Pages)
        {
            _logger?.LogDebug("[PlaywrightPdfConverter] 处理第 {PageNum} 页，文本元素数量: {TextCount}", page.PageNumber, page.Texts.Count);

            // 设置页面尺寸（PDF.js 使用 96 DPI，转换为 mm）
            var pageWidthMm = page.Width * 25.4 / 96.0;
            var pageHeightMm = page.Height * 25.4 / 96.0;
            
            _logger?.LogDebug("[PlaywrightPdfConverter] 页面尺寸: {Width}x{Height} px -> {WidthMm:F2}x{HeightMm:F2} mm", 
                page.Width, page.Height, pageWidthMm, pageHeightMm);

            // 如果配置为渲染页面图片，添加背景图片
            if (options.RenderPageAsImage)
            {
                var imageBytes = Convert.FromBase64String(page.ImageData.Split(',')[1]);
                var ofdImage = new OfdImage
                {
                    Page = page.PageNumber,
                    X = 0,
                    Y = 0,
                    Width = (float)pageWidthMm,
                    Height = (float)pageHeightMm,
                    ImageData = imageBytes,
                    Format = "png"
                };
                
                ofdWriter.AddImage(ofdImage);
                _logger?.LogDebug("[PlaywrightPdfConverter] 添加页面背景图片，大小: {Size} bytes", imageBytes.Length);
            }

            // 添加文本元素
            var textCount = 0;
            foreach (var text in page.Texts.Where(t => !string.IsNullOrWhiteSpace(t.Text)))
            {
                // 坐标转换：PDF.js 坐标 -> OFD 坐标（mm）
                var xMm = text.X * 25.4 / 96.0;
                var yMm = (page.Height - text.Y - text.Height) * 25.4 / 96.0;
                var widthMm = text.Width * 25.4 / 96.0;
                var heightMm = text.Height * 25.4 / 96.0;
                var fontSizeMm = text.FontSize * 25.4 / 96.0;
                
                // 确保字体大小不会太小（最小3mm）
                if (fontSizeMm < 3.0)
                {
                    fontSizeMm = 3.0;
                }

                var ofdText = new OfdText
                {
                    Page = page.PageNumber,
                    Text = text.Text,
                    X = (float)xMm,
                    Y = (float)yMm,
                    Width = (float)widthMm,
                    Height = (float)heightMm,
                    FontFamily = NormalizeFontName(text.FontName),
                    FontSize = (float)fontSizeMm,
                    CTM = ConvertTransformMatrix(text.Transform, 25.4 / 96.0)
                };

                ofdWriter.AddText(ofdText);
                textCount++;
                
                if (textCount <= 3) // 只记录前3个文本元素的详细信息
                {
                    _logger?.LogDebug("[PlaywrightPdfConverter] 添加文本: '{Text}' 位置:({X:F2},{Y:F2}) 大小:{FontSize:F2}mm 字体:{FontFamily}", 
                        text.Text.Length > 20 ? text.Text.Substring(0, 20) + "..." : text.Text,
                        xMm, yMm, fontSizeMm, NormalizeFontName(text.FontName));
                }
                
                // 对第一页进行更详细的调试
                if (page.PageNumber == 1 && textCount <= 10)
                {
                    _logger?.LogInformation("[DEBUG] 第1页文本 #{Index}: '{Text}' PDF坐标:({PdfX},{PdfY}) OFD坐标:({OfdX:F2},{OfdY:F2}) 字体:{Font} 尺寸:{Size:F2}mm 原始字号:{OrigSize}", 
                        textCount, 
                        text.Text.Length > 30 ? text.Text.Substring(0, 30) + "..." : text.Text,
                        text.X, text.Y, xMm, yMm, 
                        NormalizeFontName(text.FontName), fontSizeMm, text.FontSize);
                }
            }
            
            _logger?.LogInformation("[PlaywrightPdfConverter] 第 {PageNum} 页添加了 {TextCount} 个文本元素", page.PageNumber, textCount);
        }

        await ofdWriter.CloseAsync();

        _logger?.LogInformation("[PlaywrightPdfConverter] OFD 文件已生成: {OfdPath}", ofdOutputPath);
    }

    /// <summary>
    /// 转换 PDF.js 的变换矩阵为 OFD 格式
    /// </summary>
    private double[]? ConvertTransformMatrix(double[] pdfTransform, double scale)
    {
        if (pdfTransform == null || pdfTransform.Length != 6) return null;
        
        return new double[]
        {
            pdfTransform[0] * scale,  // a
            pdfTransform[1] * scale,  // b  
            pdfTransform[2] * scale,  // c
            pdfTransform[3] * scale,  // d
            pdfTransform[4] * scale,  // e (tx)
            pdfTransform[5] * scale   // f (ty)
        };
    }

    /// <summary>
    /// 规范化字体名称
    /// </summary>
    private string NormalizeFontName(string fontName)
    {
        if (string.IsNullOrEmpty(fontName)) return "SimSun";
        
        // 移除常见的字体前缀和后缀
        fontName = fontName.Replace("g_d0_f", "").Replace("+", "");
        
        // 常见字体映射
        var fontMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Arial"] = "Arial",
            ["SimSun"] = "SimSun",
            ["SimHei"] = "SimHei",
            ["Times"] = "Times New Roman",
            ["Helvetica"] = "Arial"
        };

        foreach (var mapping in fontMap)
        {
            if (fontName.Contains(mapping.Key, StringComparison.OrdinalIgnoreCase))
                return mapping.Value;
        }

        return "SimSun"; // 默认字体
    }

    /// <summary>
    /// 获取包含 PDF.js 的 HTML 模板
    /// </summary>
    private string GetPdfJsHtmlTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>PDF.js Converter</title>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.min.js""></script>
</head>
<body>
    <script>
        // 配置 PDF.js
        pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js';
        
        console.log('PDF.js loaded successfully');
    </script>
</body>
</html>";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _page?.CloseAsync().GetAwaiter().GetResult();
            _browser?.CloseAsync().GetAwaiter().GetResult();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Playwright 转换选项
/// </summary>
public class PlaywrightConvertOptions
{
    /// <summary>
    /// 渲染缩放比例（默认 2.0 以获得高分辨率）
    /// </summary>
    public double RenderScale { get; set; } = 2.0;

    /// <summary>
    /// 是否将页面渲染为图片（作为背景），默认 false
    /// </summary>
    public bool RenderPageAsImage { get; set; } = false;

    /// <summary>
    /// 是否提取文本，默认 true
    /// </summary>
    public bool ExtractText { get; set; } = true;
}

/// <summary>
/// PDF 提取的数据结构
/// </summary>
public class PdfExtractedData
{
    public int PageCount { get; set; }
    public List<PageData> Pages { get; set; } = new();
}

public class PageData
{
    public int PageNumber { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string ImageData { get; set; } = "";
    public List<TextItem> Texts { get; set; } = new();
}

public class TextItem
{
    public string Text { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string FontName { get; set; } = "";
    public double FontSize { get; set; }
    public double[] Transform { get; set; } = Array.Empty<double>();
}