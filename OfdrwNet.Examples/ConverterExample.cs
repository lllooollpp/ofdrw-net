using OfdrwNet.Converter.Export;
using OfdrwNet.Converter.OfdConverter;
using OfdrwNet.Layout;
using SkiaSharp;

namespace OfdrwNet.Examples;

/// <summary>
/// 转换器示例
/// 展示OFD格式转换功能的使用方法
/// </summary>
public static class ConverterExample
{
    /// <summary>
    /// 运行转换器示例
    /// </summary>
    /// <param name="workingDir">工作目录</param>
    public static async Task RunAsync(string workingDir)
    {
        Console.WriteLine("开始转换器功能示例...");
        Console.WriteLine();

        try
        {
            // 确保工作目录存在
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            // 1. 文本转OFD示例
            await TextToOfdExample(workingDir);
            
            // 2. 图像转OFD示例
            await ImageToOfdExample(workingDir);

            // 3. OFD转图像示例
            await OfdToImageExample(workingDir);

            // 4. OFD转文本示例
            await OfdToTextExample(workingDir);

            // 5. OFD转PDF示例
            await OfdToPdfExample(workingDir);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"转换器示例执行失败: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("转换器功能示例完成！");
    }

    /// <summary>
    /// 文本转OFD示例
    /// </summary>
    private static async Task TextToOfdExample(string workingDir)
    {
        Console.WriteLine("=== 文本转OFD示例 ===");

        // 创建示例文本文件
        var textContent = @"OfdrwNet 转换器示例文档

这是一个演示文本到OFD转换功能的示例文档。

主要特性：
1. 支持纯文本转换
2. 自动分页处理
3. 可配置字体和布局
4. 支持中英文混合内容

技术特点：
• 基于.NET 8开发
• 跨平台兼容
• 高性能转换
• 易于集成

测试内容包含：
- 中文字符：你好世界
- 英文字符：Hello World  
- 数字符号：0123456789
- 特殊符号：！@#￥%……&*（）

这段文本用于测试转换器的文本处理能力，包括：
换行处理、字符编码、页面布局等功能。";

        var textFile = Path.Combine(workingDir, "sample.txt");
        await File.WriteAllTextAsync(textFile, textContent, System.Text.Encoding.UTF8);

        // 转换为OFD
        var ofdOutput = Path.Combine(workingDir, "text_converted.ofd");
        
        using (var converter = new TextConverter(ofdOutput))
        {
            converter.SetFontSize(3.5)
                    .SetLineSpacing(1.8)
                    .SetMargin(25)
                    .SetPageLayout(OfdrwNet.Layout.PageLayout.A4());
            
            await converter.ConvertAsync(textFile);
        }

        Console.WriteLine($"文本转换完成: {ofdOutput}");
        Console.WriteLine();
    }

    /// <summary>
    /// 图像转OFD示例
    /// </summary>
    private static async Task ImageToOfdExample(string workingDir)
    {
        Console.WriteLine("=== 图像转OFD示例 ===");

        // 创建示例图像
        var imageFile = Path.Combine(workingDir, "sample_image.png");
        await CreateSampleImage(imageFile);

        // 转换为OFD
        var ofdOutput = Path.Combine(workingDir, "image_converted.ofd");
        
        using (var converter = new ImageConverter(ofdOutput))
        {
            converter.SetAutoFitToPage(true)
                    .SetMaintainAspectRatio(true)
                    .SetMargin(15)
                    .SetPageLayout(OfdrwNet.Layout.PageLayout.A4());
            
            await converter.ConvertAsync(imageFile);
        }

        Console.WriteLine($"图像转换完成: {ofdOutput}");
        Console.WriteLine();
    }

    /// <summary>
    /// OFD转图像示例
    /// </summary>
    private static async Task OfdToImageExample(string workingDir)
    {
        Console.WriteLine("=== OFD转图像示例 ===");

        var ofdFile = Path.Combine(workingDir, "text_converted.ofd");
        if (!File.Exists(ofdFile))
        {
            Console.WriteLine("未找到源OFD文件，跳过图像导出示例");
            return;
        }

        var imageOutputDir = Path.Combine(workingDir, "exported_images");

        try
        {
            // 导出为PNG图像
            using (var exporter = new ImageExporter(ofdFile, imageOutputDir, SKEncodedImageFormat.Png, 100, 200f))
            {
                await exporter.ExportAsync();
                var exportedFiles = exporter.GetOutputPaths();
                
                Console.WriteLine($"导出了 {exportedFiles.Count} 个PNG图像文件:");
                foreach (var file in exportedFiles)
                {
                    Console.WriteLine($"  - {Path.GetFileName(file)}");
                }
            }

            Console.WriteLine();

            // 导出为JPEG图像（仅第一页）
            using (var exporter = new ImageExporter(ofdFile, imageOutputDir, SKEncodedImageFormat.Jpeg, 90, 150f))
            {
                await exporter.ExportAsync(0); // 只导出第一页
                Console.WriteLine("JPEG图像导出完成（仅第一页）");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"图像导出失败: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// OFD转文本示例
    /// </summary>
    private static async Task OfdToTextExample(string workingDir)
    {
        Console.WriteLine("=== OFD转文本示例 ===");

        var ofdFile = Path.Combine(workingDir, "text_converted.ofd");
        if (!File.Exists(ofdFile))
        {
            Console.WriteLine("未找到源OFD文件，跳过文本导出示例");
            return;
        }

        var textOutput = Path.Combine(workingDir, "extracted_text.txt");

        try
        {
            using (var exporter = new TextExporter(ofdFile, textOutput))
            {
                await exporter.ExportAsync();
            }

            Console.WriteLine($"文本导出完成: {textOutput}");
            
            // 显示提取的文本内容（前200个字符）
            if (File.Exists(textOutput))
            {
                var extractedText = await File.ReadAllTextAsync(textOutput);
                var preview = extractedText.Length > 200 
                    ? extractedText.Substring(0, 200) + "..."
                    : extractedText;
                
                Console.WriteLine("提取的文本内容预览:");
                Console.WriteLine(preview);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"文本导出失败: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// OFD转PDF示例
    /// </summary>
    private static async Task OfdToPdfExample(string workingDir)
    {
        Console.WriteLine("=== OFD转PDF示例 ===");

        var ofdFile = Path.Combine(workingDir, "text_converted.ofd");
        if (!File.Exists(ofdFile))
        {
            Console.WriteLine("未找到源OFD文件，跳过PDF导出示例");
            return;
        }

        var pdfOutput = Path.Combine(workingDir, "exported_document.pdf");

        try
        {
            using (var exporter = new PDFExporter(ofdFile, pdfOutput, 150f))
            {
                await exporter.ExportAsync();
            }

            Console.WriteLine($"PDF导出完成: {pdfOutput}");
            
            if (File.Exists(pdfOutput))
            {
                var fileInfo = new FileInfo(pdfOutput);
                Console.WriteLine($"PDF文件大小: {fileInfo.Length} 字节");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PDF导出失败: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 创建示例图像
    /// </summary>
    private static async Task CreateSampleImage(string imagePath)
    {
        const int width = 800;
        const int height = 600;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        using var canvas = surface.Canvas;

        // 清除背景
        canvas.Clear(SKColors.White);

        // 绘制渐变背景
        using var paint = new SKPaint();
        var colors = new[] { SKColors.LightBlue, SKColors.LightGreen };
        var positions = new float[] { 0f, 1f };
        
        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), 
            new SKPoint(width, height), 
            colors, 
            positions, 
            SKShaderTileMode.Clamp);
        
        paint.Shader = shader;
        canvas.DrawRect(0, 0, width, height, paint);

        // 绘制文本
        paint.Shader = null;
        paint.Color = SKColors.DarkBlue;
        paint.TextSize = 48;
        paint.IsAntialias = true;
        paint.Typeface = SKTypeface.Default;

        var text = "OfdrwNet 示例图像";
        var textBounds = new SKRect();
        paint.MeasureText(text, ref textBounds);
        
        var textX = (width - textBounds.Width) / 2;
        var textY = height / 2 - textBounds.Top / 2;
        canvas.DrawText(text, textX, textY, paint);

        // 绘制几何图形
        paint.Color = SKColors.Red;
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 5;
        canvas.DrawCircle(width / 4, height * 3 / 4, 50, paint);

        paint.Color = SKColors.Green;
        paint.Style = SKPaintStyle.Fill;
        canvas.DrawRect(width * 3 / 4 - 50, height * 3 / 4 - 50, 100, 100, paint);

        // 保存图像
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        
        await File.WriteAllBytesAsync(imagePath, data.ToArray());
    }
}