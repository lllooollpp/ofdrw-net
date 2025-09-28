using Microsoft.Extensions.Logging;
using OfdrwNet.Models;

namespace OfdrwNet.Services;

/// <summary>
/// 根据 Raw 数据与虚拟页构建分页分组。
/// </summary>
internal sealed class PageGroupBuilder
{
    private readonly ILogger? _logger;
    public PageGroupBuilder(ILogger? logger){ _logger = logger; }

    public List<(int PageNumber, List<object> Items)> Build(
        IList<object> streamQueue,
        IList<VirtualPage> virtualPages,
        IList<RawImage> images,
        IList<RawPath> paths)
    {
        var runs = streamQueue.OfType<RawGlyphRun>().ToList();
        int maxPage = 0;
        if(virtualPages?.Count > 0) maxPage = Math.Max(maxPage, virtualPages.Count);
        if(runs.Any()) maxPage = Math.Max(maxPage, runs.Max(r => r.Page));
        if(images.Any()) maxPage = Math.Max(maxPage, images.Max(i => i.Page));
        if(paths.Any()) maxPage = Math.Max(maxPage, paths.Max(p => p.Page));
        if(maxPage == 0) maxPage = 1;
        var pages = new List<(int PageNumber, List<object> Items)>();
        for(int p=1;p<=maxPage;p++){
            var items = new List<object>();
            items.AddRange(runs.Where(r => r.Page == p));
            items.AddRange(paths.Where(pt => pt.Page == p));
            if(p==1){ items.AddRange(streamQueue.Where(s => s is not RawGlyphRun)); }
            pages.Add((p, items));
        }
        _logger?.LogDebug("[PageGroupBuilder] Pages={Count} Runs={Runs} Images={Images} Paths={Paths} VirtualPages={VP}",
            pages.Count, runs.Count, images.Count, paths.Count, virtualPages?.Count ?? 0);
        return pages;
    }
}
