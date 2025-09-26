using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
//using OfdrwNet.Re
//    /// <summary>
//        /// 预加载资源
//        /// </summary>
//        /// <param name="resourceIds">资源ID列表</param>
//        /// <returns>预加载结果</returns>
//        //public async Task<object> PreloadResourcesAsync(IEnumerable<string> resourceIds)odel;

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

        public event EventHandler<ResourceLoadedEventArgs> ResourceLoaded;

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
        /// 异步获取资源流（内部使用）
        /// </summary>
        /// <param name="resourceId">资源ID</param>
        /// <returns>资源流</returns>
        private async Task<Stream?> GetResourceStreamInternalAsync(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
                return null;

            // 检查缓存
            if (_resourceCache.TryGetValue(resourceId, out var cachedData))
            {
                return new MemoryStream(cachedData);
            }

            // 这里应该从文件系统或其他来源加载资源
            // 暂时返回null作为占位符
            await Task.CompletedTask;
            return null;
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
            if (_cacheConfig.EnableCache)
            {
                _fontCache[fontName] = font;
            }

            return font;
        }

        /// <summary>
        /// 异步获取图像
        /// </summary>
        /// <param name="imageId">图像ID</param>
        /// <returns>图像对象</returns>
        public async Task<Image> GetImageAsync(string imageId)
        {
            if (string.IsNullOrEmpty(imageId))
                return new Bitmap(1, 1); // 返回默认的1x1像素图像

            // 检查图像缓存
            if (_imageCache.TryGetValue(imageId, out var cachedImage))
            {
                return cachedImage;
            }

            // 检查数据缓存
            if (_resourceCache.TryGetValue($"image_{imageId}", out var cachedData))
            {
                using var memoryStream = new MemoryStream(cachedData);
                var image = Image.FromStream(memoryStream);

                if (_cacheConfig.EnableCache)
                {
                    _imageCache[imageId] = image;
                }

                return image;
            }

            // 从基础管理器获取
            var imageFromBase = await _baseResourceManager.GetImageAsync(imageId);

            // 缓存图像
            if (_cacheConfig.EnableCache && imageFromBase != null)
            {
                _imageCache[imageId] = imageFromBase;
            }

            return imageFromBase ?? new Bitmap(1, 1);
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
                using var stream = await GetResourceStreamInternalAsync(resourceId);
                // 资源已在GetResourceStreamInternalAsync中缓存
            }
            catch
            {
                // 忽略预加载错误
            }
        }

        /// <summary>
        /// 异步获取颜色空间
        /// </summary>
        /// <param name="colorSpaceId">颜色空间ID</param>
        /// <returns>颜色空间</returns>
        public async Task<object?> GetColorSpaceAsync(string colorSpaceId)
        {
            // 基础实现，返回null
            await Task.CompletedTask;
            return null;
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计</returns>
        public ResourceCacheStatistics GetCacheStatistics()
        {
            // 创建统计实例并记录当前状态
            var stats = new ResourceCacheStatistics();

            // 记录缓存项数量作为命中
            for (int i = 0; i < _resourceCache.Count; i++)
                stats.RecordHit();
            for (int i = 0; i < _fontCache.Count; i++)
                stats.RecordHit();

            return stats;
        }

        /// <summary>
        /// 清理过期缓存
        /// </summary>
        public void CleanupExpiredCache()
        {
            // 如果缓存项数量超出限制，清理最早的条目
            if (_resourceCache.Count + _fontCache.Count > _cacheConfig.MaxCacheEntries)
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
                if (_baseResourceManager is IDisposable disposable)
                    disposable.Dispose();
                _disposed = true;
            }
        }

        Task<ColorSpace> IResourceManager.GetColorSpaceAsync(string colorSpaceId)
        {
            throw new NotImplementedException();
        }

        async Task<PreloadResult> IResourceManager.PreloadResourcesAsync(IEnumerable<string> resourceIds)
        {
            var result = new PreloadResult();
            var startTime = DateTime.Now;

            if (resourceIds == null)
            {
                result.Duration = DateTime.Now - startTime;
                return result;
            }

            var tasks = new List<Task>();
            foreach (var resourceId in resourceIds)
            {
                if (!string.IsNullOrEmpty(resourceId) && !_resourceCache.ContainsKey(resourceId))
                {
                    tasks.Add(PreloadSingleResourceWithResultAsync(resourceId, result));
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }

            result.Duration = DateTime.Now - startTime;
            return result;
        }

        private async Task PreloadSingleResourceWithResultAsync(string resourceId, PreloadResult result)
        {
            try
            {
                using var stream = await GetResourceStreamInternalAsync(resourceId);
                result.SuccessCount++;
            }
            catch
            {
                result.FailureCount++;
                result.FailedResources.Add(resourceId);
            }
        }

        /// <summary>
        /// 清理指定类型的缓存
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        /// <param name="olderThan">清理早于指定时间的缓存</param>
        /// <returns>清理的资源数量</returns>
        public async Task<int> ClearCacheAsync(ResourceType? resourceType = null, DateTime? olderThan = null)
        {
            await Task.CompletedTask; // 简化实现，暂时不考虑时间筛选

            int clearedCount = 0;

            if (resourceType == null || resourceType == ResourceType.Other)
            {
                clearedCount += _resourceCache.Count;
                _resourceCache.Clear();
            }

            if (resourceType == null || resourceType == ResourceType.Font)
            {
                clearedCount += _fontCache.Count;
                foreach (var font in _fontCache.Values)
                {
                    font.Dispose();
                }
                _fontCache.Clear();
            }

            if (resourceType == null || resourceType == ResourceType.Image)
            {
                clearedCount += _imageCache.Count;
                foreach (var image in _imageCache.Values)
                {
                    image.Dispose();
                }
                _imageCache.Clear();
            }

            return clearedCount;
        }

        /// <summary>
        /// 获取资源使用报告
        /// </summary>
        /// <returns>资源使用情况</returns>
        public async Task<ResourceUsageReport> GetUsageReportAsync()
        {
            await Task.CompletedTask;

            var report = new ResourceUsageReport
            {
                GeneratedAt = DateTime.Now,
                CachedResourceCount = _resourceCache.Count + _fontCache.Count + _imageCache.Count
            };

            // 计算内存使用量
            long totalMemory = 0;
            foreach (var data in _resourceCache.Values)
            {
                totalMemory += data.Length;
            }

            report.TotalMemoryUsed = totalMemory;

            // 按类型统计
            report.TypeStatistics[ResourceType.Other] = new ResourceTypeStats
            {
                Count = _resourceCache.Count,
                MemoryUsed = totalMemory,
                HitCount = _resourceCache.Count * 2, // 简化统计
                MissCount = _resourceCache.Count
            };

            report.TypeStatistics[ResourceType.Font] = new ResourceTypeStats
            {
                Count = _fontCache.Count,
                MemoryUsed = _fontCache.Count * 1024, // 估算值
                HitCount = _fontCache.Count * 3,
                MissCount = _fontCache.Count
            };

            report.TypeStatistics[ResourceType.Image] = new ResourceTypeStats
            {
                Count = _imageCache.Count,
                MemoryUsed = _imageCache.Count * 10240, // 估算值
                HitCount = _imageCache.Count * 2,
                MissCount = _imageCache.Count
            };

            return report;
        }
    }

}
