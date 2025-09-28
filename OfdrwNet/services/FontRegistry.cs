using Microsoft.Extensions.Logging;
using OfdrwNet.Models;

namespace OfdrwNet.Services;

/// <summary>
/// 构建并缓存字体映射。
/// </summary>
internal sealed class FontRegistry
{
    private readonly ILogger? _logger;
    public FontRegistry(ILogger? logger){ _logger = logger; }

    public Dictionary<string, OfdFont> Build(IDictionary<string,string> externalFontFiles, IEnumerable<object> rawItems)
    {
        var map = new Dictionary<string, OfdFont>(StringComparer.OrdinalIgnoreCase);
        int id = 1;
        var fontNames = rawItems
            .OfType<RawGlyphRun>()
            .Select(r => r.FontName)
            .Concat(externalFontFiles.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach(var name in fontNames){
            if(!map.ContainsKey(name)) map[name] = new OfdFont(id++, name, name);
        }
        if(map.Count == 0){ map["SimSun"] = new OfdFont(1, "SimSun", "宋体"); }
        _logger?.LogDebug("[FontRegistry] Fonts={Count}", map.Count);
        return map;
    }
}
