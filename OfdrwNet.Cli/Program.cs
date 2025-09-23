using System.CommandLine;
using Microsoft.Extensions.Logging;
using OfdrwNet.Converter;

namespace OfdrwNet.Cli;

/// <summary>
/// OFDRW.NET CLI 工具
/// 提供命令行接口进行PDF到OFD转换
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        // 创建根命令
        var rootCommand = new RootCommand("OFDRW.NET - PDF到OFD转换工具");

        // 创建convert子命令
        var convertCommand = new Command("convert", "将PDF文件转换为OFD格式");

        // 添加参数和选项
        var inputFileOption = new Option<FileInfo>(
            new[] { "--input", "-i" },
            "输入的PDF文件路径")
        {
            IsRequired = true
        };

        var outputFileOption = new Option<FileInfo>(
            new[] { "--output", "-o" },
            "输出的OFD文件路径")
        {
            IsRequired = true
        };

        var passwordOption = new Option<string>(
            new[] { "--password", "-p" },
            "PDF文件的密码（如果有加密）");

        var parallelOption = new Option<int>(
            new[] { "--parallel", "--threads" },
            description: "并行处理线程数（默认：自动，根据CPU核心数）",
            getDefaultValue: () => Environment.ProcessorCount);

        var verboseOption = new Option<bool>(
            new[] { "--verbose", "-v" },
            "启用详细日志输出");

        var extractFontsOption = new Option<bool>(
            new[] { "--extract-fonts" },
            description: "提取并嵌入字体（默认：true）",
            getDefaultValue: () => true);

        var realImageEmbeddingOption = new Option<bool>(
            new[] { "--real-image-embedding" },
            description: "直接嵌入原始图片（默认：true）",
            getDefaultValue: () => true);

        var perGlyphPositioningOption = new Option<bool>(
            new[] { "--per-glyph-positioning" },
            description: "逐字定位（可能影响性能，默认：false）",
            getDefaultValue: () => false);

        // 将选项添加到convert命令
        convertCommand.AddOption(inputFileOption);
        convertCommand.AddOption(outputFileOption);
        convertCommand.AddOption(passwordOption);
        convertCommand.AddOption(parallelOption);
        convertCommand.AddOption(verboseOption);
        convertCommand.AddOption(extractFontsOption);
        convertCommand.AddOption(realImageEmbeddingOption);
        convertCommand.AddOption(perGlyphPositioningOption);

        // 设置convert命令的处理逻辑
        convertCommand.SetHandler(async (inputFile, outputFile, password, parallel, verbose,
            extractFonts, realImageEmbedding, perGlyphPositioning) =>
        {
            await ConvertPdfToOfd(inputFile, outputFile, password, parallel, verbose,
                extractFonts, realImageEmbedding, perGlyphPositioning);
        }, inputFileOption, outputFileOption, passwordOption, parallelOption, verboseOption,
           extractFontsOption, realImageEmbeddingOption, perGlyphPositioningOption);

        // 将convert命令添加到根命令
        rootCommand.AddCommand(convertCommand);

        // 解析并执行命令
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// 执行PDF到OFD转换
    /// </summary>
    private static async Task ConvertPdfToOfd(FileInfo inputFile, FileInfo outputFile, string? password,
        int parallel, bool verbose, bool extractFonts, bool realImageEmbedding, bool perGlyphPositioning)
    {
        // 设置日志级别
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            if (verbose)
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
            }
            else
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            }
        });

        var logger = loggerFactory.CreateLogger("PDF2OFD");

        try
        {
            logger.LogInformation("开始PDF到OFD转换...");
            logger.LogInformation("输入文件: {Input}", inputFile.FullName);
            logger.LogInformation("输出文件: {Output}", outputFile.FullName);
            logger.LogInformation("并行线程数: {Parallel}", parallel);

            if (!string.IsNullOrEmpty(password))
            {
                logger.LogInformation("使用密码保护的PDF文件");
            }

            // 创建转换选项
            var options = new ConvertHelper.PdfToOfdOptions
            {
                Password = password,
                ExtractAndEmbedFonts = extractFonts,
                RealImageEmbedding = realImageEmbedding,
                PerGlyphPositioning = perGlyphPositioning,
                MaxDegreeOfParallelism = parallel,
                Logger = logger,
                Progress = new Progress<(int done, int total)>(progress =>
                {
                    var percentage = progress.total > 0 ? (progress.done * 100 / progress.total) : 0;
                    logger.LogInformation("转换进度: {Done}/{Total} ({Percent}%)", progress.done, progress.total, percentage);
                })
            };

            // 记录开始时间
            var startTime = DateTime.Now;

            // 执行转换
            await ConvertHelper.PdfToOfdAsync(inputFile.FullName, outputFile.FullName, options);

            // 计算耗时
            var duration = DateTime.Now - startTime;

            // 获取文件大小信息
            var inputSize = inputFile.Exists ? inputFile.Length : 0;
            var outputSize = outputFile.Exists ? outputFile.Length : 0;

            logger.LogInformation("转换完成!");
            logger.LogInformation("耗时: {Duration:F2}秒", duration.TotalSeconds);
            logger.LogInformation("输入文件大小: {InputSize}", FormatFileSize(inputSize));
            logger.LogInformation("输出文件大小: {OutputSize}", FormatFileSize(outputSize));

            // 检查输出文件是否存在
            if (outputFile.Exists)
            {
                logger.LogInformation("OFD文件已成功生成: {Output}", outputFile.FullName);
            }
            else
            {
                logger.LogError("输出文件未生成，可能转换失败");
                Environment.ExitCode = 1;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "转换过程中发生错误");
            Environment.ExitCode = 1;
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }

    /// <summary>
    /// 格式化文件大小显示
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }
}