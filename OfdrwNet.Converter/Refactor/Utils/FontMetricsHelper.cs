using System;
using System.Collections.Generic;
using System.Linq;
using iText.Kernel.Pdf;
using iText.Kernel.Font; // PdfFont
using iText.Kernel.Pdf.Canvas.Parser.Data;
using OfdrwNet.Abstractions;
using Microsoft.Extensions.Logging;

namespace OfdrwNet.Converter.Refactor.Utils;

/// <summary>
/// 提供从 iText TextRenderInfo/PdfFont 中提取真实 glyph / kerning / space 度量并进行宽度与行高估算的辅助。
/// 所有 *Pt 以 point 为单位；调用端自行乘 ConvertHelper.Pt2Mm。
/// </summary>
internal static class FontMetricsHelper
{
    internal record RunMetrics(double RunAdvancePt, double[] CharAdvancesPt, double AvgAdvancePt, double SpaceWidthPt, bool IsCjk, double LineHeightPt, double KerningTotalPt);

    /// <summary>
    /// 主入口：为一个 renderInfo 计算字形进宽等。
    /// </summary>
    internal static RunMetrics ComputeRunMetrics(TextRenderInfo render, PdfFont font)
    {
        string text = render.GetText() ?? string.Empty;
        double fontSizePt = render.GetFontSize();
        double hScale = SafeHorizontalScale(render);
        var fp = font.GetFontProgram();
        double avgWidth1000 = fp?.GetAvgWidth() ?? 500; // fallback 500
        double avgAdvancePtBase = avgWidth1000 / 1000d * fontSizePt * hScale;
        if (avgAdvancePtBase <= 0) avgAdvancePtBase = fontSizePt * 0.5 * hScale;

        var charsInfo = render.GetCharacterRenderInfos();
        var advances = new double[text.Length];
        double kerningTotal = 0d;
        bool anyCjk = false;

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            bool isCjk = IsCjk(ch);
            anyCjk |= isCjk;
            double advPt = 0;
            var glyph = fp?.GetGlyph(ch);
            if (glyph != null && glyph.GetWidth() > 0)
            {
                advPt = glyph.GetWidth() / 1000d * fontSizePt * hScale; // glyph advance
            }
            else if (ch == ' ')
            {
                advPt = GetSingleSpaceWidthPt(render, fontSizePt, hScale, avgAdvancePtBase);
            }
            else if (isCjk)
            {
                advPt = fontSizePt * hScale; // 全宽假设
            }
            else
            {
                advPt = avgAdvancePtBase;
            }
            advances[i] = advPt;

            // kerning with next
            if (i < text.Length - 1 && glyph != null && fp != null)
            {
                var nextGlyph = fp.GetGlyph(text[i + 1]);
                if (nextGlyph != null)
                {
                    int kern = 0; // 若 API 支持 kerning，可在此接入；当前版本可能不暴露 -> 保持 0
                    if (kern != 0)
                    {
                        double kernPt = kern / 1000d * fontSizePt * hScale;
                        kerningTotal += kernPt;
                        advances[i] += kernPt; // 把 kerning 计入当前字符 advance（简化）
                    }
                }
            }
        }

        double runAdvancePt = advances.Sum();

        // 行高（TypoAsc + |TypoDesc|）
        // 行高：iText.NET 未公开 TypoAsc/Desc 时使用 ascent line & descent line 近似，若失败则 fallback
        double lineHeightPt = fontSizePt * 1.2; // 默认
        try
        {
            var ascent = render.GetAscentLine();
            var descent = render.GetDescentLine();
            double h = ascent.GetStartPoint().Get(1) - descent.GetStartPoint().Get(1);
            if (h > 0 && h < fontSizePt * 3) lineHeightPt = h; // 合理范围过滤
        }
        catch { /* ignore */ }

        // space width: 如果文本中包含空格，取第一个空格 advance；否则推算
        double spaceWidthPt = ExtractSpaceWidthFromText(text, advances, fontSizePt, hScale, avgAdvancePtBase);
        if (spaceWidthPt <= 0) spaceWidthPt = GetSingleSpaceWidthPt(render, fontSizePt, hScale, avgAdvancePtBase);

        double avgAdvPt = advances.Length > 0 ? advances.Average() : avgAdvancePtBase;

        return new RunMetrics(runAdvancePt, advances, avgAdvPt, spaceWidthPt, anyCjk, lineHeightPt, kerningTotal);
    }

    private static double SafeHorizontalScale(TextRenderInfo render)
    {
        try { return render.GetHorizontalScaling(); } catch { return 1.0; }
    }

    private static double GetSingleSpaceWidthPt(TextRenderInfo render, double fontSizePt, double hScale, double avgAdvancePtBase)
    {
        try
        {
            double w = render.GetSingleSpaceWidth(); // 用户空间 -> 可能已是 pt；如异常过大/过小做校正
            if (w <= 0) return avgAdvancePtBase;
            // 简单异常过滤：如果大于 5 * fontSize 说明单位不同或异常，回退
            if (w > fontSizePt * 5) return avgAdvancePtBase;
            return w * hScale; // 如果本身已含 hScale 这里会稍有重复风险，可后续检测
        }
        catch { return avgAdvancePtBase; }
    }

    private static double ExtractSpaceWidthFromText(string text, double[] advances, double fontSizePt, double hScale, double avgAdvancePtBase)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ') return advances[i];
        }
        return 0;
    }

    internal static bool IsCjk(char ch)
        => (ch >= '\u4E00' && ch <= '\u9FFF') || (ch >= '\u3400' && ch <= '\u4DBF');

    /// <summary>
    /// 宽度归一：修正极端偏小/偏大值。
    /// </summary>
    internal static double SanitizeWidthPt(double rawWidthPt, int charCount, bool isCjk, double fontSizePt, double hScale, double avgAdvancePt)
    {
        if (charCount <= 0) return rawWidthPt;
        double minExpected = fontSizePt * 0.35 * charCount * hScale;
        if (isCjk) minExpected = fontSizePt * 0.85 * charCount * hScale; // CJK 期待更接近满宽
        if (rawWidthPt < minExpected)
            rawWidthPt = Math.Max(minExpected, avgAdvancePt * charCount * 0.9);

        double maxExpected = fontSizePt * 2.5 * charCount * hScale;
        if (rawWidthPt > maxExpected)
            rawWidthPt = fontSizePt * charCount * hScale; // 回退
        return rawWidthPt;
    }
}
