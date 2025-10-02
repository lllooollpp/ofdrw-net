using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Extensions.Logging;

namespace OfdrwNet.Converter.Refactor.Utils;

/// <summary>
/// 字体名称归一与系统字体查找工具。
/// </summary>
internal static class FontUtils
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

    private static readonly System.Text.RegularExpressions.Regex _subsetPrefixRegex = new(@"^[A-Z]{6}\+");

    /// <summary>
    /// 规范化 PDF 中可能带子集前缀/乱码的逻辑字体名。
    /// </summary>
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
    /// 查找系统字体文件路径。
    /// </summary>
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
