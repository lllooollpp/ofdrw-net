using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Container;

namespace OfdrwNet.Core.Resource;

/// <summary>
/// OFD图像资源管理器
/// 负责统一管理OFD文档中的所有图像资源
/// </summary>
public class ImageResourceManager : IDisposable
{
    /// <summary>
    /// 图像资源缓存
    /// Key: 资源ID，Value: 图像资源
    /// </summary>
    private readonly Dictionary<string, ImageResource> _imageCache;

    /// <summary>
    /// 图像资源路径映射
    /// Key: 文件路径，Value: 资源ID
    /// </summary>
    private readonly Dictionary<string, string> _pathToIdMapping;

    /// <summary>
    /// 资源ID生成器
    /// </summary>
    private int _nextResourceId = 1;

    /// <summary>
    /// 虚拟容器引用
    /// </summary>
    private readonly IContainer _container;

    /// <summary>
    /// 资源目录路径
    /// </summary>
    public string ResourceDirectory { get; set; } = "Res";

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="container">虚拟容器</param>
    public ImageResourceManager(IContainer container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _imageCache = new Dictionary<string, ImageResource>();
        _pathToIdMapping = new Dictionary<string, string>();
    }

    /// <summary>
    /// 添加图像资源
    /// </summary>
    /// <param name="filePath">图像文件路径</param>
    /// <returns>图像资源对象</returns>
    public ImageResource AddImage(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        }

        // 检查是否已经添加过此文件
        if (_pathToIdMapping.TryGetValue(filePath, out var existingId))
        {
            return _imageCache[existingId];
        }

        // 创建新的图像资源
        var resourceId = GenerateResourceId();
        var imageResource = ImageResource.FromFile(resourceId, filePath);
        
        // 生成资源路径
        imageResource.GenerateResourcePath(ResourceDirectory);

        // 添加到缓存
        _imageCache[resourceId.ToString()] = imageResource;
        _pathToIdMapping[filePath] = resourceId.ToString();

        return imageResource;
    }

    /// <summary>
    /// 添加图像资源（从字节数组）
    /// </summary>
    /// <param name="imageData">图像数据</param>
    /// <param name="format">图像格式</param>
    /// <param name="name">资源名称（可选）</param>
    /// <returns>图像资源对象</returns>
    public ImageResource AddImage(byte[] imageData, ImageFormat format, string? name = null)
    {
        if (imageData == null || imageData.Length == 0)
        {
            throw new ArgumentException("图像数据不能为空", nameof(imageData));
        }

        // 生成资源ID
        var resourceId = GenerateResourceId();
        var imageResource = ImageResource.FromBytes(resourceId, imageData, format);
        
        // 生成资源路径
        imageResource.GenerateResourcePath(ResourceDirectory);

        // 如果提供了名称，使用自定义名称
        if (!string.IsNullOrEmpty(name))
        {
            var extension = imageResource.GetFileExtension();
            imageResource.ResourcePath = new StLoc($"/{ResourceDirectory}/{name}{extension}");
        }

        // 添加到缓存
        _imageCache[resourceId.ToString()] = imageResource;

        return imageResource;
    }

    /// <summary>
    /// 获取图像资源
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <returns>图像资源对象，如果不存在则返回null</returns>
    public ImageResource? GetImage(StId resourceId)
    {
        return _imageCache.TryGetValue(resourceId.ToString(), out var resource) ? resource : null;
    }

    /// <summary>
    /// 根据文件路径获取图像资源
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>图像资源对象，如果不存在则返回null</returns>
    public ImageResource? GetImageByPath(string filePath)
    {
        if (_pathToIdMapping.TryGetValue(filePath, out var resourceId))
        {
            return _imageCache.TryGetValue(resourceId, out var resource) ? resource : null;
        }
        return null;
    }

    /// <summary>
    /// 移除图像资源
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveImage(StId resourceId)
    {
        var id = resourceId.ToString();
        if (_imageCache.TryGetValue(id, out var resource))
        {
            // 从路径映射中移除
            var pathToRemove = _pathToIdMapping.FirstOrDefault(kvp => kvp.Value == id).Key;
            if (!string.IsNullOrEmpty(pathToRemove))
            {
                _pathToIdMapping.Remove(pathToRemove);
            }

            // 释放资源
            resource.Dispose();
            
            // 从缓存中移除
            _imageCache.Remove(id);
            
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取所有图像资源
    /// </summary>
    /// <returns>图像资源列表</returns>
    public List<ImageResource> GetAllImages()
    {
        return _imageCache.Values.ToList();
    }

    /// <summary>
    /// 获取图像资源数量
    /// </summary>
    /// <returns>资源数量</returns>
    public int GetImageCount()
    {
        return _imageCache.Count;
    }

    /// <summary>
    /// 检查是否包含指定的图像资源
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <returns>是否包含</returns>
    public bool ContainsImage(StId resourceId)
    {
        return _imageCache.ContainsKey(resourceId.ToString());
    }

    /// <summary>
    /// 将所有图像资源写入容器
    /// </summary>
    /// <param name="containerFactory">容器工厂函数</param>
    /// <returns>异步任务</returns>
    public async Task FlushToContainerAsync(Func<IContainer> containerFactory)
    {
        // 确保资源目录存在
        var resContainer = _container.ObtainContainer(ResourceDirectory, 
            containerFactory);

        foreach (var imageResource in _imageCache.Values)
        {
            if (imageResource.ImageData != null && imageResource.ResourcePath != null)
            {
                var fileName = Path.GetFileName(imageResource.ResourcePath.ToString());
                using var stream = new MemoryStream(imageResource.ImageData);
                resContainer.AddRaw(fileName, stream);
            }
        }

        await resContainer.FlushAsync();
    }

    /// <summary>
    /// 生成公共资源XML
    /// </summary>
    /// <returns>公共资源XML内容</returns>
    public string GeneratePublicResourceXml()
    {
        var xml = "<?xml version='1.0' encoding='UTF-8'?>\n";
        xml += "<ofd:Res xmlns:ofd='http://www.ofdspec.org/2016'>\n";
        
        if (_imageCache.Count > 0)
        {
            xml += "  <ofd:MultiMedias>\n";
            foreach (var imageResource in _imageCache.Values)
            {
                xml += $"    <ofd:MultiMedia ID='{imageResource.Id}' Type='{imageResource.Type}' Format='{imageResource.Format}'>\n";
                xml += $"      <ofd:MediaFile>{imageResource.ResourcePath}</ofd:MediaFile>\n";
                xml += $"    </ofd:MultiMedia>\n";
            }
            xml += "  </ofd:MultiMedias>\n";
        }
        
        xml += "</ofd:Res>";
        return xml;
    }

    /// <summary>
    /// 验证所有图像资源的完整性
    /// </summary>
    /// <returns>验证结果</returns>
    public Dictionary<string, bool> ValidateAllImages()
    {
        var results = new Dictionary<string, bool>();
        
        foreach (var kvp in _imageCache)
        {
            results[kvp.Key] = kvp.Value.ValidateIntegrity();
        }
        
        return results;
    }

    /// <summary>
    /// 压缩所有JPEG图像
    /// </summary>
    /// <param name="quality">压缩质量（0-100）</param>
    public void CompressJpegImages(int quality = 85)
    {
        foreach (var imageResource in _imageCache.Values)
        {
            if (imageResource.Format == ImageFormat.JPEG)
            {
                var compressedData = imageResource.Compress(quality);
                imageResource.ImageData = compressedData;
            }
        }
    }

    /// <summary>
    /// 统计图像资源信息
    /// </summary>
    /// <returns>统计信息</returns>
    public ImageResourceStatistics GetStatistics()
    {
        var stats = new ImageResourceStatistics();
        
        foreach (var imageResource in _imageCache.Values)
        {
            stats.TotalCount++;
            stats.TotalSize += imageResource.ImageData?.Length ?? 0;
            
            switch (imageResource.Format)
            {
                case ImageFormat.PNG:
                    stats.PngCount++;
                    break;
                case ImageFormat.JPEG:
                    stats.JpegCount++;
                    break;
                case ImageFormat.GIF:
                    stats.GifCount++;
                    break;
                case ImageFormat.BMP:
                    stats.BmpCount++;
                    break;
                case ImageFormat.TIFF:
                    stats.TiffCount++;
                    break;
            }
        }
        
        return stats;
    }

    /// <summary>
    /// 生成下一个资源ID
    /// </summary>
    /// <returns>资源ID</returns>
    private StId GenerateResourceId()
    {
        return new StId(_nextResourceId++);
    }

    /// <summary>
    /// 清空所有图像资源
    /// </summary>
    public void Clear()
    {
        foreach (var resource in _imageCache.Values)
        {
            resource.Dispose();
        }
        
        _imageCache.Clear();
        _pathToIdMapping.Clear();
        _nextResourceId = 1;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Clear();
            _disposed = true;
        }
    }

    /// <summary>
    /// 获取管理器摘要信息
    /// </summary>
    /// <returns>摘要字符串</returns>
    public override string ToString()
    {
        return $"ImageResourceManager[Images={_imageCache.Count}, NextId={_nextResourceId}]";
    }
}

/// <summary>
/// 图像资源统计信息
/// </summary>
public class ImageResourceStatistics
{
    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 总大小（字节）
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// PNG图像数量
    /// </summary>
    public int PngCount { get; set; }

    /// <summary>
    /// JPEG图像数量
    /// </summary>
    public int JpegCount { get; set; }

    /// <summary>
    /// GIF图像数量
    /// </summary>
    public int GifCount { get; set; }

    /// <summary>
    /// BMP图像数量
    /// </summary>
    public int BmpCount { get; set; }

    /// <summary>
    /// TIFF图像数量
    /// </summary>
    public int TiffCount { get; set; }

    /// <summary>
    /// 平均图像大小（字节）
    /// </summary>
    public double AverageSize => TotalCount > 0 ? (double)TotalSize / TotalCount : 0;

    /// <summary>
    /// 获取统计摘要
    /// </summary>
    /// <returns>统计摘要字符串</returns>
    public override string ToString()
    {
        return $"Statistics[Total={TotalCount}, Size={TotalSize:N0} bytes, PNG={PngCount}, JPEG={JpegCount}, GIF={GifCount}, BMP={BmpCount}, TIFF={TiffCount}]";
    }
}