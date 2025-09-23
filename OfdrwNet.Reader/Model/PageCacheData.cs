using System;
using System.Collections.Generic;
using System.Drawing;

namespace OfdrwNet.Reader.Model
{
    /// <summary>
    /// 页面缓存数据
    /// 存储单个页面的缓存信息
    /// </summary>
    public class PageCacheData
    {
        /// <summary>
        /// 渲染后的位图
        /// </summary>
        public Bitmap? RenderedBitmap { get; set; }

        /// <summary>
        /// 缩略图位图
        /// </summary>
        public Bitmap? ThumbnailBitmap { get; set; }

        /// <summary>
        /// 对象缓存字典
        /// </summary>
        public Dictionary<string, object> ObjectCache { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 内存使用量(字节)
        /// </summary>
        public long MemoryUsage
        {
            get
            {
                long total = 0;

                if (RenderedBitmap != null)
                {
                    total += RenderedBitmap.Width * RenderedBitmap.Height * 4; // 假设RGBA
                }

                if (ThumbnailBitmap != null)
                {
                    total += ThumbnailBitmap.Width * ThumbnailBitmap.Height * 4;
                }

                // 估算对象缓存大小
                total += ObjectCache.Count * 256; // 每个对象假设256字节

                return total;
            }
        }

        /// <summary>
        /// 最大内存使用量限制
        /// </summary>
        public long MaxMemoryUsage { get; set; } = 50 * 1024 * 1024; // 默认50MB

        /// <summary>
        /// 缓存是否有效
        /// </summary>
        public bool IsValid => RenderedBitmap != null && DateTime.UtcNow - LastUpdate < TimeSpan.FromMinutes(30);

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            RenderedBitmap?.Dispose();
            RenderedBitmap = null;

            ThumbnailBitmap?.Dispose();
            ThumbnailBitmap = null;

            ObjectCache.Clear();
        }

        /// <summary>
        /// 清空缓存但保留结构
        /// </summary>
        public void Clear()
        {
            RenderedBitmap?.Dispose();
            RenderedBitmap = null;

            ThumbnailBitmap?.Dispose();
            ThumbnailBitmap = null;

            ObjectCache.Clear();
            LastUpdate = DateTime.UtcNow;
        }

        /// <summary>
        /// 设置渲染位图
        /// </summary>
        /// <param name="bitmap">位图</param>
        public void SetRenderedBitmap(Bitmap bitmap)
        {
            RenderedBitmap?.Dispose();
            RenderedBitmap = bitmap?.Clone() as Bitmap;
            LastUpdate = DateTime.UtcNow;
        }

        /// <summary>
        /// 设置缩略图位图
        /// </summary>
        /// <param name="bitmap">缩略图位图</param>
        public void SetThumbnailBitmap(Bitmap bitmap)
        {
            ThumbnailBitmap?.Dispose();
            ThumbnailBitmap = bitmap?.Clone() as Bitmap;
            LastUpdate = DateTime.UtcNow;
        }

        /// <summary>
        /// 添加对象到缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        public void AddObjectToCache(string key, object value)
        {
            ObjectCache[key] = value;
            LastUpdate = DateTime.UtcNow;
        }

        /// <summary>
        /// 从缓存获取对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存的对象</returns>
        public T? GetObjectFromCache<T>(string key) where T : class
        {
            return ObjectCache.TryGetValue(key, out var value) ? value as T : null;
        }

        /// <summary>
        /// 检查是否超过内存限制
        /// </summary>
        /// <returns>是否超过限制</returns>
        public bool IsOverMemoryLimit()
        {
            return MemoryUsage > MaxMemoryUsage;
        }

        /// <summary>
        /// 获取缓存摘要信息
        /// </summary>
        /// <returns>缓存摘要</returns>
        public string GetSummary()
        {
            return $"页面缓存: 渲染={RenderedBitmap != null}, " +
                   $"缩略图={ThumbnailBitmap != null}, " +
                   $"对象数={ObjectCache.Count}, " +
                   $"内存={MemoryUsage / 1024.0:F1}KB, " +
                   $"更新时间={LastUpdate:HH:mm:ss}";
        }
    }
}
