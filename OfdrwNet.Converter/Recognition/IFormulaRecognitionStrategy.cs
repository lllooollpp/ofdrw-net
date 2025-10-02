using System;
using OfdrwNet.Converter.Domain;

namespace OfdrwNet.Converter.Recognition;

/// <summary>
/// 公式识别策略接口。
/// </summary>
/// <remarks>
/// 负责从PDF页面内容中识别数学公式并生成LaTeX表示。
/// 支持:
/// - 数学符号检测(希腊字母、运算符)
/// - 结构分析(上标、下标、分数、积分)
/// - LaTeX生成
/// - 置信度评分
/// - 文本回退(低置信度时)
///
/// 性能要求 (DR-7~DR-8):
/// - 字符级召回率 ≥ 95%
/// - LaTeX结构准确率 ≥ 88%
/// - 单页处理时间 < 200ms
/// </remarks>
public interface IFormulaRecognitionStrategy
{
    /// <summary>
    /// 识别页面中的公式。
    /// </summary>
    /// <param name="page">页面上下文(包含文本对象和布局信息)</param>
    /// <param name="options">识别选项(置信度阈值等)</param>
    /// <returns>识别结果(LaTeX表示或文本回退)</returns>
    FormulaRecognitionResult Recognize(PageContext page, FormulaRecognitionOptions options);
}

/// <summary>
/// 公式识别选项。
/// </summary>
public sealed class FormulaRecognitionOptions
{
    /// <summary>
    /// 置信度阈值(0.0-1.0)。
    /// 低于此阈值时回退到纯文本。
    /// </summary>
    /// <remarks>
    /// 默认值: 0.8 (参见 spec.md --formula-recog-threshold)
    /// </remarks>
    public float ConfidenceThreshold { get; init; } = 0.8f;

    /// <summary>
    /// 最小符号密度(0.0-1.0)。
    /// 低于此值时直接拒绝识别。
    /// </summary>
    public float MinSymbolDensity { get; init; } = 0.1f;

    /// <summary>
    /// 启用结构分析(上标、下标、分数等)。
    /// </summary>
    public bool EnableStructureAnalysis { get; init; } = true;
}

/// <summary>
/// 公式识别结果。
/// </summary>
public sealed class FormulaRecognitionResult
{
    /// <summary>
    /// 识别是否成功(置信度超过阈值)。
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// 识别置信度(0.0-1.0)。
    /// </summary>
    /// <remarks>
    /// 计算方式: 符号密度 × 0.6 + 结构质量 × 0.4
    /// </remarks>
    public required float Confidence { get; init; }

    /// <summary>
    /// LaTeX表示(成功时非空,失败时为null)。
    /// </summary>
    /// <remarks>
    /// 示例: "$E = mc^{2}$", "$\\frac{a}{b}$"
    /// </remarks>
    public string? LaTeX { get; init; }

    /// <summary>
    /// 回退文本(当LaTeX为null时使用)。
    /// </summary>
    public string? FallbackText { get; init; }

    /// <summary>
    /// 检测到的数学符号数量。
    /// </summary>
    public int DetectedSymbols { get; init; }

    /// <summary>
    /// 总字符数量。
    /// </summary>
    public int TotalCharacters { get; init; }
}
