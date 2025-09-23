using System;
using System.Collections.Generic;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// 资源缓存条目
    /// </summary>
    public class ResourceCacheEntry
    {
        /// <summary>
        /// 缓存的资源对象
        /// </summary>
        public object? Resource { get; set; }

        /// <summary>
        /// 缓存创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后访问时间
        /// </summary>
        public DateTime LastAccessTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 访问次数
        /// </summary>
        public int AccessCount { get; set; } = 0;

        /// <summary>
        /// 资源大小(字节)
        /// </summary>
        public long Size { get; set; } = 0;

        /// <summary>
        /// 资源类型
        /// </summary>
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>
        /// 是否为预加载资源
        /// </summary>
        public bool IsPreloaded { get; set; } = false;

        /// <summary>
        /// 更新访问信息
        /// </summary>
        public void UpdateAccess()
        {
            LastAccessTime = DateTime.UtcNow;
            AccessCount++;
        }

        /// <summary>
        /// 检查缓存是否过期
        /// </summary>
        /// <param name="maxAge">最大缓存时间</param>
        /// <returns>是否过期</returns>
        public bool IsExpired(TimeSpan maxAge)
        {
            return DateTime.UtcNow - CreatedTime > maxAge;
        }
    }

    /// <summary>
    /// 资源缓存配置
    /// </summary>
    public class ResourceCacheConfig
    {
        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool EnableCache { get; set; } = true;

        /// <summary>
        /// 最大缓存条目数
        /// </summary>
        public int MaxCacheEntries { get; set; } = 1000;

        /// <summary>
        /// 最大内存使用量(字节)
        /// </summary>
        public long MaxMemoryUsage { get; set; } = 100 * 1024 * 1024; // 100MB

        /// <summary>
        /// 缓存过期时间
        /// </summary>
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// 是否启用预加载
        /// </summary>
        public bool EnablePreloading { get; set; } = true;

        /// <summary>
        /// 预加载并发数
        /// </summary>
        public int PreloadConcurrency { get; set; } = 4;

        /// <summary>
        /// 缓存清理间隔
        /// </summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// LRU清理阈值(百分比)
        /// </summary>
        public double LruThreshold { get; set; } = 0.8;
    }

    /// <summary>
    /// 资源缓存统计信息
    /// </summary>
    public class ResourceCacheStatistics
    {
        private int _hitCount = 0;
        private int _missCount = 0;
        private int _evictionCount = 0;
        private int _preloadCount = 0;
        private long _totalLoadTime = 0;
        private readonly DateTime _startTime = DateTime.UtcNow;

        /// <summary>
        /// 缓存命中次数
        /// </summary>
        public int HitCount => _hitCount;

        /// <summary>
        /// 缓存未命中次数
        /// </summary>
        public int MissCount => _missCount;

        /// <summary>
        /// 缓存清理次数
        /// </summary>
        public int EvictionCount => _evictionCount;

        /// <summary>
        /// 预加载资源数量
        /// </summary>
        public int PreloadCount => _preloadCount;

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double HitRatio
        {
            get
            {
                var total = _hitCount + _missCount;
                return total > 0 ? (double)_hitCount / total : 0.0;
            }
        }

        /// <summary>
        /// 平均加载时间(毫秒)
        /// </summary>
        public double AverageLoadTime
        {
            get
            {
                var totalRequests = _hitCount + _missCount;
                return totalRequests > 0 ? (double)_totalLoadTime / totalRequests : 0.0;
            }
        }

        /// <summary>
        /// 运行时间
        /// </summary>
        public TimeSpan Uptime => DateTime.UtcNow - _startTime;

        /// <summary>
        /// 记录缓存命中
        /// </summary>
        public void RecordHit()
        {
            Interlocked.Increment(ref _hitCount);
        }

        /// <summary>
        /// 记录缓存未命中
        /// </summary>
        /// <param name="loadTimeMs">加载耗时(毫秒)</param>
        public void RecordMiss(long loadTimeMs = 0)
        {
            Interlocked.Increment(ref _missCount);
            if (loadTimeMs > 0)
            {
                Interlocked.Add(ref _totalLoadTime, loadTimeMs);
            }
        }

        /// <summary>
        /// 记录缓存清理
        /// </summary>
        public void RecordEviction()
        {
            Interlocked.Increment(ref _evictionCount);
        }

        /// <summary>
        /// 记录预加载
        /// </summary>
        public void RecordPreload()
        {
            Interlocked.Increment(ref _preloadCount);
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void Reset()
        {
            _hitCount = 0;
            _missCount = 0;
            _evictionCount = 0;
            _preloadCount = 0;
            _totalLoadTime = 0;
        }

        /// <summary>
        /// 获取统计摘要
        /// </summary>
        /// <returns>统计摘要字符串</returns>
        public string GetSummary()
        {
            return $"缓存统计: 命中={HitCount}, 未命中={MissCount}, 命中率={HitRatio:P2}, " +
                   $"清理={EvictionCount}, 预加载={PreloadCount}, " +
                   $"平均加载时间={AverageLoadTime:F1}ms, 运行时间={Uptime.TotalMinutes:F1}分钟";
        }
    }

    /// <summary>
    /// 资源缓存使用报告
    /// </summary>
    public class ResourceCacheUsageReport
    {
        /// <summary>
        /// 总内存使用量(字节)
        /// </summary>
        public long TotalMemoryUsage { get; set; }

        /// <summary>
        /// 文件内容缓存数量
        /// </summary>
        public int FileContentCacheCount { get; set; }

        /// <summary>
        /// XML文档缓存数量
        /// </summary>
        public int XmlDocumentCacheCount { get; set; }

        /// <summary>
        /// 资源缓存数量
        /// </summary>
        public int ResourceCacheCount { get; set; }

        /// <summary>
        /// 缓存统计信息
        /// </summary>
        public ResourceCacheStatistics? Statistics { get; set; }

        /// <summary>
        /// 报告生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// 获取报告摘要
        /// </summary>
        /// <returns>报告摘要字符串</returns>
        public string GetSummary()
        {
            return $"缓存使用报告: 总内存={TotalMemoryUsage / 1024.0 / 1024.0:F1}MB, " +
                   $"文件缓存={FileContentCacheCount}, XML缓存={XmlDocumentCacheCount}, " +
                   $"资源缓存={ResourceCacheCount}, 生成时间={GeneratedAt:yyyy-MM-dd HH:mm:ss}";
        }
    }
}
