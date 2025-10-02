using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions;

namespace OfdrwNet.Converter.Refactor;

/// <summary>
/// 专责：从 PDF 中提取字体（嵌入或系统回退）并注册到 OFD 文档。
/// 拆分自 ConvertHelper.PdfToOfdAsync 内部逻辑，保持行为一致。
/// </summary>
internal class FontExtractor : IPdfContentExtractor
{
    private readonly System.Text.RegularExpressions.Regex _subsetPrefixRegex = new(@"^[A-Z]{6}\+");
    private readonly Dictionary<string, string?> _fontFileTempMap = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string?> ExtractedFonts => _fontFileTempMap;

    public async Task ExtractAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, ConvertHelper.PdfToOfdOptions options, ILogger? logger, System.Threading.CancellationToken token)
    {
        if (!options.ExtractAndEmbedFonts)
        {
            logger?.LogDebug("[PDF2OFD][Font] ExtractAndEmbedFonts=false 跳过字体提取");
            return;
        }

        int totalPages = pdfDoc.GetNumberOfPages();
        logger?.LogInformation("[PDF2OFD][Font] PDF总页数: {Total}，开始提取字体...", totalPages);

        try
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i <= totalPages; i++)
            {
                token.ThrowIfCancellationRequested();
                if (options.PageFilter != null && !options.PageFilter(i)) continue;
                var page = pdfDoc.GetPage(i);
                var resources = page.GetResources();
                if (resources == null) continue;

                foreach (var fontName in resources.GetResourceNames(PdfName.Font) ?? Array.Empty<PdfName>())
                {
                    var fontObj = resources.GetResource(PdfName.Font).Get(fontName);
                    if (fontObj is not PdfDictionary fontDict) continue;
                    var baseNameRaw = fontDict.GetAsName(PdfName.BaseFont)?.GetValue() ?? fontName.GetValue();
                    var baseName = options.NormalizeSubsetFontName ? _subsetPrefixRegex.Replace(baseNameRaw, string.Empty) : baseNameRaw;
                    if (!visited.Add(baseName)) continue;

                    try
                    {
                        var descriptor = fontDict.GetAsDictionary(PdfName.FontDescriptor);
                        PdfStream? ff = descriptor?.GetAsStream(PdfName.FontFile3)
                                                ?? descriptor?.GetAsStream(PdfName.FontFile2)
                                                ?? descriptor?.GetAsStream(PdfName.FontFile);

                        // 逻辑字体名归一
                        var logicalName = ConvertHelper.NormalizeLogicalFontName(baseName);

                        if (ff != null)
                        {
                            var bytes = ff.GetBytes();
                            string ext = GuessFontExtension(fontDict);
                            string tmp = Path.Combine(Path.GetTempPath(), $"pdf_font_{Guid.NewGuid():N}{ext}");
                            await File.WriteAllBytesAsync(tmp, bytes, token);
                            _fontFileTempMap[logicalName] = tmp;
                            logger?.LogDebug("[PDF2OFD][Font] 提取并暂存字体 '{Font}' -> {Path}", logicalName, tmp);
                        }
                        else
                        {
                            // 系统回退
                            var sys = FindSystemFontPath(logicalName, logger);
                            if (sys != null)
                            {
                                string ext = Path.GetExtension(sys);
                                string tmp = Path.Combine(Path.GetTempPath(), $"pdf_sysfont_{Guid.NewGuid():N}{ext}");
                                try { File.Copy(sys, tmp, true); _fontFileTempMap[logicalName] = tmp; logger?.LogInformation("[PDF2OFD][Font] 使用系统字体回退 '{Font}' -> {Sys}", logicalName, sys); }
                                catch (Exception copyEx) { _fontFileTempMap[logicalName] = null; logger?.LogWarning(copyEx, "[PDF2OFD][Font] 系统字体复制失败 {Font}", logicalName); }
                            }
                            else
                            {
                                _fontFileTempMap[logicalName] = null;
                                logger?.LogDebug("[PDF2OFD][Font] 字体 '{Font}' 未嵌入且无系统回退", logicalName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "[PDF2OFD][Font] 提取字体 '{Font}' 失败", baseName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "[PDF2OFD][Font] 字体提取过程中发生严重异常");
            throw;
        }

        logger?.LogInformation("[PDF2OFD][Font] 字体提取完成，共处理 {Count} 种字体", _fontFileTempMap.Count);

        // 注册到 OFD
        foreach (var kv in _fontFileTempMap)
        {
            if (kv.Value == null) continue;
            try
            {
                (ofd as OfdWriter)?.AddExternalEmbeddedFont(kv.Key, kv.Value);
                logger?.LogDebug("[PDF2OFD][Font] 注册字体到OFD: {Font}", kv.Key);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "[PDF2OFD][Font] 注册字体 {Font} 失败", kv.Key);
            }
        }

        // 交由调用方整体生命周期结束后清理：此处不删除临时文件（保持原调用方 finally 中清理）
    }

    public void CleanupTempFiles(ILogger? logger)
    {
        foreach (var kv in _fontFileTempMap)
        {
            var p = kv.Value;
            if (p == null) continue;
            try { if (File.Exists(p)) File.Delete(p); } catch (Exception ex) { logger?.LogDebug(ex, "[PDF2OFD][Font] 清理临时字体失败 {Path}", p); }
        }
    }

    private static string GuessFontExtension(PdfDictionary fontDict)
    {
        var subType = fontDict.GetAsName(PdfName.Subtype)?.GetValue();
        if (subType == null) return ".font";
        if (subType.Contains("TrueType", StringComparison.OrdinalIgnoreCase)) return ".ttf";
        if (subType.Contains("Type0", StringComparison.OrdinalIgnoreCase)) return ".otf";
        if (subType.Contains("Type1", StringComparison.OrdinalIgnoreCase)) return ".pfb";
        if (subType.Contains("CIDFont", StringComparison.OrdinalIgnoreCase)) return ".otf";
        return ".font";
    }

    // 复用 ConvertHelper 中的系统字体候选逻辑（暂复制，后续可合并至 FontUtils）
    private static readonly Dictionary<string, string[]> SystemFontCandidates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SimSun"] = new[]{"simsun.ttc","SimSun.ttc"},
        ["Microsoft YaHei"] = new[]{"msyh.ttc","msyh.ttf"},
        ["SimHei"] = new[]{"simhei.ttf"},
        ["KaiTi"] = new[]{"simkai.ttf","kaiti.ttf"},
        ["FangSong"] = new[]{"simfang.ttf","FangSong.ttf"},
        ["仿宋"] = new[]{"simfang.ttf","FangSong.ttf"}
    };

    private static string? FindSystemFontPath(string logical, ILogger? logger)
    {
        if (!SystemFontCandidates.TryGetValue(logical, out var candidates)) return null;
        string fontDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
        foreach (var c in candidates)
        {
            var p = Path.Combine(fontDir, c);
            if (File.Exists(p)) return p;
        }
        logger?.LogTrace("[PDF2OFD][Font] 系统字体未找到 {Font}", logical);
        return null;
    }
}
