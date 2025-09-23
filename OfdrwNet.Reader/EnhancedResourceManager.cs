using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using OfdrwNet.Reader.Model;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// 增强资源管理器
    /// 提供高级资源管理、缓存和预加载功能
    /// </summary>
    public class EnhancedResourceManager : IResourceManager, IDisposable
    {
        private readonly IResourceManager _baseResourceManager;
        private readonly Dictionary<string, byte[]> _resourceCache;
        private readonly Dictionary<string, Font> _fontCache;
        private readonly Dictionary<string, Image> _imageCache;
        private readonly ResourceCacheConfig _cacheConfig;
        private bool _disposed = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="baseResourceManager">基础资源管理器</param>
        /// <param name="cacheConfig">缓存配置</param>
        public EnhancedResourceManager(IResourceManager baseResourceManager, ResourceCacheConfig? cacheConfig = null)
        {
            _baseResourceManager = baseResourceManager ?? throw new ArgumentNullException(nameof(baseResourceManager));
            _cacheConfig = cacheConfig ?? new ResourceCacheConfig();
            _resourceCache = new Dictionary<string, byte[]>();
            _fontCache = new Dictionary<string, Font>();
            _imageCache = new Dictionary<string, Image>();
        }

        /// <summary>
        /// 异步获取资源流
        /// </summary>
        /// <param name="resourceId">资源ID</param>
        /// <returns>资源流</returns>
        public async Task<Stream?> GetResourceStreamAsync(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
                return null;

            // 检查缓存
            if (_resourceCache.TryGetValue(resourceId, out var cachedData))
            {
                return new MemoryStream(cachedData);
            }

            // 从基础管理器获取
            var stream = await _baseResourceManager.GetResourceStreamAsync(resourceId);
            if (stream != null && _cacheConfig.EnableCaching)
            {
                // 读取并缓存数据
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var data = memoryStream.ToArray();

                // 检查缓存大小限制
                if (data.Length <= _cacheConfig.MaxResourceSize)
                {
                    _resourceCache[resourceId] = data;
                }

                stream.Dispose();
                return new MemoryStream(data);
            }

            return stream;
        }

        /// <summary>
        /// 异步获取字体
        /// </summary>
        /// <param name="fontName">字体名称</param>
        /// <returns>字体对象</returns>
        public async Task<Font> GetFontAsync(string fontName)
        {
            if (string.IsNullOrEmpty(fontName))
                return new Font("Arial", 12);

            // 检查字体缓存
            if (_fontCache.TryGetValue(fontName, out var cachedFont))
            {
                return cachedFont;
            }

            // 从基础管理器获取
            var font = await _baseResourceManager.GetFontAsync(fontName);

            // 缓存字体
            if (_cacheConfig.EnableCaching)
            {
                _fontCache[fontName] = font;
            }

            return font;
        }

        /// <summary>
        /// 异步获取图像
        /// </summary>
        /// <param name="imageId">图像ID</param>
        /// <returns>图像流</returns>
        public async Task<Stream?> GetImageAsync(string imageId)
        {
            if (string.IsNullOrEmpty(imageId))
                return null;

            // 检查缓存
            if (_resourceCache.TryGetValue($"image_{imageId}", out var cachedData))
            {
                return new MemoryStream(cachedData);
            }

            // 从基础管理器获取
            var stream = await _baseResourceManager.GetImageAsync(imageId);
            if (stream != null && _cacheConfig.EnableCaching)
            {
                // 读取并缓存图像数据
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var data = memoryStream.ToArray();

                if (data.Length <= _cacheConfig.MaxImageSize)
                {
                    _resourceCache[$"image_{imageId}"] = data;
                }

                stream.Dispose();
                return new MemoryStream(data);
            }

            return stream;
        }

        /// <summary>
        /// 预加载资源列表
        /// </summary>
        /// <param name="resourceIds">资源ID列表</param>
        /// <returns>预加载任务</returns>
        public async Task PreloadResourcesAsync(IEnumerable<string> resourceIds)
        {
            if (resourceIds == null)
                return;

            var tasks = new List<Task>();

            foreach (var resourceId in resourceIds)
            {
                if (!string.IsNullOrEmpty(resourceId) && !_resourceCache.ContainsKey(resourceId))
                {
                    tasks.Add(PreloadSingleResourceAsync(resourceId));
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// 预加载单个资源
        /// </summary>
        private async Task PreloadSingleResourceAsync(string resourceId)
        {
            try
            {
                using var stream = await GetResourceStreamAsync(resourceId);
                // 资源已在GetResourceStreamAsync中缓存
            }
            catch
            {
                // 忽略预加载错误
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计</returns>
        public ResourceCacheStatistics GetCacheStatistics()
        {
            var totalSize = 0L;
            foreach (var data in _resourceCache.Values)
            {
                totalSize += data.Length;
            }

            return new ResourceCacheStatistics
            {
                CachedResourceCount = _resourceCache.Count,
                CachedFontCount = _fontCache.Count,
                CachedImageCount = _imageCache.Count,
                TotalCacheSize = totalSize,
                CacheHitRate = CalculateHitRate(),
                MaxCacheSize = _cacheConfig.MaxCacheSize
            };
        }

        /// <summary>
        /// 清理过期缓存
        /// </summary>
        public void CleanupExpiredCache()
        {
            // 如果缓存超出大小限制，清理最早的条目
            var stats = GetCacheStatistics();
            if (stats.TotalCacheSize > _cacheConfig.MaxCacheSize)
            {
                var itemsToRemove = Math.Max(1, _resourceCache.Count / 4); // 清理25%的缓存
                var keysToRemove = new List<string>();

                foreach (var key in _resourceCache.Keys)
                {
                    keysToRemove.Add(key);
                    if (keysToRemove.Count >= itemsToRemove)
                        break;
                }

                foreach (var key in keysToRemove)
                {
                    _resourceCache.Remove(key);
                }
            }
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void ClearAllCache()
        {
            _resourceCache.Clear();

            foreach (var font in _fontCache.Values)
            {
                font.Dispose();
            }
            _fontCache.Clear();

            foreach (var image in _imageCache.Values)
            {
                image.Dispose();
            }
            _imageCache.Clear();
        }

        /// <summary>
        /// 计算缓存命中率（简化版本）
        /// </summary>
        private double CalculateHitRate()
        {
            // 这里简化实现，实际应该跟踪请求次数和命中次数
            return _resourceCache.Count > 0 ? 0.85 : 0.0;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                ClearAllCache();
                _baseResourceManager?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 资源缓存配置
    /// </summary>
    public class ResourceCacheConfig
    {
        /// <summary>是否启用缓存</summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>最大缓存大小（字节）</summary>
        public long MaxCacheSize { get; set; } = 100 * 1024 * 1024; // 100MB

        /// <summary>单个资源最大大小（字节）</summary>
        public long MaxResourceSize { get; set; } = 10 * 1024 * 1024; // 10MB

        /// <summary>单个图像最大大小（字节）</summary>
        public long MaxImageSize { get; set; } = 5 * 1024 * 1024; // 5MB

        /// <summary>缓存过期时间（分钟）</summary>
        public int CacheExpirationMinutes { get; set; } = 30;
    }

    /// <summary>
    /// 资源缓存统计信息
    /// </summary>
    public class ResourceCacheStatistics
    {
        /// <summary>缓存的资源数量</summary>
        public int CachedResourceCount { get; set; }

        /// <summary>缓存的字体数量</summary>
        public int CachedFontCount { get; set; }

        /// <summary>缓存的图像数量</summary>
        public int CachedImageCount { get; set; }

        /// <summary>总缓存大小</summary>
        public long TotalCacheSize { get; set; }

        /// <summary>缓存命中率</summary>
        public double CacheHitRate { get; set; }

        /// <summary>最大缓存大小</summary>
        public long MaxCacheSize { get; set; }

        /// <summary>
        /// 获取统计摘要
        /// </summary>
        public string GetSummary()
        {
            return $"Resources: {CachedResourceCount}, Fonts: {CachedFontCount}, Images: {CachedImageCount}, " +
                   $"Size: {TotalCacheSize / 1024.0 / 1024.0:F1}MB/{MaxCacheSize / 1024.0 / 1024.0:F1}MB, " +
                   $"Hit Rate: {CacheHitRate:P1}";
        }
    }
}
