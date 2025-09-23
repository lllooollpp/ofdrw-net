using System;
using System.Collections.Generic;
using System.Drawing;
using OfdrwNet.Reader.Model;

namespace OfdrwNet.Reader.Navigation
{
    /// <summary>
    /// 缩略图缓存管理器
    /// 管理页面缩略图的生成与缓存
    /// </summary>
    public class ThumbnailCache : IDisposable
    {
        private readonly Dictionary<int, Bitmap> _cache = new();
        private readonly int _maxCacheSize;
        private bool _disposed = false;

        public ThumbnailCache(int maxCacheSize = 32)
        {
            _maxCacheSize = maxCacheSize;
        }

        /// <summary>
        /// 获取缩略图
        /// </summary>
        public Bitmap? GetThumbnail(int pageIndex)
        {
            _cache.TryGetValue(pageIndex, out var bmp);
            return bmp;
        }

        /// <summary>
        /// 添加缩略图
        /// </summary>
        public void AddThumbnail(int pageIndex, Bitmap thumbnail)
        {
            if (_cache.Count >= _maxCacheSize)
            {
                // 移除最早的一个
                var firstKey = new List<int>(_cache.Keys)[0];
                _cache[firstKey].Dispose();
                _cache.Remove(firstKey);
            }
            _cache[pageIndex] = thumbnail;
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear()
        {
            foreach (var bmp in _cache.Values)
            {
                bmp.Dispose();
            }
            _cache.Clear();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Clear();
                _disposed = true;
            }
        }
    }
}
