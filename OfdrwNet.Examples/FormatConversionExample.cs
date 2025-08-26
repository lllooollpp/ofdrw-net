using OfdrwNet;

namespace OfdrwNet.Examples;

/// <summary>
/// 格式转换示例
/// 演示 OFD 与其他格式的转换功能
/// </summary>
public class FormatConversionExample
{
    /// <summary>
    /// 运行格式转换示例
    /// </summary>
    public static async Task RunAsync()
    {
        Console.WriteLine("=== 格式转换示例 ===");
        
        var exampleDir = "Examples";
        Directory.CreateDirectory(exampleDir);
        
        var ofdFile = Path.Combine(exampleDir, "ConversionTest.ofd");
        var pdfFile = Path.Combine(exampleDir, "ConversionTest.pdf");
        var imageDir = Path.Combine(exampleDir, "Images");
        
        try
        {
            // 1. 创建用于转换的 OFD 文档
            await CreateSampleDocumentAsync(ofdFile);
            
            // 2. OFD 转 PDF
            Console.WriteLine("2. OFD 转 PDF...");
            await ConvertOfdToPdfAsync(ofdFile, pdfFile);
            
            // 3. OFD 转图片
            Console.WriteLine("3. OFD 转图片...");
            await ConvertOfdToImagesAsync(ofdFile, imageDir);
            
            // 4. 演示批量转换
            Console.WriteLine("4. 批量转换演示...");
            await BatchConversionExample(exampleDir);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 格式转换示例出错: {ex.Message}");
        }
        
        Console.WriteLine("=== 格式转换示例完成 ===\n");
    }
    
    /// <summary>
    /// 创建用于转换的示例文档
    /// </summary>
    private static async Task CreateSampleDocumentAsync(string filePath)
    {
        Console.WriteLine("1. 创建用于转换的 OFD 文档...");
        
        using var doc = new OFDDoc(filePath);
        
        // 设置 A4 页面
        doc.SetDefaultPageLayout(PageLayout.A4());
        
        // 添加标题
        doc.Add(new TextParagraph("格式转换测试文档")
        {
            FontSize = 20,
            FontFamily = "Microsoft YaHei",
            Position = new Position(50, 50)
        });
        
        // 添加描述
        doc.Add(new TextParagraph("本文档用于演示 OFDRW.NET 的格式转换功能")
        {
            FontSize = 12,
            Position = new Position(50, 90)
        });
        
        // 添加特性列表
        var features = new[]
        {
            "支持的转换格式：",
            "  • OFD → PDF",
            "  • OFD → PNG/JPG 图片",
            "  • PDF → OFD（规划中）",
            "",
            "转换特点：",
            "  • 保持版面布局", 
            "  • 支持矢量图形",
            "  • 可配置分辨率",
            "  • 批量转换支持"
        };
        
        var yPos = 130.0;
        foreach (var feature in features)
        {
            doc.Add(new TextParagraph(feature)
            {
                FontSize = feature.StartsWith(" ") ? 10 : 11,
                FontFamily = feature.StartsWith(" ") ? "Consolas" : "SimSun",
                Position = new Position(50, yPos)
            });
            yPos += 18;
        }
        
        // 添加页脚
        doc.Add(new TextParagraph($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
        {
            FontSize = 9,
            Position = new Position(50, 250)
        });
        
        await doc.CloseAsync();
        Console.WriteLine($"  ✓ 测试文档已创建: {Path.GetFileName(filePath)}");
    }
    
    /// <summary>
    /// OFD 转 PDF
    /// </summary>
    private static async Task ConvertOfdToPdfAsync(string ofdPath, string pdfPath)
    {
        try
        {
            await OfdrwHelper.ConvertToPdfAsync(ofdPath, pdfPath);
            
            // 检查输出文件（注意：当前是占位符实现）
            if (File.Exists(pdfPath))
            {
                Console.WriteLine($"  ✓ PDF 转换成功: {Path.GetFileName(pdfPath)}");
            }
            else
            {
                Console.WriteLine("  ℹ PDF 转换完成（占位符实现）");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ PDF 转换失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// OFD 转图片
    /// </summary>
    private static async Task ConvertOfdToImagesAsync(string ofdPath, string imageDir)
    {
        try
        {
            Directory.CreateDirectory(imageDir);
            
            // 转换为 PNG，300 DPI
            await OfdrwHelper.ConvertToImagesAsync(ofdPath, imageDir, "png", 300);
            
            Console.WriteLine($"  ✓ 图片转换完成（占位符实现）");
            Console.WriteLine($"    输出目录: {Path.GetFileName(imageDir)}");
            Console.WriteLine($"    格式: PNG");
            Console.WriteLine($"    分辨率: 300 DPI");
            
            // 也尝试 JPG 格式
            await OfdrwHelper.ConvertToImagesAsync(ofdPath, imageDir, "jpg", 150);
            Console.WriteLine($"  ✓ JPG 格式转换完成（占位符实现，150 DPI）");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ 图片转换失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 批量转换示例
    /// </summary>
    private static async Task BatchConversionExample(string baseDir)
    {
        try
        {
            // 创建多个测试文件
            var testFiles = new[]
            {
                "BatchTest1.ofd",
                "BatchTest2.ofd", 
                "BatchTest3.ofd"
            };
            
            Console.WriteLine($"  创建 {testFiles.Length} 个测试文档...");
            
            foreach (var fileName in testFiles)
            {
                var filePath = Path.Combine(baseDir, fileName);
                await CreateSimpleDocumentAsync(filePath, fileName);
            }
            
            // 批量转换为 PDF
            Console.WriteLine("  批量转换为 PDF...");
            var convertedCount = 0;
            
            foreach (var fileName in testFiles)
            {
                var ofdPath = Path.Combine(baseDir, fileName);
                var pdfPath = Path.ChangeExtension(ofdPath, ".pdf");
                
                try
                {
                    await OfdrwHelper.ConvertToPdfAsync(ofdPath, pdfPath);
                    convertedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    转换 {fileName} 失败: {ex.Message}");
                }
            }
            
            Console.WriteLine($"  ✓ 批量转换完成: {convertedCount}/{testFiles.Length} 个文件");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ 批量转换失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 创建简单文档
    /// </summary>
    private static async Task CreateSimpleDocumentAsync(string filePath, string title)
    {
        using var doc = new OFDDoc(filePath);
        
        doc.Add(new TextParagraph(title)
        {
            FontSize = 16,
            Position = new Position(50, 50)
        });
        
        doc.Add(new TextParagraph("这是一个用于批量转换测试的简单文档。")
        {
            FontSize = 12,
            Position = new Position(50, 80)
        });
        
        await doc.CloseAsync();
    }
}