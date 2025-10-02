using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;

namespace OfdrwNet.Converter.Layout;

/// <summary>
/// 布局特性检测器，检测复杂中文排版特性（竖排文字、Ruby注音等）。
/// </summary>
/// <remarks>
/// 功能需求 (FR-9):
/// - 竖排文字检测: 识别从右至左或从左至右的竖排文本
/// - Ruby注音检测: 识别汉字上方或旁边的拼音/假名标注
/// - 标点符号规则: 检测特殊标点符号排版（引号、括号等竖排方向）
///
/// 实现策略:
/// - 初版: 占位实现，检测到特殊排版时记录日志警告
/// - 后续: 可扩展为完整的排版分析引擎
///
/// 性能要求:
/// - 单页检测时间 < 100ms
/// - 不阻塞主转换流程
/// </remarks>
public sealed class LayoutFeaturesDetector
{
    private readonly ILogger<LayoutFeaturesDetector> _logger;

    // 常见竖排标点符号（需要旋转方向）
    private static readonly HashSet<char> VerticalPunctuation = new()
    {
        '「', '」', '『', '』',  // 日文引号
        '（', '）', '【', '】',  // 全角括号
        '〈', '〉', '《', '》',  // 书名号
        '｛', '｝', '［', '］',  // 全角大括号
        '…', '—',              // 省略号、破折号
    };

    // Ruby注音常见模式（拼音或假名在汉字上方）
    private static readonly Regex RubyPattern = new(
        @"[\p{IsHiragana}\p{IsKatakana}ぁ-んァ-ヶー]+",
        RegexOptions.Compiled);

    // 汉字字符范围
    private static readonly Regex HanziPattern = new(
        @"[\u4E00-\u9FFF]+",
        RegexOptions.Compiled);

    /// <summary>
    /// 初始化 LayoutFeaturesDetector 实例。
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public LayoutFeaturesDetector(ILogger<LayoutFeaturesDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 检测页面中的复杂布局特性。
    /// </summary>
    /// <param name="page">页面上下文</param>
    /// <returns>检测结果</returns>
    public LayoutFeatureResult DetectFeatures(PageContext page)
    {
        if (page == null)
        {
            throw new ArgumentNullException(nameof(page));
        }

        var startTime = DateTime.UtcNow;
        var result = new LayoutFeatureResult
        {
            PageNumber = page.PageNumber
        };

        try
        {
            // 1. 检测竖排文字
            result.HasVerticalText = DetectVerticalText(page);

            // 2. 检测Ruby注音
            result.HasRubyAnnotation = DetectRubyAnnotation(page);

            // 3. 检测特殊标点
            result.HasSpecialPunctuation = DetectSpecialPunctuation(page);

            // 4. 记录检测到的特性
            LogDetectedFeatures(result);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogDebug(
                "Layout feature detection completed for page {PageNumber} in {Elapsed:F1}ms",
                page.PageNumber, elapsed);

            if (elapsed > 100)
            {
                _logger.LogWarning(
                    "Layout feature detection exceeded 100ms threshold: page {PageNumber} took {Elapsed:F1}ms",
                    page.PageNumber, elapsed);
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Layout feature detection failed for page {PageNumber}", page.PageNumber);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 检测竖排文字。
    /// </summary>
    /// <param name="page">页面上下文</param>
    /// <returns>是否检测到竖排文字</returns>
    private bool DetectVerticalText(PageContext page)
    {
        // 策略: 分析文本对象的排列方向
        // 初版: 简单检测文本块的高度/宽度比
        // 竖排文本通常高度 > 宽度 * 2

        var textObjects = page.SourceObjects
            .Where(obj => obj != null)
            .ToList();

        if (!textObjects.Any())
        {
            return false;
        }

        // 简化实现: 检查文本内容是否包含大量竖排标点
        foreach (var obj in textObjects)
        {
            var text = obj.ToString() ?? string.Empty;
            if (ContainsVerticalPunctuation(text))
            {
                _logger.LogDebug(
                    "Detected potential vertical punctuation on page {PageNumber}: {Sample}",
                    page.PageNumber,
                    text.Length > 50 ? text.Substring(0, 47) + "..." : text);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检测Ruby注音。
    /// </summary>
    /// <param name="page">页面上下文</param>
    /// <returns>是否检测到Ruby注音</returns>
    private bool DetectRubyAnnotation(PageContext page)
    {
        // 策略: 检测小字号假名/拼音紧邻大字号汉字的模式
        // 初版: 简单检测是否同时包含汉字和假名

        var allText = string.Join(" ", page.SourceObjects
            .Where(obj => obj != null)
            .Select(obj => obj.ToString() ?? string.Empty));

        if (string.IsNullOrWhiteSpace(allText))
        {
            return false;
        }

        // 检测是否同时包含汉字和假名
        var hasHanzi = HanziPattern.IsMatch(allText);
        var hasKana = RubyPattern.IsMatch(allText);

        if (hasHanzi && hasKana)
        {
            _logger.LogDebug(
                "Detected potential Ruby annotation on page {PageNumber} (contains both Hanzi and Kana)",
                page.PageNumber);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检测特殊标点符号。
    /// </summary>
    /// <param name="page">页面上下文</param>
    /// <returns>是否检测到特殊标点</returns>
    private bool DetectSpecialPunctuation(PageContext page)
    {
        var allText = string.Join("", page.SourceObjects
            .Where(obj => obj != null)
            .Select(obj => obj.ToString() ?? string.Empty));

        return ContainsVerticalPunctuation(allText);
    }

    /// <summary>
    /// 检查文本是否包含竖排标点符号。
    /// </summary>
    private bool ContainsVerticalPunctuation(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return text.Any(c => VerticalPunctuation.Contains(c));
    }

    /// <summary>
    /// 记录检测到的布局特性。
    /// </summary>
    private void LogDetectedFeatures(LayoutFeatureResult result)
    {
        var features = new List<string>();

        if (result.HasVerticalText)
        {
            features.Add("竖排文字");
        }

        if (result.HasRubyAnnotation)
        {
            features.Add("Ruby注音");
        }

        if (result.HasSpecialPunctuation)
        {
            features.Add("特殊标点");
        }

        if (features.Any())
        {
            _logger.LogInformation(
                "页面 {PageNumber} 检测到复杂排版特性: {Features} (注意: 当前版本仅检测不转换)",
                result.PageNumber,
                string.Join(", ", features));

            _logger.LogWarning(
                "页面 {PageNumber} 包含不支持的排版特性: {Features}。转换后可能丢失语义信息。" +
                "建议: 使用专业排版工具手动调整，或等待后续版本支持。",
                result.PageNumber,
                string.Join(", ", features));
        }
    }

    /// <summary>
    /// 生成排版提示信息（供报告使用）。
    /// </summary>
    /// <param name="result">检测结果</param>
    /// <returns>提示信息</returns>
    public string GenerateHints(LayoutFeatureResult result)
    {
        if (result == null || !result.Success)
        {
            return string.Empty;
        }

        var hints = new List<string>();

        if (result.HasVerticalText)
        {
            hints.Add("• 竖排文字: 已检测到，但转换为横排文本。建议手动检查排版一致性。");
        }

        if (result.HasRubyAnnotation)
        {
            hints.Add("• Ruby注音: 已检测到，但未转换为OFD Ruby结构。注音信息可能丢失。");
        }

        if (result.HasSpecialPunctuation)
        {
            hints.Add("• 特殊标点: 已检测到竖排标点，但未调整方向。标点显示可能不符合竖排规范。");
        }

        if (!hints.Any())
        {
            return string.Empty;
        }

        return $"页面 {result.PageNumber} 排版提示:\n" + string.Join("\n", hints);
    }
}

/// <summary>
/// 布局特性检测结果。
/// </summary>
public sealed class LayoutFeatureResult
{
    /// <summary>
    /// 页码。
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// 检测是否成功。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息（如果失败）。
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 是否包含竖排文字。
    /// </summary>
    public bool HasVerticalText { get; set; }

    /// <summary>
    /// 是否包含Ruby注音。
    /// </summary>
    public bool HasRubyAnnotation { get; set; }

    /// <summary>
    /// 是否包含特殊标点符号。
    /// </summary>
    public bool HasSpecialPunctuation { get; set; }

    /// <summary>
    /// 是否检测到任何复杂排版特性。
    /// </summary>
    public bool HasAnyComplexFeature => HasVerticalText || HasRubyAnnotation || HasSpecialPunctuation;
}

