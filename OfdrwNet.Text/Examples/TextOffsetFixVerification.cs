using OfdrwNet.Text;
using OfdrwNet.Text.Renderers;
using System.Drawing;
using System.Xml.Linq;
using SkiaSharp;

namespace OfdrwNet.Text.Examples;

/// <summary>
/// 文本偏移修复验证示例
/// </summary>
public class TextOffsetFixVerification
{
    /// <summary>
    /// 验证 GDI 文本渲染器的修复
    /// </summary>
    public static void VerifyGdiTextRenderer()
    {
        Console.WriteLine("=== 验证 GDI 文本渲染器修复 ===");

        // 创建测试用的文本对象 XML
        var textObjectXml = CreateSampleTextObjectXml();
        var textObject = XElement.Parse(textObjectXml);

        // 生成诊断报告
        var report = TextPositionDiagnostic.GenerateDebugReport(textObject);
        Console.WriteLine(report);

        // 使用 GDI 渲染器测试
        using var bitmap = new Bitmap(400, 200);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);

        var gdiRenderer = new GdiTextRenderer();

        try
        {
            // 渲染文本（使用修复后的代码）
            var renderTask = gdiRenderer.RenderTextObjectAsync(graphics, textObject);
            renderTask.Wait();

            // 保存测试图像
            var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "gdi_text_render_test.png");
            bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
            Console.WriteLine($"GDI 渲染测试图像已保存到: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GDI 渲染测试失败: {ex.Message}");
        }
        finally
        {
            gdiRenderer.Dispose();
        }
    }

    /// <summary>
    /// 验证 Skia 文本渲染器的修复
    /// </summary>
    public static void VerifySkiaTextRenderer()
    {
        Console.WriteLine("\n=== 验证 Skia 文本渲染器修复 ===");

        // 创建测试用的文本对象 XML
        var textObjectXml = CreateSampleTextObjectXml();
        var textObject = XElement.Parse(textObjectXml);

        // 生成诊断报告
        var report = TextPositionDiagnostic.GenerateDebugReport(textObject);
        Console.WriteLine(report);

        // 使用 Skia 渲染器测试
        var imageInfo = new SKImageInfo(400, 200);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var skiaRenderer = new SkiaTextRenderer();

        try
        {
            // 渲染文本（使用修复后的代码）
            var renderTask = skiaRenderer.RenderTextObjectAsync(canvas, textObject);
            renderTask.Wait();

            // 保存测试图像
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "skia_text_render_test.png");

            using var stream = File.OpenWrite(outputPath);
            data.SaveTo(stream);

            Console.WriteLine($"Skia 渲染测试图像已保存到: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Skia 渲染测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 对比修复前后的效果
    /// </summary>
    public static void CompareBeforeAfterFix()
    {
        Console.WriteLine("\n=== 对比修复前后效果 ===");

        var textObjectXml = CreateSampleTextObjectXml();
        var textObject = XElement.Parse(textObjectXml);

        // 模拟修复前的渲染（不使用基线校正）
        Console.WriteLine("模拟修复前的渲染位置:");
        var boundary = TextRenderingUtils.ParseBoundary(textObject);
        var textCodes = TextRenderingUtils.ExtractTextCodes(textObject);

        foreach (var textCode in textCodes)
        {
            var oldX = boundary.X + textCode.X;
            var oldY = boundary.Y + textCode.Y;
            Console.WriteLine($"  修复前: X={oldX:F2}, Y={oldY:F2}, Text='{textCode.Text}'");
        }

        // 显示修复后的渲染位置
        Console.WriteLine("\n修复后的渲染位置:");
        var fontSize = TextRenderingUtils.GetFontSize(textObject);
        var gdiOffset = fontSize * 0.15f;
        var skiaOffset = fontSize * 0.75f;

        foreach (var textCode in textCodes)
        {
            var newGdiX = boundary.X + textCode.X;
            var newGdiY = boundary.Y + textCode.Y - gdiOffset;
            var newSkiaX = boundary.X + textCode.X;
            var newSkiaY = boundary.Y + textCode.Y + skiaOffset;

            Console.WriteLine($"  GDI修复后: X={newGdiX:F2}, Y={newGdiY:F2}, Text='{textCode.Text}'");
            Console.WriteLine($"  Skia修复后: X={newSkiaX:F2}, Y={newSkiaY:F2}, Text='{textCode.Text}'");
        }
    }

    /// <summary>
    /// 创建示例文本对象 XML
    /// </summary>
    /// <returns>文本对象 XML 字符串</returns>
    private static string CreateSampleTextObjectXml()
    {
        return @"<TextObject Boundary=""50 50 300 100"" Size=""16"" Font=""SimSun"">
            <TextCode X=""0"" Y=""20"">测试文本第一行</TextCode>
            <TextCode X=""0"" Y=""50"">Test text second line</TextCode>
            <TextCode X=""0"" Y=""80"">第三行混合文本ABC123</TextCode>
        </TextObject>";
    }

    /// <summary>
    /// 运行所有验证测试
    /// </summary>
    public static void RunAllVerificationTests()
    {
        try
        {
            Console.WriteLine("开始文本偏移修复验证...\n");

            // 对比修复前后效果
            CompareBeforeAfterFix();

            // 验证 GDI 渲染器
            VerifyGdiTextRenderer();

            // 验证 Skia 渲染器
            VerifySkiaTextRenderer();

            Console.WriteLine("\n=== 验证完成 ===");
            Console.WriteLine("请检查生成的图像文件，确认文本位置是否正确。");
            Console.WriteLine("如果文本仍然偏移，您可以调整基线校正系数:");
            Console.WriteLine("- GDI: 修改 CalculateBaselineOffset 方法中的 0.15f 系数");
            Console.WriteLine("- Skia: 修改 CalculateSkiaBaselineOffset 方法中的 0.8f 系数");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"验证过程中发生错误: {ex.Message}");
            Console.WriteLine($"详细信息: {ex}");
        }
    }
}
