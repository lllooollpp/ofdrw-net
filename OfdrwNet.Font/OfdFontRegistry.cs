using Microsoft.Extensions.Logging;

namespace OfdrwNet.Font;

/// <summary>
/// OFD 字体注册器，负责构建并缓存字体映射
/// </summary>
public sealed class OfdFontRegistry
{
    private readonly ILogger? _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public OfdFontRegistry(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 构建字体映射表
    /// </summary>
    /// <param name="externalFontFiles">外部字体文件映射</param>
    /// <param name="rawItems">原始项目集合（包含字形运行等）</param>
    /// <returns>字体映射表</returns>
    public Dictionary<string, OfdFont> Build(
        IDictionary<string, string> externalFontFiles,
        IEnumerable<object> rawItems)
    {
        var map = new Dictionary<string, OfdFont>(StringComparer.OrdinalIgnoreCase);
        int id = 1;

        // 从原始项目中提取字体名称
        var fontNamesFromRawItems = ExtractFontNamesFromRawItems(rawItems);

        // 合并所有字体名称
        var fontNames = fontNamesFromRawItems
            .Concat(externalFontFiles.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        // 为每个字体名称创建字体对象
        foreach (var name in fontNames)
        {
            if (!map.ContainsKey(name))
            {
                map[name] = new OfdFont(id++, name, name);
            }
        }

        // 确保至少有一个默认字体
        if (map.Count == 0)
        {
            map["SimSun"] = new OfdFont(1, "SimSun", "宋体");
        }

        _logger?.LogDebug("[OfdFontRegistry] 构建字体映射完成，字体数量: {Count}", map.Count);
        return map;
    }

    /// <summary>
    /// 从原始项目中提取字体名称（支持不同类型的字形运行对象）
    /// </summary>
    /// <param name="rawItems">原始项目集合</param>
    /// <returns>字体名称集合</returns>
    private IEnumerable<string> ExtractFontNamesFromRawItems(IEnumerable<object> rawItems)
    {
        var fontNames = new List<string>();

        foreach (var item in rawItems)
        {
            var fontName = ExtractFontNameFromItem(item);
            if (!string.IsNullOrEmpty(fontName))
            {
                fontNames.Add(fontName);
            }
        }

        return fontNames;
    }

    /// <summary>
    /// 从单个项目中提取字体名称
    /// </summary>
    /// <param name="item">项目对象</param>
    /// <returns>字体名称</returns>
    private string? ExtractFontNameFromItem(object item)
    {
        // 使用反射获取 FontName 属性（支持不同类型的字形运行对象）
        var itemType = item.GetType();
        var fontNameProperty = itemType.GetProperty("FontName");

        if (fontNameProperty != null && fontNameProperty.CanRead)
        {
            var fontName = fontNameProperty.GetValue(item) as string;
            return fontName;
        }

        return null;
    }

    /// <summary>
    /// 字体映射配置
    /// </summary>
    public static class FontMappings
    {
        private static readonly Dictionary<string, string> _mappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Arial"] = "Arial",
            ["Times New Roman"] = "Times New Roman",
            ["Courier New"] = "Courier New",
            ["Helvetica"] = "Arial",
            ["Times"] = "Times New Roman",
            ["Courier"] = "Courier New",
            ["SimSun"] = "SimSun",
            ["宋体"] = "SimSun",
            ["KaiTi"] = "KaiTi",
            ["楷体"] = "KaiTi",
            ["Microsoft YaHei"] = "Microsoft YaHei",
            ["微软雅黑"] = "Microsoft YaHei",
            ["黑体"] = "SimHei",
            ["SimHei"] = "SimHei"
        };

        /// <summary>
        /// 获取所有字体映射
        /// </summary>
        public static IReadOnlyDictionary<string, string> All => _mappings;

        /// <summary>
        /// 添加字体映射
        /// </summary>
        /// <param name="fontName">字体名称</param>
        /// <param name="systemFontName">系统字体名称</param>
        public static void AddMapping(string fontName, string systemFontName)
        {
            _mappings[fontName] = systemFontName;
        }

        /// <summary>
        /// 获取映射的字体名称
        /// </summary>
        /// <param name="fontName">原始字体名称</param>
        /// <returns>映射后的字体名称</returns>
        public static string GetMappedFontName(string fontName)
        {
            return _mappings.TryGetValue(fontName, out var mappedFont) ? mappedFont : fontName;
        }

        /// <summary>
        /// 清除所有映射
        /// </summary>
        public static void Clear()
        {
            _mappings.Clear();
        }

        /// <summary>
        /// 移除指定映射
        /// </summary>
        /// <param name="fontName">字体名称</param>
        /// <returns>是否成功移除</returns>
        public static bool RemoveMapping(string fontName)
        {
            return _mappings.Remove(fontName);
        }
    }
}
