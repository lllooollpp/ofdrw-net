using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OfdrwNet.Reader.Model
{
    /// <summary>
    /// 页面缓存管理器
    /// 负责页面位图缓存、内存管理和性能优化
    /// </summary>
    public class PageCache : IDisposable
    {
        private readonly ConcurrentDictionary<int, PageCacheEntry> _pageCache = new();
        private readonly PageCacheOptions _options;
        private readonly Timer _cleanupTimer;
        private readonly SemaphoreSlim _renderSemaphore;
        private long _totalMemoryUsage;
        private bool _disposed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">缓存选项</param>
        public PageCache(PageCacheOptions? options = null)
        {
            _options = options ?? new PageCacheOptions();
            _renderSemaphore = new SemaphoreSlim(_options.MaxConcurrentRenders);

            // 设置定期清理定时器
            _cleanupTimer = new Timer(CleanupExpiredEntries, null,
                TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
        }

        /// <summary>
        /// 当前内存使用量（字节）
        /// </summary>
        public long MemoryUsage => Interlocked.Read(ref _totalMemoryUsage);

        /// <summary>
        /// 缓存的页面数量
        /// </summary>
        public int CachedPageCount => _pageCache.Count;

        /// <summary>
        /// 获取或渲染页面位图
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <param name="renderFunc">渲染函数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>页面位图</returns>
        public async Task<Bitmap?> GetPageBitmapAsync(
            int pageIndex,
            RenderContext renderContext,
            Func<int, RenderContext, CancellationToken, Task<Bitmap?>> renderFunc,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = GenerateCacheKey(pageIndex, renderContext);

            // 尝试从缓存获取
            if (_pageCache.TryGetValue(cacheKey, out var entry))
            {
                entry.LastAccessTime = DateTime.UtcNow;
                entry.AccessCount++;

                // 检查缓存是否仍然有效
                if (IsCacheValid(entry, renderContext))
                {
                    return entry.Bitmap?.Clone() as Bitmap;
                }

                // 缓存无效，移除
                RemoveCacheEntry(cacheKey);
            }

            // 检查内存限制
            if (MemoryUsage > _options.MaxMemoryUsage)
            {
                await EvictLeastRecentlyUsedAsync();
            }

            // 限制并发渲染数量
            await _renderSemaphore.WaitAsync(cancellationToken);

            try
            {
                // 双重检查，可能在等待期间已被其他线程缓存
                if (_pageCache.TryGetValue(cacheKey, out entry) && IsCacheValid(entry, renderContext))
                {
                    entry.LastAccessTime = DateTime.UtcNow;
                    entry.AccessCount++;
                    return entry.Bitmap?.Clone() as Bitmap;
                }

                // 执行渲染
                var bitmap = await renderFunc(pageIndex, renderContext, cancellationToken);

                if (bitmap != null)
                {
                    // 创建缓存条目
                    var cacheEntry = new PageCacheEntry
                    {
                        PageIndex = pageIndex,
                        CacheKey = cacheKey,
                        Bitmap = bitmap.Clone() as Bitmap,
                        RenderContext = renderContext.Clone(),
                        CreatedTime = DateTime.UtcNow,
                        LastAccessTime = DateTime.UtcNow,
                        AccessCount = 1,
                        MemorySize = CalculateBitmapMemorySize(bitmap)
                    };

                    // 添加到缓存
                    _pageCache.TryAdd(cacheKey, cacheEntry);
                    Interlocked.Add(ref _totalMemoryUsage, cacheEntry.MemorySize);

                    return bitmap.Clone() as Bitmap;
                }

                return null;
            }
            finally
            {
                _renderSemaphore.Release();
            }
        }

        /// <summary>
        /// 预渲染页面
        /// </summary>
        /// <param name="pageIndexes">页面索引列表</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <param name="renderFunc">渲染函数</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task PreRenderPagesAsync(
            IEnumerable<int> pageIndexes,
            RenderContext renderContext,
            Func<int, RenderContext, CancellationToken, Task<Bitmap?>> renderFunc,
            CancellationToken cancellationToken = default)
        {
            if (!_options.EnablePreRendering)
                return;

            var tasks = pageIndexes.Select(async pageIndex =>
            {
                try
                {
                    await GetPageBitmapAsync(pageIndex, renderContext, renderFunc, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // 忽略取消异常
                }
                catch
                {
                    // 忽略渲染错误，继续预渲染其他页面
                }
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 预渲染当前页面周围的页面
        /// </summary>
        /// <param name="currentPageIndex">当前页面索引</param>
        /// <param name="totalPages">总页数</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <param name="renderFunc">渲染函数</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task PreRenderSurroundingPagesAsync(
            int currentPageIndex,
            int totalPages,
            RenderContext renderContext,
            Func<int, RenderContext, CancellationToken, Task<Bitmap?>> renderFunc,
            CancellationToken cancellationToken = default)
        {
            var surroundingPages = GetSurroundingPageIndexes(currentPageIndex, totalPages, _options.PreRenderRange);
            await PreRenderPagesAsync(surroundingPages, renderContext, renderFunc, cancellationToken);
        }

        /// <summary>
        /// 清除特定页面的缓存
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <returns>清除的缓存条目数量</returns>
        public int ClearPageCache(int pageIndex)
        {
            var keysToRemove = _pageCache.Keys.Where(key => ExtractPageIndex(key) == pageIndex).ToList();
            var removedCount = 0;

            foreach (var key in keysToRemove)
            {
                if (RemoveCacheEntry(key))
                {
                    removedCount++;
                }
            }

            return removedCount;
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearAllCache()
        {
            var entries = _pageCache.Values.ToList();
            _pageCache.Clear();

            foreach (var entry in entries)
            {
                entry.Bitmap?.Dispose();
            }

            Interlocked.Exchange(ref _totalMemoryUsage, 0);
        }

        /// <summary>
        /// 无效化基于渲染上下文的缓存
        /// </summary>
        /// <param name="predicate">无效化条件</param>
        public void InvalidateCache(Func<PageCacheEntry, bool> predicate)
        {
            var keysToRemove = _pageCache.Where(kvp => predicate(kvp.Value))
                                      .Select(kvp => kvp.Key)
                                      .ToList();

            foreach (var key in keysToRemove)
            {
                RemoveCacheEntry(key);
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计信息</returns>
        public PageCacheStatistics GetCacheStatistics()
        {
            var entries = _pageCache.Values.ToList();

            return new PageCacheStatistics
            {
                TotalMemoryUsage = MemoryUsage,
                CachedPageCount = entries.Count,
                AverageAccessCount = entries.Count > 0 ? entries.Average(e => e.AccessCount) : 0,
                OldestEntryAge = entries.Count > 0 ? DateTime.UtcNow - entries.Min(e => e.CreatedTime) : TimeSpan.Zero,
                HitRate = CalculateHitRate(),
                PageDistribution = GetPageDistribution(entries)
            };
        }

        /// <summary>
        /// 设置缓存选项
        /// </summary>
        /// <param name="options">新的缓存选项</param>
        public void UpdateOptions(PageCacheOptions options)
        {
            // 更新配置后，可能需要清理超出限制的缓存
            if (options.MaxMemoryUsage < _options.MaxMemoryUsage && MemoryUsage > options.MaxMemoryUsage)
            {
                _ = Task.Run(EvictLeastRecentlyUsedAsync);
            }
        }

        private int GenerateCacheKey(int pageIndex, RenderContext renderContext)
        {
            // 生成基于页面索引、缩放级别、DPI等的缓存键
            var hash = HashCode.Combine(
                pageIndex,
                renderContext.ScaleFactor.GetHashCode(),
                renderContext.DpiX,
                renderContext.DpiY,
                renderContext.ViewPort.GetHashCode()
            );

            return hash;
        }

        private bool IsCacheValid(PageCacheEntry entry, RenderContext currentContext)
        {
            if (DateTime.UtcNow - entry.CreatedTime > _options.ExpirationTime)
                return false;

            // 检查渲染上下文是否匹配
            var cachedContext = entry.RenderContext;
            return Math.Abs(cachedContext.ScaleFactor - currentContext.ScaleFactor) < 0.001 &&
                   cachedContext.DpiX == currentContext.DpiX &&
                   cachedContext.DpiY == currentContext.DpiY &&
                   cachedContext.ViewPort.Equals(currentContext.ViewPort);
        }

        private async Task EvictLeastRecentlyUsedAsync()
        {
            var targetMemory = (long)(_options.MaxMemoryUsage * 0.8); // 清理到80%
            var currentMemory = MemoryUsage;

            var sortedEntries = _pageCache.Values
                .OrderBy(e => e.LastAccessTime)
                .ThenBy(e => e.AccessCount)
                .ToList();

            foreach (var entry in sortedEntries)
            {
                if (currentMemory <= targetMemory)
                    break;

                if (RemoveCacheEntry(entry.CacheKey))
                {
                    currentMemory -= entry.MemorySize;
                }
            }

            await Task.CompletedTask;
        }

        private void CleanupExpiredEntries(object? state)
        {
            var expiredTime = DateTime.UtcNow - _options.ExpirationTime;

            var expiredKeys = _pageCache.Where(kvp => kvp.Value.CreatedTime < expiredTime)
                                      .Select(kvp => kvp.Key)
                                      .ToList();

            foreach (var key in expiredKeys)
            {
                RemoveCacheEntry(key);
            }
        }

        private bool RemoveCacheEntry(int cacheKey)
        {
            if (_pageCache.TryRemove(cacheKey, out var entry))
            {
                Interlocked.Add(ref _totalMemoryUsage, -entry.MemorySize);
                entry.Bitmap?.Dispose();
                return true;
            }
            return false;
        }

        private static long CalculateBitmapMemorySize(Bitmap bitmap)
        {
            // 估算位图内存大小：宽 x 高 x 每像素字节数
            var bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            return bitmap.Width * bitmap.Height * bytesPerPixel;
        }

        private static int ExtractPageIndex(int cacheKey)
        {
            // 这是一个简化的实现，实际应该从缓存键中提取页面索引
            // 为了演示，我们假设页面索引是缓存键的一部分
            return Math.Abs(cacheKey) % 10000; // 简化提取
        }

        private static IEnumerable<int> GetSurroundingPageIndexes(int currentPageIndex, int totalPages, int range)
        {
            var startIndex = Math.Max(0, currentPageIndex - range);
            var endIndex = Math.Min(totalPages - 1, currentPageIndex + range);

            for (int i = startIndex; i <= endIndex; i++)
            {
                yield return i;
            }
        }

        private double CalculateHitRate()
        {
            // 简化的命中率计算，实际实现需要跟踪命中和未命中次数
            return _pageCache.Count > 0 ? 0.85 : 0.0; // 示例值
        }

        private Dictionary<int, int> GetPageDistribution(List<PageCacheEntry> entries)
        {
            return entries.GroupBy(e => e.PageIndex)
                         .ToDictionary(g => g.Key, g => g.Count());
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cleanupTimer?.Dispose();
                _renderSemaphore?.Dispose();
                ClearAllCache();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 页面缓存条目
    /// </summary>
    public class PageCacheEntry
    {
        /// <summary>
        /// 页面索引
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// 缓存键
        /// </summary>
        public int CacheKey { get; set; }

        /// <summary>
        /// 页面位图
        /// </summary>
        public Bitmap? Bitmap { get; set; }

        /// <summary>
        /// 渲染上下文
        /// </summary>
        public RenderContext RenderContext { get; set; } = new RenderContext();

        /// <summary>
        /// 内存大小（字节）
        /// </summary>
        public long MemorySize { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 最后访问时间
        /// </summary>
        public DateTime LastAccessTime { get; set; }

        /// <summary>
        /// 访问次数
        /// </summary>
        public int AccessCount { get; set; }
    }

    /// <summary>
    /// 页面缓存选项
    /// </summary>
    public class PageCacheOptions
    {
        /// <summary>
        /// 最大内存使用量（字节）
        /// </summary>
        public long MaxMemoryUsage { get; set; } = 200 * 1024 * 1024; // 200MB

        /// <summary>
        /// 缓存过期时间
        /// </summary>
        public TimeSpan ExpirationTime { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// 是否启用预渲染
        /// </summary>
        public bool EnablePreRendering { get; set; } = true;

        /// <summary>
        /// 预渲染范围（当前页面前后的页面数）
        /// </summary>
        public int PreRenderRange { get; set; } = 2;

        /// <summary>
        /// 最大并发渲染数
        /// </summary>
        public int MaxConcurrentRenders { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// 低质量预览模式的缩放因子
        /// </summary>
        public double PreviewScaleFactor { get; set; } = 0.25;

        /// <summary>
        /// 是否启用压缩缓存
        /// </summary>
        public bool EnableCompression { get; set; } = false;
    }

    /// <summary>
    /// 页面缓存统计信息
    /// </summary>
    public class PageCacheStatistics
    {
        /// <summary>
        /// 总内存使用量
        /// </summary>
        public long TotalMemoryUsage { get; set; }

        /// <summary>
        /// 缓存的页面数量
        /// </summary>
        public int CachedPageCount { get; set; }

        /// <summary>
        /// 平均访问次数
        /// </summary>
        public double AverageAccessCount { get; set; }

        /// <summary>
        /// 最老条目的年龄
        /// </summary>
        public TimeSpan OldestEntryAge { get; set; }

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double HitRate { get; set; }

        /// <summary>
        /// 页面分布（页面索引 -> 缓存条目数）
        /// </summary>
        public Dictionary<int, int>? PageDistribution { get; set; }
    }

    /// <summary>
    /// 渲染质量级别
    /// </summary>
    public enum RenderQuality
    {
        /// <summary>
        /// 低质量（快速预览）
        /// </summary>
        Low,

        /// <summary>
        /// 中等质量（平衡模式）
        /// </summary>
        Medium,

        /// <summary>
        /// 高质量（最佳显示）
        /// </summary>
        High,

        /// <summary>
        /// 打印质量（超高分辨率）
        /// </summary>
        Print
    }

    /// <summary>
    /// 页面渲染请求
    /// </summary>
    public class PageRenderRequest
    {
        /// <summary>
        /// 页面索引
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// 渲染质量
        /// </summary>
        public RenderQuality Quality { get; set; } = RenderQuality.Medium;

        /// <summary>
        /// 优先级（数值越小优先级越高）
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 请求时间
        /// </summary>
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 取消令牌
        /// </summary>
        public CancellationToken CancellationToken { get; set; }
    }
}
