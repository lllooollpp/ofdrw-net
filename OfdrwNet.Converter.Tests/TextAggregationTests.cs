using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using OfdrwNet.Abstractions;
using OfdrwNet.Converter;
using OfdrwNet.Text.Pdf;
using Xunit;

namespace OfdrwNet.Converter.Tests;

public class TextAggregationTests
{
    private static OfdText Make(
        int page,
        string text,
        double x,
        double width,
        double fontSize,
        double y = 10,
        double height = 12,
        string fontFamily = "F")
        => new()
        {
            Page = page,
            Text = text,
            X = (float)x,
            Width = (float)width,
            FontSize = (float)fontSize,
            Y = (float)y,
            Height = (float)height,
            FontFamily = fontFamily
        };

    [Fact]
    public void EnglishLine_WordSplit_ShouldHaveMonotonicPositions()
    {
        var raw = new List<OfdText>
        {
            Make(1, "Hello", 10, 20, 12),
            Make(1, "World", 40, 24, 12)
        };

        var options = new ConvertHelper.PdfToOfdOptions
        {
            SplitTextBySpace = true,
            OnlySplitLatinWords = true,
            PerGlyphPositioning = false
        };

        var result = InvokeAggregateWords(raw, options);
        Assert.True(result.Count >= 2, "Should split into at least two words");

        double prevRight = -1;
        foreach (var word in result)
        {
            Assert.True(word.Width > 0, "Word width > 0");
            Assert.True(word.X >= prevRight, "Words should be ordered without overlap");
            prevRight = word.X + word.Width - 0.01;
        }
    }

    [Fact]
    public void ChineseLine_ShouldNotSplit_WhenOnlySplitLatin()
    {
        var raw = new List<OfdText>
        {
            Make(1, "中文测试", 10, 32, 12)
        };

        var options = new ConvertHelper.PdfToOfdOptions
        {
            SplitTextBySpace = true,
            OnlySplitLatinWords = true,
            PerGlyphPositioning = false
        };

        var result = InvokeAggregateWords(raw, options);
        Assert.Single(result); // 不拆分
    }

    [Fact]
    public void LargeGap_ShouldLimitSyntheticSpaces()
    {
        var raw = new List<OfdText>
        {
            Make(1, "A", 10, 5, 12),
            Make(1, "B", 100, 5, 12)
        };

        var options = new ConvertHelper.PdfToOfdOptions
        {
            SplitTextBySpace = true,
            OnlySplitLatinWords = true,
            PerGlyphPositioning = false,
            MaxSyntheticSpacesPerGap = 2
        };

        var result = InvokeAggregateWords(raw, options);
        Assert.True(result.Count <= 3, "Synthetic spaces limited");
    }

    private static List<OfdText> InvokeAggregateWords(
        List<OfdText> raw,
        ConvertHelper.PdfToOfdOptions options)
    {
        var extractorType = Type.GetType(
            "OfdrwNet.Text.Pdf.PdfTextExtractor, OfdrwNet.Text",
            throwOnError: true)!;

        var helperType = extractorType.GetNestedType(
            "TextAggregationHelper",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("TextAggregationHelper not found");

        var method = helperType.GetMethod(
            "AggregateWords",
            BindingFlags.Public | BindingFlags.Static);

        var textOptions = new PdfTextExtractionOptions
        {
            SplitTextBySpace = options.SplitTextBySpace,
            OnlySplitLatinWords = options.OnlySplitLatinWords,
            PerGlyphPositioning = options.PerGlyphPositioning,
            MaxSyntheticSpacesPerGap = options.MaxSyntheticSpacesPerGap,
            MinGapForSyntheticSpaceMm = options.MinGapForSyntheticSpaceMm,
            NumericGapMultiplier = options.NumericGapMultiplier,
            NumericMinGapMm = options.NumericMinGapMm,
            GapSpaceTriggerRatio = options.GapSpaceTriggerRatio,
            CjkGapTriggerRatio = options.CjkGapTriggerRatio,
            EnableDeltaX = options.EnableDeltaX,
            ExpandCjkWidth = options.ExpandCjkWidth,
            CjkExtraAdvanceRatio = options.CjkExtraAdvanceRatio,
            EnableDebugWordLayout = options.EnableDebugWordLayout,
            MaxNegativeKerningAbsorbMm = options.MaxNegativeKerningAbsorbMm
        };

        return (List<OfdText>)method!.Invoke(
            null,
            new object[] { raw, NullLogger.Instance, 1, textOptions })!;
    }
}
