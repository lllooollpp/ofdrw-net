using System;
using Microsoft.Extensions.Logging;

namespace OfdrwNet.Converter.Options;

/// <summary>
/// PDF 导出选项配置类
/// </summary>
public class PdfExportOptions
{
    /// <summary>DPI（>=72）。默认 150。</summary>
    public float Dpi { get; set; } = 150f;

    /// <summary>起始页(1-based，可空)。</summary>
    public int? StartPage { get; set; }

    /// <summary>结束页(1-based，可空)。</summary>
    public int? EndPage { get; set; }

    /// <summary>进度回调（已转换页数, 总页数）。</summary>
    public IProgress<(int done, int total)>? Progress { get; set; }

    /// <summary>是否保留版式（绝对定位）。</summary>
    public bool PreserveLayout { get; set; }

    /// <summary>统计信息输出 JSON 文件路径（可空：不输出）。</summary>
    public string? StatsJsonPath { get; set; }

    /// <summary>字体名称映射回调：参数为 OFD 中字体名，返回 PDF 可用字体名（null 则使用默认）。</summary>
    public Func<string, string?>? FontMapper { get; set; }

    /// <summary>是否尝试嵌入映射字体（占位，当前未实现实际嵌入）。</summary>
    public bool EmbedFonts { get; set; }

    /// <summary>页面过滤器（1-based 页码）。返回 true 表示需要导出。</summary>
    public Func<int, bool>? PageFilter { get; set; }

    /// <summary>是否提取真实图片</summary>
    public bool RealImageEmbedding { get; set; } = true;

    /// <summary>日志记录器</summary>
    public ILogger? Logger { get; set; }
}
