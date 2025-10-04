using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions;

namespace OfdrwNet.Font;

/// <summary>
/// PDF 字体提取选项配置
/// </summary>
public sealed class FontExtractionOptions
{
    /// <summary>
    /// 是否提取并嵌入字体
    /// </summary>
    public bool ExtractAndEmbedFonts { get; set; } = true;

    /// <summary>
    /// 是否规范化子集字体名称（移除前缀如 ABCDEF+）
    /// </summary>
    public bool NormalizeSubsetFontName { get; set; } = true;

    /// <summary>
    /// 页面过滤器，如果返回 false 则跳过该页面
    /// </summary>
    public Func<int, bool>? PageFilter { get; set; }
}

/// <summary>
/// OFD 字体数据传输对象
/// </summary>
public sealed class OfdFontData
{
    /// <summary>
    /// 逻辑字体名称
    /// </summary>
    public required string LogicalName { get; init; }

    /// <summary>
    /// 临时字体文件路径，可能为 null（表示未找到字体文件）
    /// </summary>
    public string? TempFilePath { get; init; }

    /// <summary>
    /// 是否为系统字体
    /// </summary>
    public bool IsSystemFont { get; init; }

    /// <summary>
    /// 原始字体名称
    /// </summary>
    public required string OriginalName { get; init; }
}

/// <summary>
/// PDF 字体渲染事件监听器，用于从 PDF 中提取字体
/// </summary>
public sealed class PdfFontExtractor
{
    private readonly Regex _subsetPrefixRegex = new(@"^[A-Z]{6}\+");
    private readonly Dictionary<string, OfdFontData> _extractedFonts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 获取已提取的字体集合
    /// </summary>
    public IReadOnlyDictionary<string, OfdFontData> ExtractedFonts => _extractedFonts;

    /// <summary>
    /// 从 PDF 文档中提取字体
    /// </summary>
    /// <param name="pdfDoc">PDF 文档</param>
    /// <param name="options">提取选项</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="token">取消令牌</param>
    /// <returns>提取的字体数据</returns>
    public async Task<IReadOnlyDictionary<string, OfdFontData>> ExtractFontsAsync(
        PdfDocument pdfDoc,
        FontExtractionOptions options,
        ILogger? logger = null,
        CancellationToken token = default)
    {
        if (pdfDoc == null) throw new ArgumentNullException(nameof(pdfDoc));
        if (options == null) throw new ArgumentNullException(nameof(options));

        if (!options.ExtractAndEmbedFonts)
        {
            logger?.LogDebug("[PDF2OFD][Font] ExtractAndEmbedFonts=false 跳过字体提取");
            return _extractedFonts;
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
                        var logicalName = FontUtils.NormalizeLogicalFontName(baseName);

                        if (ff != null)
                        {
                            // 嵌入字体提取
                            var bytes = ff.GetBytes();
                            string ext = GuessFontExtension(fontDict);
                            string tmp = Path.Combine(Path.GetTempPath(), $"pdf_font_{Guid.NewGuid():N}{ext}");
                            await File.WriteAllBytesAsync(tmp, bytes, token);

                            _extractedFonts[logicalName] = new OfdFontData
                            {
                                LogicalName = logicalName,
                                TempFilePath = tmp,
                                IsSystemFont = false,
                                OriginalName = baseNameRaw
                            };

                            logger?.LogDebug("[PDF2OFD][Font] 提取并暂存字体 '{Font}' -> {Path}", logicalName, tmp);
                        }
                        else
                        {
                            // 系统字体回退
                            var sysPath = FontUtils.FindSystemFontPath(logicalName, logger);
                            if (sysPath != null)
                            {
                                string ext = Path.GetExtension(sysPath);
                                string tmp = Path.Combine(Path.GetTempPath(), $"pdf_sysfont_{Guid.NewGuid():N}{ext}");
                                try
                                {
                                    File.Copy(sysPath, tmp, true);
                                    _extractedFonts[logicalName] = new OfdFontData
                                    {
                                        LogicalName = logicalName,
                                        TempFilePath = tmp,
                                        IsSystemFont = true,
                                        OriginalName = baseNameRaw
                                    };
                                    logger?.LogInformation("[PDF2OFD][Font] 使用系统字体回退 '{Font}' -> {Sys}", logicalName, sysPath);
                                }
                                catch (Exception copyEx)
                                {
                                    _extractedFonts[logicalName] = new OfdFontData
                                    {
                                        LogicalName = logicalName,
                                        TempFilePath = null,
                                        IsSystemFont = true,
                                        OriginalName = baseNameRaw
                                    };
                                    logger?.LogWarning(copyEx, "[PDF2OFD][Font] 系统字体复制失败 {Font}", logicalName);
                                }
                            }
                            else
                            {
                                _extractedFonts[logicalName] = new OfdFontData
                                {
                                    LogicalName = logicalName,
                                    TempFilePath = null,
                                    IsSystemFont = false,
                                    OriginalName = baseNameRaw
                                };
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

        logger?.LogInformation("[PDF2OFD][Font] 字体提取完成，共处理 {Count} 种字体", _extractedFonts.Count);
        return _extractedFonts;
    }

    /// <summary>
    /// 将提取的字体注册到 OFD Writer
    /// </summary>
    /// <param name="ofdWriter">OFD Writer 实例</param>
    /// <param name="logger">日志记录器</param>
    public void RegisterFontsToOfd(IOfdDocWriter ofdWriter, ILogger? logger = null)
    {
        foreach (var fontData in _extractedFonts.Values)
        {
            if (fontData.TempFilePath == null) continue;

            try
            {
                ofdWriter.AddExternalEmbeddedFont(fontData.LogicalName, fontData.TempFilePath);
                logger?.LogDebug("[PDF2OFD][Font] 注册字体到OFD: {Font}", fontData.LogicalName);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "[PDF2OFD][Font] 注册字体 {Font} 失败", fontData.LogicalName);
            }
        }
    }

    /// <summary>
    /// 清理临时字体文件
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public void CleanupTempFiles(ILogger? logger = null)
    {
        foreach (var fontData in _extractedFonts.Values)
        {
            var path = fontData.TempFilePath;
            if (path == null) continue;

            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "[PDF2OFD][Font] 清理临时字体失败 {Path}", path);
            }
        }
        _extractedFonts.Clear();
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
}

/// <summary>
/// 字体名称归一与系统字体查找工具
/// </summary>
public static class FontUtils
{
    private static readonly Dictionary<string, string> _fontNameFallbackMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // 常见的乱码/变体映射到系统字体
        ["ËÎÌå"] = "SimSun",          // 宋体 (乱码形式)
        ["Î¢ÈíÑÅºÚ"] = "Microsoft YaHei", // 微软雅黑 (乱码形式)
        ["ºÚÌå"] = "SimHei",          // 黑体 (乱码形式)
        ["¿¬Ìå"] = "KaiTi",           // 楷体 (乱码形式)
        ["KaiTi_GB2312"] = "KaiTi",   // 另一种写法

        // 中文原始名直接映射
        ["宋体"] = "SimSun",
        ["新宋体"] = "NSimSun",
        ["微软雅黑"] = "Microsoft YaHei",
        ["微软雅黑 UI"] = "Microsoft YaHei",
        ["黑体"] = "SimHei",
        ["楷体"] = "KaiTi",
        ["仿宋"] = "FangSong",

        // 西文字体常见 PDF 名称映射
        ["TimesNewRomanPSMT"] = "Times New Roman",
        ["TimesNewRoman"] = "Times New Roman",
        ["Times-Roman"] = "Times New Roman",
        ["Times"] = "Times New Roman",
        ["TimesNewRomanPS-BoldMT"] = "Times New Roman",
        ["ArialMT"] = "Arial",
        ["Helvetica"] = "Arial",
        ["Calibri"] = "Calibri",
        ["Tahoma"] = "Tahoma",
        ["Arial Unicode MS"] = "Arial Unicode MS",
    };

    private static readonly Dictionary<string, string[]> _systemFontCandidates = new(StringComparer.OrdinalIgnoreCase)
    {
        // 中文字体
        ["SimSun"] = new[] { "simsun.ttc", "SimSun.ttc", "simsun.ttf" },
        ["NSimSun"] = new[] { "nsimsun.ttc", "NSimSun.ttc" },
        ["Microsoft YaHei"] = new[] { "msyh.ttc", "msyh.ttf", "msyhui.ttc" },
        ["Microsoft YaHei UI"] = new[] { "msyhui.ttc", "msyh.ttc" },
        ["SimHei"] = new[] { "simhei.ttf", "simhei.ttc" },
        ["KaiTi"] = new[] { "simkai.ttf", "kaiti.ttf", "kaiu.ttf" },
        ["FangSong"] = new[] { "simfang.ttf", "FangSong.ttf" },
        ["仿宋"] = new[] { "simfang.ttf", "FangSong.ttf" },

        // 西文/通用字体
        ["Times New Roman"] = new[] { "times.ttf", "times new roman.ttf", "times.ttf", "timesbd.ttf" },
        ["Arial"] = new[] { "arial.ttf", "Arial.ttf", "arialbd.ttf" },
        ["Calibri"] = new[] { "calibri.ttf", "calibri.ttc" },
        ["Tahoma"] = new[] { "tahoma.ttf" },
        ["Arial Unicode MS"] = new[] { "arialuni.ttf", "ARIALUNI.TTF" },

        // 保留旧有条目以兼容
        ["SimSun-ExtB"] = new[] { "simsun-extb.ttf" }
    };

    private static readonly Regex _subsetPrefixRegex = new(@"^[A-Z]{6}\+");

    /// <summary>
    /// 规范化 PDF 中可能带子集前缀/乱码的逻辑字体名
    /// </summary>
    /// <param name="baseName">原始字体名称</param>
    /// <param name="stripSubsetPrefix">是否移除子集前缀</param>
    /// <returns>规范化后的字体名称</returns>
    public static string NormalizeLogicalFontName(string baseName, bool stripSubsetPrefix = true)
    {
        if (string.IsNullOrWhiteSpace(baseName))
            return baseName ?? string.Empty;

        var cleaned = baseName.Trim();

        if (stripSubsetPrefix && _subsetPrefixRegex.IsMatch(cleaned))
        {
            cleaned = cleaned[(cleaned.IndexOf('+') + 1)..];
        }

        int idx = cleaned.IndexOf("-WinCharSet", StringComparison.OrdinalIgnoreCase);
        if (idx > 0)
            cleaned = cleaned[..idx];

        idx = cleaned.IndexOf("_GB2312", StringComparison.OrdinalIgnoreCase);
        if (idx > 0)
            cleaned = cleaned[..idx];

        idx = cleaned.IndexOf("-GB2312", StringComparison.OrdinalIgnoreCase);
        if (idx > 0)
            cleaned = cleaned[..idx];

        if (cleaned.EndsWith("GB2312", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[..^"GB2312".Length];
        }

        foreach (var kv in _fontNameFallbackMap)
        {
            if (cleaned.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }

        return cleaned;
    }

    /// <summary>
    /// 查找系统字体文件路径
    /// </summary>
    /// <param name="logicalName">逻辑字体名称</param>
    /// <param name="logger">日志记录器</param>
    /// <returns>字体文件路径，如果未找到则返回 null</returns>
    public static string? FindSystemFontPath(string logicalName, ILogger? logger = null)
    {
        if (!_systemFontCandidates.TryGetValue(logicalName, out var candidates))
            return null;

        string fontDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
        foreach (var c in candidates)
        {
            var p = Path.Combine(fontDir, c);
            if (File.Exists(p))
                return p;
        }

        logger?.LogDebug("[FontUtils] 未找到系统字体 {Font}", logicalName);
        return null;
    }
}
