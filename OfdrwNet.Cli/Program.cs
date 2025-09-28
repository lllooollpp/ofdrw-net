using System.CommandLine;
using Microsoft.Extensions.Logging;
using OfdrwNet.Converter;
using OfdrwNet.Reader;
using System.IO.Compression;

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

        // 三态：未提供 -> 使用内部默认(true)；提供 true/false -> 强制覆盖
        var alphaWhiteToTransparentOption = new Option<bool?>(
            new[] { "--alpha-white", "--alpha-white-to-transparent" },
            description: "启用/关闭白底转透明 (未提供则用内部默认: true)。示例: --alpha-white true 或 --alpha-white false");

        var whiteThresholdOption = new Option<byte>(
            new[] { "--white-threshold" },
            description: "白色判定阈值(0-255, 默认 250)",
            getDefaultValue: () => (byte)250);

        var onlyIfOpaqueOption = new Option<bool>(
            new[] { "--only-if-opaque" },
            description: "仅当图片原本没有 Alpha 通道时才执行白底转透明（默认:true）",
            getDefaultValue: () => true);

        var forceAlphaWhiteOption = new Option<bool>(
            new[] { "--force-alpha-white" },
            description: "忽略是否已有 Alpha，强制执行白底转透明（优先级高于 --only-if-opaque）");

        var docIdOption = new Option<string>(
            new[] { "--doc-id" },
            description: "覆盖 DocInfo.DocID，传入 32 位 UUID 字符串");

        var noDocIdOption = new Option<bool>(
            new[] { "--no-doc-id" },
            description: "生成的 OFD 中移除 DocID，并禁用自动生成" );

        var docTitleOption = new Option<string>(
            new[] { "--doc-title" },
            description: "覆盖 DocInfo 标题" );

        var docAuthorOption = new Option<string>(
            new[] { "--doc-author" },
            description: "覆盖 DocInfo 作者" );

        var docCreatorOption = new Option<string>(
            new[] { "--doc-creator" },
            description: "覆盖 DocInfo Creator" );

        var docCreatorVersionOption = new Option<string>(
            new[] { "--doc-creator-version" },
            description: "覆盖 DocInfo CreatorVersion" );

        var docSubjectOption = new Option<string>(
            new[] { "--doc-subject" },
            description: "覆盖 DocInfo Subject" );

        var docKeywordsOption = new Option<string>(
            new[] { "--doc-keywords" },
            description: "覆盖 DocInfo Keywords 原始文本" );

        var docCreationDateOption = new Option<string>(
            new[] { "--doc-creation-date" },
            description: "覆盖 DocInfo CreationDate（原始字符串，如 D:20201223235959+08'00'）" );

        var docModDateOption = new Option<string>(
            new[] { "--doc-mod-date" },
            description: "覆盖 DocInfo ModDate（原始字符串）" );

        // 将选项添加到convert命令
        convertCommand.AddOption(inputFileOption);
        convertCommand.AddOption(outputFileOption);
        convertCommand.AddOption(passwordOption);
        convertCommand.AddOption(parallelOption);
        convertCommand.AddOption(verboseOption);
        convertCommand.AddOption(extractFontsOption);
        convertCommand.AddOption(realImageEmbeddingOption);
        convertCommand.AddOption(perGlyphPositioningOption);
        convertCommand.AddOption(alphaWhiteToTransparentOption);
        convertCommand.AddOption(whiteThresholdOption);
        convertCommand.AddOption(onlyIfOpaqueOption);
        convertCommand.AddOption(forceAlphaWhiteOption);
        convertCommand.AddOption(docIdOption);
        convertCommand.AddOption(noDocIdOption);
        convertCommand.AddOption(docTitleOption);
        convertCommand.AddOption(docAuthorOption);
        convertCommand.AddOption(docCreatorOption);
        convertCommand.AddOption(docCreatorVersionOption);
        convertCommand.AddOption(docSubjectOption);
        convertCommand.AddOption(docKeywordsOption);
        convertCommand.AddOption(docCreationDateOption);
        convertCommand.AddOption(docModDateOption);

        // 设置convert命令的处理逻辑
        convertCommand.SetHandler(async (inputFile, outputFile, password, parallel, verbose,
            extractFonts, realImageEmbedding, perGlyphPositioning, alphaWhiteNullable, whiteThr, onlyIfOpaque, forceAlphaWhite,
            docId, noDocId, docTitle, docAuthor, docCreator, docCreatorVersion, docSubject, docKeywords, docCreationDate, docModDate) =>
        {
            // 处理三态逻辑：未提供 -> null -> 使用内部默认 (true)
            bool makeWhiteTransparent = alphaWhiteNullable ?? true; // 内部默认 true
            bool effectiveOnlyIfOpaque = onlyIfOpaque;
            if (forceAlphaWhite) effectiveOnlyIfOpaque = false;

            await ConvertPdfToOfd(inputFile, outputFile, password, parallel, verbose,
                extractFonts, realImageEmbedding, perGlyphPositioning, makeWhiteTransparent, whiteThr, effectiveOnlyIfOpaque, forceAlphaWhite,
                alphaWhiteNullable.HasValue, docId, noDocId, docTitle, docAuthor, docCreator, docCreatorVersion, docSubject, docKeywords, docCreationDate, docModDate);
        }, inputFileOption, outputFileOption, passwordOption, parallelOption, verboseOption,
           extractFontsOption, realImageEmbeddingOption, perGlyphPositioningOption, alphaWhiteToTransparentOption, whiteThresholdOption, onlyIfOpaqueOption, forceAlphaWhiteOption,
           docIdOption, noDocIdOption, docTitleOption, docAuthorOption, docCreatorOption, docCreatorVersionOption, docSubjectOption, docKeywordsOption, docCreationDateOption, docModDateOption);

        // 将convert命令添加到根命令
        rootCommand.AddCommand(convertCommand);

        // 创建debug子命令
        var debugCommand = CreateDebugCommand();
        rootCommand.AddCommand(debugCommand);

        // 创建 alpha-scan 子命令（检测 PNG 是否含透明像素）
        var alphaScanCommand = new Command("alpha-scan", "扫描目录下的 Image_*.png 是否含透明像素并输出统计")
        {
            new Option<DirectoryInfo>(new[]{"--dir","-d"}, "要扫描的目录") { IsRequired = true },
            new Option<int>(new[]{"--sample-step"}, ()=>40, "抽样步长 (越小越精确, 默认40 像素步长)"),
            new Option<bool>(new[]{"--full"}, "是否逐像素遍历 (可能较慢)" )
        };
        alphaScanCommand.SetHandler((DirectoryInfo dir, int step, bool full) =>
        {
            if(!dir.Exists){ Console.WriteLine($"目录不存在: {dir.FullName}"); return; }
            var files = dir.GetFiles("Image_*.png");
            if(files.Length==0){ Console.WriteLine("无匹配文件 Image_*.png"); return; }
            Console.WriteLine($"扫描目录: {dir.FullName} 文件数={files.Length}");
            foreach(var f in files){
                try{
                    using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(f.FullName);
                    bool hasAlphaChannel = true; // Rgba32 已包含 Alpha 通道
                    long w = img.Width; long h = img.Height;
                    long total=0; long trans=0;
                    int sx = full?1: Math.Max(1, (int)w/(step==0?40:step));
                    int sy = full?1: Math.Max(1, (int)h/(step==0?40:step));
                    img.ProcessPixelRows(accessor=>{
                        for(int y=0;y<h;y+=sy){
                            var row = accessor.GetRowSpan(y);
                            for(int x=0;x<w;x+=sx){
                                var px = row[x];
                                if(px.A < 255) trans++;
                                total++;
                            }
                        }
                    });
                    double ratio = total==0?0: (double)trans/total;
                    Console.WriteLine($"{f.Name}\tSize={w}x{h}\tAlphaSamples={total}\tTransSamples={trans}\tTransRatio={(ratio*100):0.##}%");
                }catch(Exception ex){
                    Console.WriteLine($"{f.Name}\tERROR {ex.Message}");
                }
            }
        }, alphaScanCommand.Options[0], alphaScanCommand.Options[1], alphaScanCommand.Options[2]);
        rootCommand.AddCommand(alphaScanCommand);

        // 解析并执行命令
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// 执行PDF到OFD转换
    /// </summary>
    private static async Task ConvertPdfToOfd(FileInfo inputFile, FileInfo outputFile, string? password,
        int parallel, bool verbose, bool extractFonts, bool realImageEmbedding, bool perGlyphPositioning,
        bool makeWhiteTransparent, byte whiteThr, bool onlyIfOpaque, bool forceAlphaWhite, bool alphaWhiteExplicit,
        string? docId, bool noDocId, string? docTitle, string? docAuthor, string? docCreator, string? docCreatorVersion,
        string? docSubject, string? docKeywords, string? docCreationDate, string? docModDate)
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
                MakeWhiteBackgroundTransparent = makeWhiteTransparent,
                WhiteThreshold = whiteThr,
                OnlyIfOpaque = onlyIfOpaque,
                Progress = new Progress<(int done, int total)>(progress =>
                {
                    var percentage = progress.total > 0 ? (progress.done * 100 / progress.total) : 0;
                    logger.LogInformation("转换进度: {Done}/{Total} ({Percent}%)", progress.done, progress.total, percentage);
                })
            };

            bool hasDocOverrides = !string.IsNullOrWhiteSpace(docId) || noDocId
                || !string.IsNullOrWhiteSpace(docTitle) || !string.IsNullOrWhiteSpace(docAuthor)
                || !string.IsNullOrWhiteSpace(docCreator) || !string.IsNullOrWhiteSpace(docCreatorVersion)
                || !string.IsNullOrWhiteSpace(docSubject) || !string.IsNullOrWhiteSpace(docKeywords)
                || !string.IsNullOrWhiteSpace(docCreationDate) || !string.IsNullOrWhiteSpace(docModDate);

            if (hasDocOverrides)
            {
                options.OverrideDocId = string.IsNullOrWhiteSpace(docId) ? null : docId.Trim();
                options.AutoGenerateDocId = string.IsNullOrWhiteSpace(docId) && !noDocId;
                options.RemoveDocId = noDocId && string.IsNullOrWhiteSpace(docId);
                options.DocTitle = string.IsNullOrWhiteSpace(docTitle) ? null : docTitle;
                options.DocAuthor = string.IsNullOrWhiteSpace(docAuthor) ? null : docAuthor;
                options.DocCreator = string.IsNullOrWhiteSpace(docCreator) ? null : docCreator;
                options.DocCreatorVersion = string.IsNullOrWhiteSpace(docCreatorVersion) ? null : docCreatorVersion;
                options.DocSubject = string.IsNullOrWhiteSpace(docSubject) ? null : docSubject;
                options.DocKeywords = string.IsNullOrWhiteSpace(docKeywords) ? null : docKeywords;
                options.DocCreationDateRaw = string.IsNullOrWhiteSpace(docCreationDate) ? null : docCreationDate;
                options.DocModDateRaw = string.IsNullOrWhiteSpace(docModDate) ? null : docModDate;
                if (!string.IsNullOrWhiteSpace(docId))
                {
                    logger.LogInformation("DocID 将被覆盖为 {DocId}", docId);
                }
                else if (noDocId)
                {
                    logger.LogInformation("已禁用 DocID 自动生成并移除 DocID 元素");
                }
            }
            else
            {
                options.AutoGenerateDocId = true;
                options.RemoveDocId = false;
            }

            if (!string.IsNullOrWhiteSpace(docTitle)) logger.LogInformation("DocInfo.Title -> {Value}", docTitle);
            if (!string.IsNullOrWhiteSpace(docAuthor)) logger.LogInformation("DocInfo.Author -> {Value}", docAuthor);
            if (!string.IsNullOrWhiteSpace(docCreator)) logger.LogInformation("DocInfo.Creator -> {Value}", docCreator);

            logger.LogInformation("白底转透明: {Enabled} (来源: {Source}) 阈值: {Thr} OnlyIfOpaque={OnlyIfOpaque} Force={Force}",
                makeWhiteTransparent, alphaWhiteExplicit ? "用户显式" : "内部默认", whiteThr, onlyIfOpaque, forceAlphaWhite);

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

    /// <summary>
    /// 创建debug命令
    /// </summary>
    private static Command CreateDebugCommand()
    {
        var debugCommand = new Command("debug", "调试OFD文件加载和渲染问题");

        var inputFileOption = new Option<FileInfo>(
            new[] { "--file", "-f" },
            "要调试的OFD文件路径")
        {
            IsRequired = true
        };

        var verboseOption = new Option<bool>(
            new[] { "--verbose", "-v" },
            "启用详细日志输出");

        debugCommand.AddOption(inputFileOption);
        debugCommand.AddOption(verboseOption);

        debugCommand.SetHandler(async (inputFile, verbose) =>
        {
            await DebugOfdFile(inputFile, verbose);
        }, inputFileOption, verboseOption);

        return debugCommand;
    }

    /// <summary>
    /// 调试OFD文件
    /// </summary>
    private static async Task DebugOfdFile(FileInfo inputFile, bool verbose)
    {
        // 设置日志级别
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(verbose ? Microsoft.Extensions.Logging.LogLevel.Debug : Microsoft.Extensions.Logging.LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger("OFDDebug");

        try
        {
            Console.WriteLine($"=== 调试OFD文件: {inputFile.Name} ===");
            Console.WriteLine();

            // 检查文件是否存在
            if (!inputFile.Exists)
            {
                Console.WriteLine($"❌ 文件不存在: {inputFile.FullName}");
                return;
            }

            Console.WriteLine($"✅ 文件存在，大小: {FormatFileSize(inputFile.Length)}");

            // 检查文件是否为有效的ZIP文件
            try
            {
                using (var zipArchive = ZipFile.OpenRead(inputFile.FullName))
                {
                    Console.WriteLine($"✅ OFD文件结构有效，包含 {zipArchive.Entries.Count} 个条目");

                    // 显示文件结构
                    Console.WriteLine();
                    Console.WriteLine("📁 文件结构:");
                    foreach (var entry in zipArchive.Entries.OrderBy(e => e.FullName))
                    {
                        Console.WriteLine($"   {entry.FullName} ({FormatFileSize(entry.Length)})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ OFD文件结构无效: {ex.Message}");
                return;
            }

            Console.WriteLine();

            // 尝试使用OfdReader读取文件
            Console.WriteLine("📖 使用OfdReader加载文档...");
            try
            {
                using (var reader = new OfdReader(inputFile.FullName))
                {
                    Console.WriteLine("✅ OfdReader创建成功");

                    // 获取文档信息
                    var docInfo = await reader.GetDocumentInfoAsync();
                    Console.WriteLine($"✅ 文档元数据加载成功");
                    Console.WriteLine($"   标题: {docInfo.Title ?? "无"}");
                    Console.WriteLine($"   作者: {docInfo.Author ?? "无"}");
                    Console.WriteLine($"   页数: {docInfo.PageCount}");

                    // 获取页面列表
                    var pageList = reader.GetPageList();
                    Console.WriteLine($"✅ 页面列表获取成功，共 {pageList.Count} 页");

                    // 获取资源管理器
                    var resourceManager = reader.GetResourceManager();
                    Console.WriteLine("✅ 资源管理器创建成功");

                    // 尝试加载第一页内容
                    if (pageList.Count > 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("🔍 分析第一页内容...");

                        var firstPage = pageList[0];
                        Console.WriteLine($"   页面ID: {firstPage.Id}");
                        Console.WriteLine($"   页面尺寸: {firstPage.Width:F1}mm x {firstPage.Height:F1}mm");
                        Console.WriteLine($"   页面索引: {firstPage.Index}");
                        Console.WriteLine($"   页面序号: {firstPage.PageN}");

                        if (firstPage.Obj != null)
                        {
                            Console.WriteLine("✅ 页面内容XML加载成功");

                            // 分析页面内容
                            AnalyzePageContent(firstPage.Obj);

                            // 测试实际的图像资源加载
                            await TestImageResourceLoading(resourceManager, firstPage.Obj);
                        }
                        else
                        {
                            Console.WriteLine("❌ 页面内容XML为空");
                        }
                    }                    // 验证文档
                    var validation = await reader.ValidateDocumentAsync();
                    Console.WriteLine();
                    Console.WriteLine($"📋 文档验证结果: {(validation.IsValid ? "✅ 有效" : "❌ 无效")}");

                    if (validation.Errors.Any())
                    {
                        Console.WriteLine("❌ 验证错误:");
                        foreach (var error in validation.Errors)
                        {
                            Console.WriteLine($"   {error.Code}: {error.Message}");
                        }
                    }

                    if (validation.Warnings.Any())
                    {
                        Console.WriteLine("⚠️ 验证警告:");
                        foreach (var warning in validation.Warnings)
                        {
                            Console.WriteLine($"   {warning.Code}: {warning.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ OfdReader加载失败: {ex.Message}");
                if (verbose)
                {
                    Console.WriteLine($"详细错误: {ex}");
                }
            }
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }

    /// <summary>
    /// 分析页面内容
    /// </summary>
    private static void AnalyzePageContent(System.Xml.Linq.XElement pageContent)
    {
        try
        {
            var ns = System.Xml.Linq.XNamespace.Get("http://www.ofdspec.org/2016");

            // 统计文本对象
            var textObjects = pageContent.Descendants(ns + "TextObject");
            Console.WriteLine($"   📝 文本对象: {textObjects.Count()} 个");

            // 统计图像对象
            var imageObjects = pageContent.Descendants(ns + "ImageObject");
            Console.WriteLine($"   🖼️ 图像对象: {imageObjects.Count()} 个");

            if (imageObjects.Any())
            {
                Console.WriteLine("   图像资源ID:");
                foreach (var img in imageObjects)
                {
                    var resourceId = img.Attribute("ResourceID")?.Value;
                    var boundary = img.Attribute("Boundary")?.Value;
                    Console.WriteLine($"     - ID={resourceId}, Boundary={boundary}");
                }
            }

            // 统计路径对象
            var pathObjects = pageContent.Descendants(ns + "PathObject");
            Console.WriteLine($"   📐 路径对象: {pathObjects.Count()} 个");

            Console.WriteLine($"   📊 总对象数: {textObjects.Count() + imageObjects.Count() + pathObjects.Count()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ 页面内容分析失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试图像资源加载
    /// </summary>
    private static async Task TestImageResourceLoading(IResourceManager resourceManager, System.Xml.Linq.XElement pageContent)
    {
        try
        {
            Console.WriteLine();
            Console.WriteLine("🔍 测试图像资源加载...");

            var ns = System.Xml.Linq.XNamespace.Get("http://www.ofdspec.org/2016");
            var imageObjects = pageContent.Descendants(ns + "ImageObject");

            if (!imageObjects.Any())
            {
                Console.WriteLine("   没有找到图像对象");
                return;
            }

            // 获取前3个图像对象进行测试
            var testImages = imageObjects.Take(3).ToList();

            Console.WriteLine($"   正在测试 {testImages.Count} 个图像资源...");

            foreach (var img in testImages)
            {
                var resourceId = img.Attribute("ResourceID")?.Value;
                var boundary = img.Attribute("Boundary")?.Value;

                if (!string.IsNullOrEmpty(resourceId))
                {
                    try
                    {
                        Console.WriteLine($"   📷 测试图像 ID={resourceId}, Boundary={boundary}");

                        // 尝试加载图像资源
                        var image = await resourceManager.GetImageAsync(resourceId);

                        if (image != null)
                        {
                            Console.WriteLine($"      ✅ 成功加载图像: {image.Width}x{image.Height} 像素");
                            image.Dispose(); // 释放资源
                        }
                        else
                        {
                            Console.WriteLine($"      ❌ 图像加载返回null");
                        }
                    }
                    catch (Exception imgEx)
                    {
                        Console.WriteLine($"      ❌ 图像加载失败: {imgEx.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ 图像资源加载测试失败: {ex.Message}");
        }
    }
}
