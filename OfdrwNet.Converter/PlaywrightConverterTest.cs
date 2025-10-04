using OfdrwNet.Converter;
using OfdrwNet.Converter.Options;
using Microsoft.Extensions.Logging;

namespace OfdrwNet.Converter.Tests;

/// <summary>
/// 测试 Playwright PDF 转换器
/// </summary>
public class PlaywrightConverterTest
{
    public static async Task TestPlaywrightConversion()
    {
        // 设置日志
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
        });
        var logger = loggerFactory.CreateLogger<PlaywrightConverterTest>();

        logger.LogInformation("开始 Playwright PDF 转换测试");

        try
        {
            // 测试文件路径
            var testPdfPath = @"D:\workspace\ofdrw-master\ofdrw-net-copilot\33190-2016-gbt-cd-300.pdf";
            var outputOfdPath = @"D:\workspace\ofdrw-master\ofdrw-net-copilot\test_playwright_output.ofd";

            // 转换选项
            var options = new PlaywrightConvertOptions
            {
                RenderScale = 2.0,
                RenderPageAsImage = false, // 只提取文本，不渲染背景图片
                ExtractText = true
            };

            // 执行转换
            await ConvertHelper.PdfToOfdByPlaywrightAsync(testPdfPath, outputOfdPath, options);

            logger.LogInformation("Playwright PDF 转换测试完成");
            logger.LogInformation("输出文件: {OutputPath}", outputOfdPath);

            // 验证输出文件
            if (File.Exists(outputOfdPath))
            {
                var fileInfo = new FileInfo(outputOfdPath);
                logger.LogInformation("OFD 文件生成成功，大小: {Size} bytes", fileInfo.Length);
            }
            else
            {
                logger.LogError("OFD 文件生成失败");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playwright PDF 转换测试失败");
            throw;
        }
    }

    /// <summary>
    /// 对比测试：使用传统方法和 Playwright 方法转换同一个 PDF
    /// </summary>
    public static async Task CompareConversionMethods()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
        });
        var logger = loggerFactory.CreateLogger<PlaywrightConverterTest>();

        var testPdfPath = @"D:\workspace\ofdrw-master\ofdrw-net-copilot\33190-2016-gbt-cd-300.pdf";
        var traditionalOutput = @"D:\workspace\ofdrw-master\ofdrw-net-copilot\test_traditional_output.ofd";
        var playwrightOutput = @"D:\workspace\ofdrw-master\ofdrw-net-copilot\test_playwright_output.ofd";

        try
        {
            logger.LogInformation("开始对比测试：传统方法 vs Playwright 方法");

            // 1. 传统方法
            logger.LogInformation("使用传统方法转换...");
            var traditionalOptions = new PdfToOfdOptions
            {
                ExtractText = true,
                ExtractImage = true,
                Logger = logger
            };

            await ConvertHelper.PdfToOfdAsync(testPdfPath, Path.GetDirectoryName(traditionalOutput) ?? "", traditionalOptions);

            // 2. Playwright 方法
            logger.LogInformation("使用 Playwright 方法转换...");
            var playwrightOptions = new PlaywrightConvertOptions
            {
                RenderScale = 2.0,
                RenderPageAsImage = false,
                ExtractText = true
            };

            await ConvertHelper.PdfToOfdByPlaywrightAsync(testPdfPath, playwrightOutput, playwrightOptions);

            // 3. 比较结果
            logger.LogInformation("转换完成，比较结果:");

            if (File.Exists(traditionalOutput))
            {
                var traditionalSize = new FileInfo(traditionalOutput).Length;
                logger.LogInformation("传统方法输出: {Size} bytes", traditionalSize);
            }
            else
            {
                logger.LogWarning("传统方法输出文件不存在");
            }

            if (File.Exists(playwrightOutput))
            {
                var playwrightSize = new FileInfo(playwrightOutput).Length;
                logger.LogInformation("Playwright 方法输出: {Size} bytes", playwrightSize);
            }
            else
            {
                logger.LogWarning("Playwright 方法输出文件不存在");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "对比测试失败");
            throw;
        }
    }
}
