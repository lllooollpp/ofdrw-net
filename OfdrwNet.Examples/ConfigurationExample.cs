using OfdrwNet;

namespace OfdrwNet.Examples;

/// <summary>
/// 配置管理示例
/// 演示 OFDRW.NET 的各种配置选项
/// </summary>
public class ConfigurationExample
{
    /// <summary>
    /// 运行配置管理示例
    /// </summary>
    public static async Task RunAsync()
    {
        Console.WriteLine("=== 配置管理示例 ===");
        
        try
        {
            // 1. 显示默认配置
            ShowDefaultConfiguration();
            
            // 2. 字体配置示例
            await FontConfigurationExample();
            
            // 3. 性能配置示例
            PerformanceConfigurationExample();
            
            // 4. 渲染配置示例
            RenderingConfigurationExample();
            
            // 5. 自定义配置示例
            CustomConfigurationExample();
            
            // 6. 重置配置示例
            ResetConfigurationExample();
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 配置管理示例出错: {ex.Message}");
        }
        
        Console.WriteLine("=== 配置管理示例完成 ===\n");
    }
    
    /// <summary>
    /// 显示默认配置
    /// </summary>
    private static void ShowDefaultConfiguration()
    {
        Console.WriteLine("1. 默认配置信息:");
        Console.WriteLine($"  版本: {OfdrwConfiguration.Version}");
        Console.WriteLine($"  OFD标准: {OfdrwConfiguration.OfdStandardVersion}");
        Console.WriteLine($"  默认字体: {OfdrwConfiguration.DefaultFont}");
        Console.WriteLine($"  默认字号: {OfdrwConfiguration.DefaultFontSize}mm");
        Console.WriteLine($"  默认创建者: {OfdrwConfiguration.DefaultCreator}");
        Console.WriteLine($"  并行处理: {OfdrwConfiguration.EnableParallelProcessing}");
        Console.WriteLine($"  最大并行度: {OfdrwConfiguration.MaxDegreeOfParallelism}");
        Console.WriteLine($"  内存缓存: {OfdrwConfiguration.MemoryCacheSizeMB}MB");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 字体配置示例
    /// </summary>
    private static async Task FontConfigurationExample()
    {
        Console.WriteLine("2. 字体配置示例:");
        
        // 显示内置字体映射
        Console.WriteLine("  内置字体映射:");
        var commonFonts = new[] { "宋体", "黑体", "楷体", "Arial", "Times New Roman" };
        foreach (var font in commonFonts)
        {
            if (OfdrwConfiguration.FontMappings.ContainsKey(font))
            {
                var mapped = OfdrwConfiguration.GetMappedFont(font);
                Console.WriteLine($"    {font} -> {mapped}");
            }
        }
        
        // 添加自定义字体映射
        Console.WriteLine("  添加自定义字体映射:");
        OfdrwConfiguration.AddFontMapping("华文细黑", "STXihei");
        OfdrwConfiguration.AddFontMapping("Comic Sans MS", "Comic Sans MS");
        Console.WriteLine("    华文细黑 -> STXihei");
        Console.WriteLine("    Comic Sans MS -> Comic Sans MS");
        
        // 测试字体映射
        Console.WriteLine("  字体映射测试:");
        var testFonts = new[] { "宋体", "华文细黑", "UnknownFont" };
        foreach (var font in testFonts)
        {
            var mapped = OfdrwConfiguration.GetMappedFont(font);
            Console.WriteLine($"    {font} -> {mapped}");
        }
        
        // 创建使用自定义字体的文档
        var fontTestDoc = Path.Combine("Examples", "FontTest.ofd");
        Directory.CreateDirectory("Examples");
        
        using (var doc = new OFDDoc(fontTestDoc))
        {
            doc.Add(new TextParagraph("字体映射测试文档")
            {
                FontSize = 16,
                FontFamily = "华文细黑", // 使用自定义映射的字体
                Position = new Position(50, 50)
            });
            
            doc.Add(new TextParagraph("这段文字使用了自定义字体映射")
            {
                FontSize = 12,
                FontFamily = "Comic Sans MS",
                Position = new Position(50, 80)
            });
            
            await doc.CloseAsync();
        }
        
        Console.WriteLine($"  ✓ 字体测试文档已创建: FontTest.ofd");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 性能配置示例
    /// </summary>
    private static void PerformanceConfigurationExample()
    {
        Console.WriteLine("3. 性能配置示例:");
        
        // 保存原始配置
        var originalParallel = OfdrwConfiguration.EnableParallelProcessing;
        var originalDegree = OfdrwConfiguration.MaxDegreeOfParallelism;
        var originalCacheSize = OfdrwConfiguration.MemoryCacheSizeMB;
        
        Console.WriteLine($"  原始配置:");
        Console.WriteLine($"    并行处理: {originalParallel}");
        Console.WriteLine($"    最大并行度: {originalDegree}");
        Console.WriteLine($"    内存缓存: {originalCacheSize}MB");
        
        // 修改性能配置
        OfdrwConfiguration.EnableParallelProcessing = true;
        OfdrwConfiguration.MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount / 2);
        OfdrwConfiguration.MemoryCacheSizeMB = 200;
        OfdrwConfiguration.EnableFileCache = true;
        
        Console.WriteLine($"  优化后配置:");
        Console.WriteLine($"    并行处理: {OfdrwConfiguration.EnableParallelProcessing}");
        Console.WriteLine($"    最大并行度: {OfdrwConfiguration.MaxDegreeOfParallelism}");
        Console.WriteLine($"    内存缓存: {OfdrwConfiguration.MemoryCacheSizeMB}MB");
        Console.WriteLine($"    文件缓存: {OfdrwConfiguration.EnableFileCache}");
        
        Console.WriteLine("  ℹ 这些配置在处理大量文档时可以提高性能");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 渲染配置示例
    /// </summary>
    private static void RenderingConfigurationExample()
    {
        Console.WriteLine("4. 渲染配置示例:");
        
        Console.WriteLine($"  当前渲染配置:");
        Console.WriteLine($"    默认DPI: {OfdrwConfiguration.DefaultDpi}");
        Console.WriteLine($"    图片压缩质量: {OfdrwConfiguration.ImageCompressionQuality}%");
        Console.WriteLine($"    抗锯齿: {OfdrwConfiguration.EnableAntiAliasing}");
        Console.WriteLine($"    文本渲染: {OfdrwConfiguration.TextRenderingHint}");
        
        // 高质量渲染配置
        OfdrwConfiguration.DefaultDpi = 600;
        OfdrwConfiguration.ImageCompressionQuality = 95;
        OfdrwConfiguration.EnableAntiAliasing = true;
        OfdrwConfiguration.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        
        Console.WriteLine($"  高质量渲染配置:");
        Console.WriteLine($"    默认DPI: {OfdrwConfiguration.DefaultDpi}");
        Console.WriteLine($"    图片压缩质量: {OfdrwConfiguration.ImageCompressionQuality}%");
        Console.WriteLine($"    抗锯齿: {OfdrwConfiguration.EnableAntiAliasing}");
        Console.WriteLine($"    文本渲染: {OfdrwConfiguration.TextRenderingHint}");
        Console.WriteLine("  ℹ 高质量配置适合最终输出，但会增加处理时间");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 自定义配置示例
    /// </summary>
    private static void CustomConfigurationExample()
    {
        Console.WriteLine("5. 自定义配置示例:");
        
        // 添加自定义配置
        OfdrwConfiguration.SetCustomSetting("CompanyName", "示例公司");
        OfdrwConfiguration.SetCustomSetting("ProjectName", "OFD文档处理项目");
        OfdrwConfiguration.SetCustomSetting("MaxRetries", 3);
        OfdrwConfiguration.SetCustomSetting("EnableDebugMode", true);
        OfdrwConfiguration.SetCustomSetting("WatermarkText", "内部文档");
        
        Console.WriteLine("  已添加自定义配置:");
        Console.WriteLine($"    公司名称: {OfdrwConfiguration.GetCustomSetting<string>("CompanyName")}");
        Console.WriteLine($"    项目名称: {OfdrwConfiguration.GetCustomSetting<string>("ProjectName")}");
        Console.WriteLine($"    最大重试: {OfdrwConfiguration.GetCustomSetting<int>("MaxRetries")}");
        Console.WriteLine($"    调试模式: {OfdrwConfiguration.GetCustomSetting<bool>("EnableDebugMode")}");
        Console.WriteLine($"    水印文字: {OfdrwConfiguration.GetCustomSetting<string>("WatermarkText")}");
        
        // 获取不存在的配置（使用默认值）
        var timeout = OfdrwConfiguration.GetCustomSetting("RequestTimeout", 30);
        var theme = OfdrwConfiguration.GetCustomSetting("Theme", "Default");
        
        Console.WriteLine("  默认值示例:");
        Console.WriteLine($"    请求超时: {timeout}秒 (默认值)");
        Console.WriteLine($"    主题: {theme} (默认值)");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 重置配置示例
    /// </summary>
    private static void ResetConfigurationExample()
    {
        Console.WriteLine("6. 配置重置示例:");
        
        Console.WriteLine($"  重置前 - 默认字体: {OfdrwConfiguration.DefaultFont}");
        Console.WriteLine($"  重置前 - DPI: {OfdrwConfiguration.DefaultDpi}");
        Console.WriteLine($"  重置前 - 自定义配置数量: {OfdrwConfiguration.CustomSettings.Count}");
        
        // 重置所有配置
        OfdrwConfiguration.ResetToDefaults();
        
        Console.WriteLine($"  重置后 - 默认字体: {OfdrwConfiguration.DefaultFont}");
        Console.WriteLine($"  重置后 - DPI: {OfdrwConfiguration.DefaultDpi}");
        Console.WriteLine($"  重置后 - 自定义配置数量: {OfdrwConfiguration.CustomSettings.Count}");
        Console.WriteLine("  ✓ 所有配置已重置为默认值");
        Console.WriteLine();
    }
}