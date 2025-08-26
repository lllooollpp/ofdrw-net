using OfdrwNet;

namespace OfdrwNet.Examples;

/// <summary>
/// 文档编辑示例
/// 演示如何编辑现有的 OFD 文档
/// </summary>
public class DocumentEditExample
{
    /// <summary>
    /// 运行文档编辑示例
    /// </summary>
    public static async Task RunAsync()
    {
        Console.WriteLine("=== 文档编辑示例 ===");
        
        // 输出目录
        var exampleDir = "Examples";
        Directory.CreateDirectory(exampleDir);
        
        var originalDoc = Path.Combine(exampleDir, "Original.ofd");
        var editedDoc = Path.Combine(exampleDir, "Edited.ofd");
        
        try
        {
            // 1. 创建原始文档
            Console.WriteLine("1. 创建原始文档...");
            await CreateOriginalDocumentAsync(originalDoc);
            
            // 2. 编辑文档
            Console.WriteLine("2. 编辑文档...");
            await EditDocumentAsync(originalDoc, editedDoc);
            
            // 3. 比较文档
            Console.WriteLine("3. 比较文档信息...");
            await CompareDocumentsAsync(originalDoc, editedDoc);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 文档编辑示例出错: {ex.Message}");
        }
        
        Console.WriteLine("=== 文档编辑示例完成 ===\n");
    }
    
    /// <summary>
    /// 创建原始文档
    /// </summary>
    private static async Task CreateOriginalDocumentAsync(string filePath)
    {
        using var doc = new OFDDoc(filePath);
        
        // 添加标题
        doc.Add(new TextParagraph("原始文档")
        {
            FontSize = 16,
            Position = new Position(50, 50)
        });
        
        // 添加内容
        doc.Add(new TextParagraph("这是原始文档的内容。")
        {
            FontSize = 12,
            Position = new Position(50, 80)
        });
        
        doc.Add(new TextParagraph("创建时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
        {
            FontSize = 10,
            Position = new Position(50, 110)
        });
        
        await doc.CloseAsync();
        Console.WriteLine($"  ✓ 原始文档已创建: {Path.GetFileName(filePath)}");
    }
    
    /// <summary>
    /// 编辑文档
    /// </summary>
    private static async Task EditDocumentAsync(string inputPath, string outputPath)
    {
        // 注意：由于我们的实现还是占位符，这里展示的是API的使用方法
        // 在实际实现中，这会真正编辑现有文档
        
        try
        {
            // 尝试编辑文档（当前为占位符实现）
            // using var doc = await OfdrwHelper.EditDocumentAsync(inputPath, outputPath);
            
            // 由于EditDocumentAsync还未完全实现，我们创建一个新文档来模拟编辑
            using var doc = new OFDDoc(outputPath);
            
            // 保留原始内容（在真实实现中会从原文档读取）
            doc.Add(new TextParagraph("原始文档")
            {
                FontSize = 16,
                Position = new Position(50, 50)
            });
            
            doc.Add(new TextParagraph("这是原始文档的内容。")
            {
                FontSize = 12,
                Position = new Position(50, 80)
            });
            
            doc.Add(new TextParagraph("创建时间: " + DateTime.Now.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss"))
            {
                FontSize = 10,
                Position = new Position(50, 110)
            });
            
            // 添加编辑内容
            doc.Add(new TextParagraph("--- 编辑内容 ---")
            {
                FontSize = 14,
                Position = new Position(50, 150)
            });
            
            doc.Add(new TextParagraph("这是后来添加的内容。")
            {
                FontSize = 12,
                Position = new Position(50, 180)
            });
            
            doc.Add(new TextParagraph("编辑时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            {
                FontSize = 10,
                Position = new Position(50, 210)
            });
            
            await doc.CloseAsync();
            Console.WriteLine($"  ✓ 文档编辑完成: {Path.GetFileName(outputPath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ 编辑文档失败: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// 比较文档
    /// </summary>
    private static async Task CompareDocumentsAsync(string originalPath, string editedPath)
    {
        try
        {
            var originalInfo = await OfdrwHelper.GetDocumentInfoAsync(originalPath);
            var editedInfo = await OfdrwHelper.GetDocumentInfoAsync(editedPath);
            var originalSize = new FileInfo(originalPath).Length;
            var editedSize = new FileInfo(editedPath).Length;
            
            Console.WriteLine($"  原始文档:");
            Console.WriteLine($"    文件大小: {originalSize} 字节");
            Console.WriteLine($"    页面数量: {originalInfo.PageCount}");
            
            Console.WriteLine($"  编辑后文档:");
            Console.WriteLine($"    文件大小: {editedSize} 字节");
            Console.WriteLine($"    页面数量: {editedInfo.PageCount}");
            Console.WriteLine($"    大小变化: {(editedSize > originalSize ? "+" : "")}{editedSize - originalSize} 字节");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ 比较文档失败: {ex.Message}");
        }
    }
}