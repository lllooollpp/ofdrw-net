using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// 资源管理器接口，管理文档资源的加载、缓存和释放
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// 获取字体资源
        /// </summary>
        /// <param name="fontId">字体ID</param>
        /// <returns>字体对象</returns>
        Task<Font> GetFontAsync(string fontId);

        /// <summary>
        /// 获取图像资源
        /// </summary>
        /// <param name="imageId">图像ID</param>
        /// <returns>图像对象</returns>
        Task<Image> GetImageAsync(string imageId);

        /// <summary>
        /// 获取颜色空间资源
        /// </summary>
        /// <param name="colorSpaceId">颜色空间ID</param>
        /// <returns>颜色空间对象</returns>
        Task<ColorSpace> GetColorSpaceAsync(string colorSpaceId);

        /// <summary>
        /// 预加载指定资源
        /// </summary>
        /// <param name="resourceIds">资源ID列表</param>
        /// <returns>预加载结果</returns>
        Task<PreloadResult> PreloadResourcesAsync(IEnumerable<string> resourceIds);

        /// <summary>
        /// 清理指定类型的缓存
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        /// <param name="olderThan">清理早于指定时间的缓存</param>
        /// <returns>清理的资源数量</returns>
        Task<int> ClearCacheAsync(ResourceType? resourceType = null, DateTime? olderThan = null);

        /// <summary>
        /// 获取资源使用报告
        /// </summary>
        /// <returns>资源使用情况</returns>
        Task<ResourceUsageReport> GetUsageReportAsync();

        /// <summary>
        /// 资源加载完成事件
        /// </summary>
        event EventHandler<ResourceLoadedEventArgs> ResourceLoaded;
    }

    /// <summary>
    /// 颜色空间定义
    /// </summary>
    public class ColorSpace
    {
        /// <summary>
        /// 颜色空间标识符
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 颜色空间类型
        /// </summary>
        public ColorSpaceType Type { get; set; }

        /// <summary>
        /// 颜色空间数据
        /// </summary>
        public object? Data { get; set; }
    }

    /// <summary>
    /// 颜色空间类型
    /// </summary>
    public enum ColorSpaceType
    {
        /// <summary>
        /// RGB颜色空间
        /// </summary>
        RGB,

        /// <summary>
        /// CMYK颜色空间
        /// </summary>
        CMYK,

        /// <summary>
        /// 灰度颜色空间
        /// </summary>
        Gray,

        /// <summary>
        /// Lab颜色空间
        /// </summary>
        Lab,

        /// <summary>
        /// ICC颜色空间
        /// </summary>
        ICC
    }

    /// <summary>
    /// 预加载结果
    /// </summary>
    public class PreloadResult
    {
        /// <summary>
        /// 成功加载的资源数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 加载失败的资源数量
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 失败的资源ID列表
        /// </summary>
        public List<string> FailedResources { get; set; } = new List<string>();

        /// <summary>
        /// 预加载耗时
        /// </summary>
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// 资源使用报告
    /// </summary>
    public class ResourceUsageReport
    {
        /// <summary>
        /// 总内存使用量(字节)
        /// </summary>
        public long TotalMemoryUsed { get; set; }

        /// <summary>
        /// 缓存的资源数量
        /// </summary>
        public int CachedResourceCount { get; set; }

        /// <summary>
        /// 按类型的统计信息
        /// </summary>
        public Dictionary<ResourceType, ResourceTypeStats> TypeStatistics { get; set; } = new Dictionary<ResourceType, ResourceTypeStats>();

        /// <summary>
        /// 报告生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// 资源类型统计
    /// </summary>
    public class ResourceTypeStats
    {
        /// <summary>
        /// 资源数量
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 内存使用量(字节)
        /// </summary>
        public long MemoryUsed { get; set; }

        /// <summary>
        /// 缓存命中次数
        /// </summary>
        public int HitCount { get; set; }

        /// <summary>
        /// 缓存未命中次数
        /// </summary>
        public int MissCount { get; set; }

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double HitRatio => HitCount / (double)(HitCount + MissCount);
    }

    /// <summary>
    /// 资源类型
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// 字体资源
        /// </summary>
        Font,

        /// <summary>
        /// 图像资源
        /// </summary>
        Image,

        /// <summary>
        /// 颜色空间资源
        /// </summary>
        ColorSpace,

        /// <summary>
        /// 矢量图形资源
        /// </summary>
        Vector,

        /// <summary>
        /// 其他资源
        /// </summary>
        Other
    }

    /// <summary>
    /// 资源加载事件参数
    /// </summary>
    public class ResourceLoadedEventArgs : EventArgs
    {
        /// <summary>
        /// 资源标识符
        /// </summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// 资源类型
        /// </summary>
        public ResourceType ResourceType { get; set; }

        /// <summary>
        /// 资源大小(字节)
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 加载耗时
        /// </summary>
        public TimeSpan LoadDuration { get; set; }

        /// <summary>
        /// 是否从缓存加载
        /// </summary>
        public bool FromCache { get; set; }
    }
}
