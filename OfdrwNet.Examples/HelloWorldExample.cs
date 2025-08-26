using OfdrwNet;

namespace OfdrwNet.Examples;

/// <summary>
/// HelloWorld 示例
/// 演示如何创建一个简单的 OFD 文档
/// </summary>
public class HelloWorldExample
{
    /// <summary>
    /// 运行 HelloWorld 示例
    /// </summary>
    public static async Task RunAsync()
    {
        Console.WriteLine("=== OFDRW.NET HelloWorld 示例 ===");
        
        // 设置输出文件路径
        var outputPath = Path.Combine("Examples", "HelloWorld.ofd");
        
        // 确保输出目录存在
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        
        try
        {
            // 方法1: 使用便捷 API
            Console.WriteLine("正在使用便捷API创建HelloWorld文档...");
            await OfdrwHelper.CreateHelloWorldAsync(outputPath, "你好，OFDRW.NET!");
            Console.WriteLine($"✓ HelloWorld文档已创建: {Path.GetFullPath(outputPath)}");
            
            // 方法2: 使用完整 API
            var outputPath2 = Path.Combine("Examples", "HelloWorldAdvanced.ofd");
            Console.WriteLine("正在使用完整API创建高级HelloWorld文档...");
            
            using (var doc = new OFDDoc(outputPath2))
            {
                // 设置页面布局为 A4
                doc.SetDefaultPageLayout(PageLayout.A4());
                
                // 添加标题
                doc.Add(new TextParagraph("OFDRW.NET 示例文档")
                {
                    FontSize = 18,
                    FontFamily = "Microsoft YaHei",
                    Position = new Position(50, 50)
                });
                
                // 添加副标题
                doc.Add(new TextParagraph("这是一个由 OFDRW.NET 生成的 OFD 文档")
                {
                    FontSize = 12,
                    FontFamily = "SimSun", 
                    Position = new Position(50, 80)
                });
                
                // 添加正文
                doc.Add(new TextParagraph("OFDRW.NET 是一个完整的 OFD 文档处理库，支持：")
                {
                    FontSize = 10,
                    Position = new Position(50, 120)
                });
                
                var features = new[]
                {
                    "• 文档创建和编辑",
                    "• 数字签名和验证", 
                    "• 格式转换 (OFD ↔ PDF)",
                    "• 图形绘制和布局",
                    "• 字体管理和映射"
                };
                
                var yPos = 140.0;
                foreach (var feature in features)
                {
                    doc.Add(new TextParagraph(feature)
                    {
                        FontSize = 9,
                        Position = new Position(70, yPos)
                    });
                    yPos += 20;
                }
                
                // 保存文档
                await doc.CloseAsync();
            }
            
            Console.WriteLine($"✓ 高级HelloWorld文档已创建: {Path.GetFullPath(outputPath2)}");
            
            // 显示文档信息
            await ShowDocumentInfoAsync(outputPath);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 创建文档时出错: {ex.Message}");
        }
        
        Console.WriteLine("=== HelloWorld 示例完成 ===\n");
    }
    
    /// <summary>
    /// 显示文档信息
    /// </summary>
    private static async Task ShowDocumentInfoAsync(string filePath)
    {
        try
        {
            Console.WriteLine($"\n文档信息:");
            Console.WriteLine($"  路径: {Path.GetFullPath(filePath)}");
            Console.WriteLine($"  大小: {new FileInfo(filePath).Length} 字节");
            
            var info = await OfdrwHelper.GetDocumentInfoAsync(filePath);
            Console.WriteLine($"  标题: {info.Title ?? "未设置"}");
            Console.WriteLine($"  作者: {info.Author ?? "OFDRW.NET"}");
            Console.WriteLine($"  页数: {info.PageCount}");
            
            var isValid = await OfdrwHelper.IsValidOfdDocumentAsync(filePath);
            Console.WriteLine($"  有效性: {(isValid ? "有效" : "无效")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取文档信息失败: {ex.Message}");
        }
    }
}