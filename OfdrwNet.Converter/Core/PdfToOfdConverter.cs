using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using iText.Kernel.Pdf;
using OfdrwNet.Converter.Options;
using OfdrwNet.Converter.Refactor;
using OfdrwNet;
using OfdrwNet.Abstractions;
using OfdrwNet.Layout;
using OfdrwNet.Core.BasicStructure.Ofd.DocInfo;

namespace OfdrwNet.Converter.Core;

/// <summary>
/// PDF 到 OFD 转换器
/// 负责处理所有 PDF 到 OFD 格式的转换操作
/// </summary>
public class PdfToOfdConverter
{
    public const double Pt2Mm = 25.4 / 72.0; // 点到毫米转换常数
    private static readonly System.Text.RegularExpressions.Regex _subsetPrefixRegex = new System.Text.RegularExpressions.Regex(@"^[A-Z]{6}\+");

    #region 同步转换方法

    /// <summary>
    /// 同步将 PDF 转换为 OFD
    /// </summary>
    public void ConvertToOfd(string pdfPath, string ofdOutputDir, PdfToOfdOptions? options = null)
    {
        ConvertToOfdAsync(pdfPath, ofdOutputDir, options).GetAwaiter().GetResult();
    }



    #endregion

    #region 异步转换方法

    /// <summary>
    /// 异步将 PDF 转换为 OFD
    /// </summary>
    public async Task ConvertToOfdAsync(string pdfPath, string ofdOutputDir, PdfToOfdOptions? options = null)
    {
        options ??= new PdfToOfdOptions();
        ILogger? logger = GetOrCreateLogger(options);

        logger.LogInformation("[PDF2OFD] 开始转换 PDF -> OFD 输入={Pdf} 输出={Ofd}", pdfPath, ofdOutputDir);

        // T073: 内存检查初始化 (如果启用)
        await CheckMemoryIfEnabled(options, logger);

        if (!File.Exists(pdfPath))
        {
            logger.LogError("[PDF2OFD] PDF文件不存在: {Pdf}", pdfPath);
            throw new FileNotFoundException("PDF文件不存在", pdfPath);
        }

        // 创建PdfReader，支持密码
        using var pdfReader = CreatePdfReader(pdfPath, options, logger);
        using var pdfDoc = new PdfDocument(pdfReader);

        // 预读取第一页尺寸，后续用于设置页面尺寸
        var firstPageSize = GetFirstPageSize(pdfDoc);

        // 1. 字体抽取（重构：委派 FontExtractor）
        var fontExtractor = new PdfFontExtractor();

        // 2. 创建 OFD 文档
        logger.LogInformation("[PDF2OFD] 创建 OfdWriter, 输出目录: {OfdOutputDir}", ofdOutputDir);
        using IOfdDocWriter ofd = new OfdWriter(ofdOutputDir, logger);

        ConfigureOfdWriter(ofd, options, logger);

        // 在创建 writer 之后执行字体提取，以便 FontExtractor 直接注册字体
        await fontExtractor.ExtractAsync(pdfDoc, ofd, options, logger, options.CancellationToken);

        // 根据PDF第一页实际尺寸动态设置页面布局(缺省A4时避免变形)
        ConfigurePageLayout(ofd, firstPageSize, logger);

        try
        {
            // 字体注册已经在 FontExtractor 中完成

            // 3. Orchestrator 统一调度剩余内容提取（字体已在前）
            // T073: 传递 options 以支持服务依赖注入
            var orchestrator = new PdfToOfdOrchestrator(options);
            await orchestrator.ExecuteAsync(pdfDoc, ofd, options, logger, options.CancellationToken);

            await ofd.CloseAsync().ConfigureAwait(false);
            logger.LogInformation("[PDF2OFD] OFD文档异步关闭完成");

            // T073: 转换后验证和报告生成 (如果启用)
            await ValidateAndReportIfEnabled(options, pdfPath, ofdOutputDir, logger);
        }
        finally
        {
            // 清理临时字体文件（与原逻辑相同的生命周期）
            fontExtractor.CleanupTempFiles(logger);
            logger.LogInformation("[PDF2OFD] 临时字体文件清理完毕(由 FontExtractor)");
        }
    }


    /// <summary>
    /// 新增：别名方法以兼容测试程序
    /// </summary>
    public Task ConvertPdfToOfdAsync(string pdfPath, string ofdOutputDir, PdfToOfdOptions? options = null)
    {
        return ConvertToOfdAsync(pdfPath, ofdOutputDir, options);
    }

    #endregion

    #region 内部辅助方法

    /// <summary>
    /// 获取或创建日志记录器
    /// </summary>
    private static ILogger GetOrCreateLogger(PdfToOfdOptions options)
    {
        ILogger? logger = options.Logger;
        if (logger == null)
        {
            // 如果未提供日志记录器,创建一个临时的
            var lf = LoggerFactory.Create(b =>
            {
                b.AddConsole();
                b.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
            });
            logger = lf.CreateLogger("PDF2OFD");
            logger.LogInformation("[PDF2OFD] 未提供外部Logger,已启用内部临时Logger");
        }
        return logger;
    }

    /// <summary>
    /// 内存检查（如果启用）
    /// </summary>
    private static async Task CheckMemoryIfEnabled(PdfToOfdOptions options, ILogger logger)
    {
        var memoryGuard = options.MemoryGuard;
        if (memoryGuard != null)
        {
            logger.LogInformation("[PDF2OFD] 内存监控已启用 警告阈值={Warning}MB 关键阈值={Critical}MB",
                options.MemoryWarningThresholdMB, options.MemoryCriticalThresholdMB);
            var snapshot = memoryGuard.CheckMemory();
            if (snapshot.Action == Domain.MemoryAction.Abort)
            {
                logger.LogError("[PDF2OFD] 内存不足,转换中止 当前使用={Current}MB", snapshot.AllocatedMB);
                throw new OutOfMemoryException("转换前内存检查失败");
            }
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 创建 PDF 读取器
    /// </summary>
    private static PdfReader CreatePdfReader(string pdfPath, PdfToOfdOptions options, ILogger logger)
    {
        if (!string.IsNullOrEmpty(options.Password))
        {
            var reader = new PdfReader(pdfPath, new ReaderProperties().SetPassword(System.Text.Encoding.UTF8.GetBytes(options.Password)));
            logger.LogInformation("[PDF2OFD] 使用密码打开PDF文件");
            return reader;
        }
        else
        {
            return new PdfReader(pdfPath);
        }
    }

    /// <summary>
    /// 获取第一页尺寸
    /// </summary>
    private static iText.Kernel.Geom.Rectangle? GetFirstPageSize(PdfDocument pdfDoc)
    {
        if (pdfDoc.GetNumberOfPages() > 0)
        {
            return pdfDoc.GetPage(1).GetPageSize();
        }
        return null;
    }

    /// <summary>
    /// 配置 OFD 写入器
    /// </summary>
    private static void ConfigureOfdWriter(IOfdDocWriter ofd, PdfToOfdOptions options, ILogger logger)
    {
        if (ofd is OfdWriter writer)
        {
            var autoDocId = options.AutoGenerateDocId && !options.RemoveDocId;
            writer.SetAutoGenerateDocId(autoDocId);

            var shouldRemoveDocId = options.RemoveDocId || (!autoDocId && string.IsNullOrWhiteSpace(options.OverrideDocId));
            if (shouldRemoveDocId)
            {
                writer.ConfigureDocInfo(info => info.RemoveOfdElementsByNames("DocID"));
            }

            // 应用各种配置
            if (!string.IsNullOrWhiteSpace(options.OverrideDocId))
            {
                writer.ConfigureDocInfo(info => info.SetOfdEntity("DocID", options.OverrideDocId));
            }

            if (!string.IsNullOrWhiteSpace(options.TargetOfdVersion))
            {
                writer.SetOfdVersion(options.TargetOfdVersion);
            }

            if (options.ConfigureDocInfo != null)
            {
                writer.ConfigureDocInfo(options.ConfigureDocInfo!);
            }
        }
    }

    /// <summary>
    /// 配置页面布局
    /// </summary>
    private static void ConfigurePageLayout(IOfdDocWriter ofd, iText.Kernel.Geom.Rectangle? firstPageSize, ILogger logger)
    {
        if (firstPageSize != null)
        {
            var pw = firstPageSize.GetWidth() * Pt2Mm;
            var ph = firstPageSize.GetHeight() * Pt2Mm;
            (ofd as OfdWriter)?.SetDefaultPageLayout(new PageLayout(pw, ph));
            logger.LogInformation("[PDF2OFD] 已设置OFD页面尺寸 {W:0.##}mm x {H:0.##}mm", pw, ph);
        }
    }

    /// <summary>
    /// 验证和报告（如果启用）
    /// </summary>
    private static async Task ValidateAndReportIfEnabled(PdfToOfdOptions options, string pdfPath, string ofdOutputDir, ILogger logger)
    {
        if (options.EnableValidation && options.Validator != null)
        {
            logger.LogInformation("[PDF2OFD] 开始验证生成的OFD文件");
            var validationResult = options.Validator.Validate(ofdOutputDir);

            var allErrors = validationResult.SchemaErrors.Concat(validationResult.SemanticErrors).ToList();

            if (allErrors.Count > 0)
            {
                logger.LogWarning("[PDF2OFD] 发现 {Count} 个验证问题", allErrors.Count);

                // 如果配置了报告生成器,生成报告
                if (options.ReportBuilder != null && !string.IsNullOrWhiteSpace(options.ReportPath))
                {
                    options.ReportBuilder
                        .WithJob(System.Guid.NewGuid().ToString(), pdfPath, ofdOutputDir)
                        .WithStartTime(DateTime.UtcNow)
                        .AddErrors(allErrors)
                        .BuildToFile(options.ReportPath, indented: true);

                    logger.LogInformation("[PDF2OFD] 验证报告已保存到: {Path}", options.ReportPath);
                }
            }
            else
            {
                logger.LogInformation("[PDF2OFD] OFD验证通过,无问题发现");
            }
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 获取 PDF 信息值
    /// </summary>
    private static string? GetPdfInfoValue(PdfDocumentInfo info, string key)
    {
        var value = info.GetMoreInfo(key);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var lower = key.ToLowerInvariant();
        if (!string.Equals(lower, key, StringComparison.Ordinal))
        {
            value = info.GetMoreInfo(lower);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// 标准化 PDF 日期字符串
    /// </summary>
    private static string? NormalizePdfDateString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (!trimmed.StartsWith("D:", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        // 标准化前缀为大写 D:
        return trimmed.Length > 2 ? "D:" + trimmed.Substring(2) : "D:";
    }

    /// <summary>
    /// 尝试解析 PDF 日期
    /// </summary>
    private static DateTime? TryParsePdfDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        try
        {
            return PdfDate.Decode(trimmed);
        }
        catch
        {
            if (DateTime.TryParse(trimmed, out var dt))
            {
                return dt;
            }
        }
        return null;
    }

    #endregion
}
