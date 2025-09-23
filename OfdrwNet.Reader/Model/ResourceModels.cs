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
    /// 资源管理器数据模型
    /// 管理OFD文档中的图像、字体、模板等资源的缓存和加载
    /// </summary>
    public class ResourceManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, ResourceCacheEntry> _imageCache = new();
        private readonly ConcurrentDictionary<string, ResourceCacheEntry> _fontCache = new();
        private readonly ConcurrentDictionary<string, ResourceCacheEntry> _templateCache = new();
        private readonly ResourceCacheOptions _options;
        private readonly Timer _cleanupTimer;
        private long _totalMemoryUsage;
        private bool _disposed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">缓存选项</param>
        public ResourceManager(ResourceCacheOptions? options = null)
        {
            _options = options ?? new ResourceCacheOptions();

            // 设置定期清理定时器
            _cleanupTimer = new Timer(CleanupExpiredEntries, null,
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// 当前内存使用量（字节）
        /// </summary>
        public long MemoryUsage => Interlocked.Read(ref _totalMemoryUsage);

        /// <summary>
        /// 缓存的图像数量
        /// </summary>
        public int ImageCacheCount => _imageCache.Count;

        /// <summary>
        /// 缓存的字体数量
        /// </summary>
        public int FontCacheCount => _fontCache.Count;

        /// <summary>
        /// 缓存的模板数量
        /// </summary>
        public int TemplateCacheCount => _templateCache.Count;

        /// <summary>
        /// 异步获取图像资源
        /// </summary>
        /// <param name="resourceId">资源ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>图像资源</returns>
        public async Task<ImageResource?> GetImageAsync(string resourceId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(resourceId))
                return null;

            // 尝试从缓存获取
            if (_imageCache.TryGetValue(resourceId, out var entry))
            {
                entry.LastAccessTime = DateTime.UtcNow;
                entry.AccessCount++;
                return entry.Resource as ImageResource;
            }

            // 检查内存限制
            if (MemoryUsage > _options.MaxMemoryUsage)
            {
                await EvictLeastRecentlyUsedAsync();
            }

            // 加载资源
            var resource = await LoadImageResourceAsync(resourceId, cancellationToken);
            if (resource != null)
            {
                var cacheEntry = new ResourceCacheEntry
                {
                    ResourceId = resourceId,
                    Resource = resource,
                    MemorySize = resource.EstimatedMemorySize,
                    CreatedTime = DateTime.UtcNow,
                    LastAccessTime = DateTime.UtcNow,
                    AccessCount = 1
                };

                _imageCache.TryAdd(resourceId, cacheEntry);
                Interlocked.Add(ref _totalMemoryUsage, resource.EstimatedMemorySize);
            }

            return resource;
        }

        /// <summary>
        /// 异步获取字体资源
        /// </summary>
        /// <param name="resourceId">资源ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>字体资源</returns>
        public async Task<FontResource?> GetFontAsync(string resourceId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(resourceId))
                return null;

            // 尝试从缓存获取
            if (_fontCache.TryGetValue(resourceId, out var entry))
            {
                entry.LastAccessTime = DateTime.UtcNow;
                entry.AccessCount++;
                return entry.Resource as FontResource;
            }

            // 检查内存限制
            if (MemoryUsage > _options.MaxMemoryUsage)
            {
                await EvictLeastRecentlyUsedAsync();
            }

            // 加载资源
            var resource = await LoadFontResourceAsync(resourceId, cancellationToken);
            if (resource != null)
            {
                var cacheEntry = new ResourceCacheEntry
                {
                    ResourceId = resourceId,
                    Resource = resource,
                    MemorySize = resource.EstimatedMemorySize,
                    CreatedTime = DateTime.UtcNow,
                    LastAccessTime = DateTime.UtcNow,
                    AccessCount = 1
                };

                _fontCache.TryAdd(resourceId, cacheEntry);
                Interlocked.Add(ref _totalMemoryUsage, resource.EstimatedMemorySize);
            }

            return resource;
        }

        /// <summary>
        /// 异步获取模板资源
        /// </summary>
        /// <param name="resourceId">资源ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>模板资源</returns>
        public async Task<TemplateResource?> GetTemplateAsync(string resourceId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(resourceId))
                return null;

            // 尝试从缓存获取
            if (_templateCache.TryGetValue(resourceId, out var entry))
            {
                entry.LastAccessTime = DateTime.UtcNow;
                entry.AccessCount++;
                return entry.Resource as TemplateResource;
            }

            // 检查内存限制
            if (MemoryUsage > _options.MaxMemoryUsage)
            {
                await EvictLeastRecentlyUsedAsync();
            }

            // 加载资源
            var resource = await LoadTemplateResourceAsync(resourceId, cancellationToken);
            if (resource != null)
            {
                var cacheEntry = new ResourceCacheEntry
                {
                    ResourceId = resourceId,
                    Resource = resource,
                    MemorySize = resource.EstimatedMemorySize,
                    CreatedTime = DateTime.UtcNow,
                    LastAccessTime = DateTime.UtcNow,
                    AccessCount = 1
                };

                _templateCache.TryAdd(resourceId, cacheEntry);
                Interlocked.Add(ref _totalMemoryUsage, resource.EstimatedMemorySize);
            }

            return resource;
        }

        /// <summary>
        /// 预加载资源
        /// </summary>
        /// <param name="resourceIds">资源ID列表</param>
        /// <param name="resourceType">资源类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task PreloadResourcesAsync(IEnumerable<string> resourceIds, ResourceType resourceType, CancellationToken cancellationToken = default)
        {
            var tasks = resourceIds.Select(async resourceId =>
            {
                try
                {
                    switch (resourceType)
                    {
                        case ResourceType.Image:
                            await GetImageAsync(resourceId, cancellationToken);
                            break;
                        case ResourceType.Font:
                            await GetFontAsync(resourceId, cancellationToken);
                            break;
                        case ResourceType.Template:
                            await GetTemplateAsync(resourceId, cancellationToken);
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    // 忽略取消异常
                }
                catch
                {
                    // 忽略加载错误，继续预加载其他资源
                }
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 清除特定资源缓存
        /// </summary>
        /// <param name="resourceId">资源ID</param>
        /// <param name="resourceType">资源类型</param>
        /// <returns>是否成功清除</returns>
        public bool ClearResource(string resourceId, ResourceType resourceType)
        {
            var cache = GetCacheByType(resourceType);
            if (cache.TryRemove(resourceId, out var entry))
            {
                Interlocked.Add(ref _totalMemoryUsage, -entry.MemorySize);
                entry.Resource?.Dispose();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearAllCaches()
        {
            ClearCache(_imageCache);
            ClearCache(_fontCache);
            ClearCache(_templateCache);
            Interlocked.Exchange(ref _totalMemoryUsage, 0);
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计信息</returns>
        public ResourceCacheStatistics GetCacheStatistics()
        {
            return new ResourceCacheStatistics
            {
                TotalMemoryUsage = MemoryUsage,
                ImageCacheCount = ImageCacheCount,
                FontCacheCount = FontCacheCount,
                TemplateCacheCount = TemplateCacheCount,
                ImageCacheStats = GetCacheTypeStatistics(_imageCache),
                FontCacheStats = GetCacheTypeStatistics(_fontCache),
                TemplateCacheStats = GetCacheTypeStatistics(_templateCache)
            };
        }

        private async Task<ImageResource?> LoadImageResourceAsync(string resourceId, CancellationToken cancellationToken)
        {
            // TODO: 实现实际的图像资源加载逻辑
            // 这里提供一个示例实现
            await Task.Delay(10, cancellationToken); // 模拟异步加载

            var bitmap = new Bitmap(100, 100);
            return new ImageResource
            {
                ResourceId = resourceId,
                Image = bitmap,
                Width = bitmap.Width,
                Height = bitmap.Height,
                Format = ImageFormat.Png,
                EstimatedMemorySize = bitmap.Width * bitmap.Height * 4 // RGBA
            };
        }

        private async Task<FontResource?> LoadFontResourceAsync(string resourceId, CancellationToken cancellationToken)
        {
            // TODO: 实现实际的字体资源加载逻辑
            await Task.Delay(10, cancellationToken); // 模拟异步加载

            return new FontResource
            {
                ResourceId = resourceId,
                FontFamily = "Arial",
                EstimatedMemorySize = 1024 // 示例大小
            };
        }

        private async Task<TemplateResource?> LoadTemplateResourceAsync(string resourceId, CancellationToken cancellationToken)
        {
            // TODO: 实现实际的模板资源加载逻辑
            await Task.Delay(10, cancellationToken); // 模拟异步加载

            return new TemplateResource
            {
                ResourceId = resourceId,
                TemplateData = new byte[1024], // 示例数据
                EstimatedMemorySize = 1024
            };
        }

        private async Task EvictLeastRecentlyUsedAsync()
        {
            var allEntries = _imageCache.Values
                .Concat(_fontCache.Values)
                .Concat(_templateCache.Values)
                .OrderBy(e => e.LastAccessTime)
                .ToList();

            var targetMemory = (long)(_options.MaxMemoryUsage * 0.8); // 清理到80%
            var currentMemory = MemoryUsage;

            foreach (var entry in allEntries)
            {
                if (currentMemory <= targetMemory)
                    break;

                var cache = GetCacheByResourceId(entry.ResourceId);
                if (cache.TryRemove(entry.ResourceId, out var removedEntry))
                {
                    Interlocked.Add(ref _totalMemoryUsage, -removedEntry.MemorySize);
                    removedEntry.Resource?.Dispose();
                    currentMemory -= removedEntry.MemorySize;
                }
            }

            await Task.CompletedTask;
        }

        private void CleanupExpiredEntries(object? state)
        {
            var expiredTime = DateTime.UtcNow - _options.ExpirationTime;

            CleanupExpiredInCache(_imageCache, expiredTime);
            CleanupExpiredInCache(_fontCache, expiredTime);
            CleanupExpiredInCache(_templateCache, expiredTime);
        }

        private void CleanupExpiredInCache(ConcurrentDictionary<string, ResourceCacheEntry> cache, DateTime expiredTime)
        {
            var expiredKeys = cache.Where(kvp => kvp.Value.LastAccessTime < expiredTime)
                                  .Select(kvp => kvp.Key)
                                  .ToList();

            foreach (var key in expiredKeys)
            {
                if (cache.TryRemove(key, out var entry))
                {
                    Interlocked.Add(ref _totalMemoryUsage, -entry.MemorySize);
                    entry.Resource?.Dispose();
                }
            }
        }

        private ConcurrentDictionary<string, ResourceCacheEntry> GetCacheByType(ResourceType resourceType)
        {
            return resourceType switch
            {
                ResourceType.Image => _imageCache,
                ResourceType.Font => _fontCache,
                ResourceType.Template => _templateCache,
                _ => throw new ArgumentException($"不支持的资源类型: {resourceType}")
            };
        }

        private ConcurrentDictionary<string, ResourceCacheEntry> GetCacheByResourceId(string resourceId)
        {
            // 简化实现：按顺序检查各个缓存
            if (_imageCache.ContainsKey(resourceId)) return _imageCache;
            if (_fontCache.ContainsKey(resourceId)) return _fontCache;
            return _templateCache;
        }

        private void ClearCache(ConcurrentDictionary<string, ResourceCacheEntry> cache)
        {
            foreach (var entry in cache.Values)
            {
                entry.Resource?.Dispose();
            }
            cache.Clear();
        }

        private CacheTypeStatistics GetCacheTypeStatistics(ConcurrentDictionary<string, ResourceCacheEntry> cache)
        {
            var entries = cache.Values.ToList();
            return new CacheTypeStatistics
            {
                Count = entries.Count,
                TotalMemoryUsage = entries.Sum(e => e.MemorySize),
                AverageAccessCount = entries.Count > 0 ? entries.Average(e => e.AccessCount) : 0,
                OldestEntryAge = entries.Count > 0 ?
                    DateTime.UtcNow - entries.Min(e => e.CreatedTime) : TimeSpan.Zero
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cleanupTimer?.Dispose();
                ClearAllCaches();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 资源缓存条目
    /// </summary>
    public class ResourceCacheEntry
    {
        /// <summary>
        /// 资源ID
        /// </summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// 资源对象
        /// </summary>
        public IResource? Resource { get; set; }

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
    /// 资源缓存选项
    /// </summary>
    public class ResourceCacheOptions
    {
        /// <summary>
        /// 最大内存使用量（字节）
        /// </summary>
        public long MaxMemoryUsage { get; set; } = 500 * 1024 * 1024; // 500MB

        /// <summary>
        /// 资源过期时间
        /// </summary>
        public TimeSpan ExpirationTime { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// 是否启用预加载
        /// </summary>
        public bool EnablePreloading { get; set; } = true;

        /// <summary>
        /// 最大并发加载数
        /// </summary>
        public int MaxConcurrentLoads { get; set; } = Environment.ProcessorCount;
    }

    /// <summary>
    /// 资源基础接口
    /// </summary>
    public interface IResource : IDisposable
    {
        /// <summary>
        /// 资源ID
        /// </summary>
        string ResourceId { get; set; }

        /// <summary>
        /// 估计内存大小
        /// </summary>
        long EstimatedMemorySize { get; set; }
    }

    /// <summary>
    /// 图像资源
    /// </summary>
    public class ImageResource : IResource
    {
        /// <summary>
        /// 资源ID
        /// </summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// 估计内存大小
        /// </summary>
        public long EstimatedMemorySize { get; set; }

        /// <summary>
        /// 图像对象
        /// </summary>
        public Image? Image { get; set; }

        /// <summary>
        /// 图像宽度
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 图像高度
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 图像格式
        /// </summary>
        public ImageFormat? Format { get; set; }

        public void Dispose()
        {
            Image?.Dispose();
        }
    }

    /// <summary>
    /// 字体资源
    /// </summary>
    public class FontResource : IResource
    {
        /// <summary>
        /// 资源ID
        /// </summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// 估计内存大小
        /// </summary>
        public long EstimatedMemorySize { get; set; }

        /// <summary>
        /// 字体族名称
        /// </summary>
        public string FontFamily { get; set; } = string.Empty;

        /// <summary>
        /// 字体数据
        /// </summary>
        public byte[]? FontData { get; set; }

        public void Dispose()
        {
            // 字体资源通常不需要特殊清理
        }
    }

    /// <summary>
    /// 模板资源
    /// </summary>
    public class TemplateResource : IResource
    {
        /// <summary>
        /// 资源ID
        /// </summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// 估计内存大小
        /// </summary>
        public long EstimatedMemorySize { get; set; }

        /// <summary>
        /// 模板数据
        /// </summary>
        public byte[]? TemplateData { get; set; }

        public void Dispose()
        {
            // 模板资源通常不需要特殊清理
        }
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class ResourceCacheStatistics
    {
        /// <summary>
        /// 总内存使用量
        /// </summary>
        public long TotalMemoryUsage { get; set; }

        /// <summary>
        /// 图像缓存数量
        /// </summary>
        public int ImageCacheCount { get; set; }

        /// <summary>
        /// 字体缓存数量
        /// </summary>
        public int FontCacheCount { get; set; }

        /// <summary>
        /// 模板缓存数量
        /// </summary>
        public int TemplateCacheCount { get; set; }

        /// <summary>
        /// 图像缓存统计
        /// </summary>
        public CacheTypeStatistics? ImageCacheStats { get; set; }

        /// <summary>
        /// 字体缓存统计
        /// </summary>
        public CacheTypeStatistics? FontCacheStats { get; set; }

        /// <summary>
        /// 模板缓存统计
        /// </summary>
        public CacheTypeStatistics? TemplateCacheStats { get; set; }
    }

    /// <summary>
    /// 特定类型缓存统计
    /// </summary>
    public class CacheTypeStatistics
    {
        /// <summary>
        /// 缓存条目数量
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 总内存使用量
        /// </summary>
        public long TotalMemoryUsage { get; set; }

        /// <summary>
        /// 平均访问次数
        /// </summary>
        public double AverageAccessCount { get; set; }

        /// <summary>
        /// 最老条目的年龄
        /// </summary>
        public TimeSpan OldestEntryAge { get; set; }
    }
}
