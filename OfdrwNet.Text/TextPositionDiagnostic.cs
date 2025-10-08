using SkiaSharp;
using System.Drawing;
using System.Xml.Linq;

namespace OfdrwNet.Text;

/// <summary>
/// 文本位置诊断工具，用于分析和调试文本偏移问题
/// </summary>
public static class TextPositionDiagnostic
{
    /// <summary>
    /// 诊断文本对象的位置信息
    /// </summary>
    /// <param name="textObject">文本对象</param>
    /// <returns>诊断信息</returns>
    public static TextPositionInfo DiagnoseTextPosition(XElement textObject)
    {
        var boundary = TextRenderingUtils.ParseBoundary(textObject);
        var fontSize = TextRenderingUtils.GetFontSize(textObject);
        var textCodes = TextRenderingUtils.ExtractTextCodes(textObject);

        return new TextPositionInfo
        {
            Boundary = boundary,
            FontSize = fontSize,
            TextCodes = textCodes,
            RecommendedGdiOffset = CalculateRecommendedGdiOffset(fontSize),
            RecommendedSkiaOffset = CalculateRecommendedSkiaOffset(fontSize)
        };
    }

    /// <summary>
    /// 计算推荐的 GDI 偏移量
    /// </summary>
    /// <param name="fontSize">字体大小</param>
    /// <returns>推荐偏移量</returns>
    private static float CalculateRecommendedGdiOffset(float fontSize)
    {
        // 基于经验值的偏移计算
        return fontSize * 0.15f;
    }

    /// <summary>
    /// 计算推荐的 Skia 偏移量
    /// </summary>
    /// <param name="fontSize">字体大小</param>
    /// <returns>推荐偏移量</returns>
    private static float CalculateRecommendedSkiaOffset(float fontSize)
    {
        // 基于经验值的偏移计算
        return fontSize * 0.75f;
    }

    /// <summary>
    /// 验证文本位置是否合理
    /// </summary>
    /// <param name="info">文本位置信息</param>
    /// <returns>验证结果</returns>
    public static PositionValidationResult ValidatePosition(TextPositionInfo info)
    {
        var result = new PositionValidationResult();

        // 检查边界是否合理
        if (info.Boundary.IsEmpty)
        {
            result.Issues.Add("文本对象边界为空");
        }

        if (info.Boundary.Width <= 0 || info.Boundary.Height <= 0)
        {
            result.Issues.Add("文本对象边界尺寸无效");
        }

        // 检查字体大小是否合理
        if (info.FontSize <= 0)
        {
            result.Issues.Add("字体大小无效");
        }
        else if (info.FontSize > info.Boundary.Height * 2)
        {
            result.Issues.Add("字体大小相对于边界高度过大，可能存在缩放问题");
        }

        // 检查文本代码位置
        foreach (var textCode in info.TextCodes)
        {
            if (textCode.X < 0 || textCode.Y < 0)
            {
                result.Issues.Add($"文本代码位置为负数: X={textCode.X}, Y={textCode.Y}");
            }

            if (textCode.X > info.Boundary.Width || textCode.Y > info.Boundary.Height)
            {
                result.Issues.Add($"文本代码位置超出边界: X={textCode.X}, Y={textCode.Y}");
            }
        }

        result.IsValid = result.Issues.Count == 0;
        return result;
    }

    /// <summary>
    /// 生成位置调试报告
    /// </summary>
    /// <param name="textObject">文本对象</param>
    /// <returns>调试报告</returns>
    public static string GenerateDebugReport(XElement textObject)
    {
        var info = DiagnoseTextPosition(textObject);
        var validation = ValidatePosition(info);

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== 文本位置诊断报告 ===");
        report.AppendLine($"边界: X={info.Boundary.X:F2}, Y={info.Boundary.Y:F2}, W={info.Boundary.Width:F2}, H={info.Boundary.Height:F2}");
        report.AppendLine($"字体大小: {info.FontSize:F2}");
        report.AppendLine($"文本代码数量: {info.TextCodes.Count}");
        report.AppendLine();

        report.AppendLine("=== 推荐偏移量 ===");
        report.AppendLine($"GDI 推荐偏移: {info.RecommendedGdiOffset:F2}");
        report.AppendLine($"Skia 推荐偏移: {info.RecommendedSkiaOffset:F2}");
        report.AppendLine();

        report.AppendLine("=== 文本代码详情 ===");
        for (int i = 0; i < info.TextCodes.Count; i++)
        {
            var tc = info.TextCodes[i];
            report.AppendLine($"  [{i}] X={tc.X:F2}, Y={tc.Y:F2}, Text='{tc.Text}'");

            // 计算调整后的位置
            var adjustedGdiY = tc.Y - info.RecommendedGdiOffset;
            var adjustedSkiaY = tc.Y + info.RecommendedSkiaOffset;
            report.AppendLine($"       GDI调整后Y={adjustedGdiY:F2}, Skia调整后Y={adjustedSkiaY:F2}");
        }
        report.AppendLine();

        report.AppendLine("=== 验证结果 ===");
        report.AppendLine($"验证状态: {(validation.IsValid ? "通过" : "有问题")}");
        if (!validation.IsValid)
        {
            report.AppendLine("发现的问题:");
            foreach (var issue in validation.Issues)
            {
                report.AppendLine($"  - {issue}");
            }
        }

        return report.ToString();
    }
}

/// <summary>
/// 文本位置信息
/// </summary>
public class TextPositionInfo
{
    public RectangleF Boundary { get; set; }
    public float FontSize { get; set; }
    public List<TextCodeInfo> TextCodes { get; set; } = new();
    public float RecommendedGdiOffset { get; set; }
    public float RecommendedSkiaOffset { get; set; }
}

/// <summary>
/// 位置验证结果
/// </summary>
public class PositionValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Issues { get; set; } = new();
}
