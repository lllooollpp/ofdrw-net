using System.Text.RegularExpressions;
using OfdrwNet.Core;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Packaging.Container;
using System.Xml.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace OfdrwNet.Reader;

/// <summary>
/// 资源定位器
/// 对应 Java 版本的 org.ofdrw.reader.ResourceLocator
/// 用于在OFD文档中定位和访问资源
/// </summary>
public class ResourceLocator : IDisposable
{
    /// <summary>
    /// 当前工作目录的容器
    /// </summary>
    private VirtualContainer _currentContainer;

    /// <summary>
    /// OFD根目录容器
    /// </summary>
    private readonly VirtualContainer _rootContainer;

    /// <summary>
    /// 工作目录栈，用于save和restore操作
    /// </summary>
    private readonly Stack<string> _workingDirectoryStack;

    /// <summary>
    /// 当前工作目录路径
    /// </summary>
    private string _currentWorkingDirectory;

    // ===== T029: 新增缓存管理和预加载功能字段 =====

    /// <summary>
    /// 资源缓存，键为资源路径，值为缓存的资源对象
    /// </summary>
    private readonly ConcurrentDictionary<string, ResourceCacheEntry> _resourceCache = new ConcurrentDictionary<string, ResourceCacheEntry>();

    /// <summary>
    /// 文件内容缓存，键为文件路径，值为文件内容
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _fileContentCache = new ConcurrentDictionary<string, string>();

    /// <summary>
    /// XML文档缓存，键为文件路径，值为解析的XDocument
    /// </summary>
    private readonly ConcurrentDictionary<string, XDocument> _xmlDocumentCache = new ConcurrentDictionary<string, XDocument>();

    /// <summary>
    /// 缓存配置
    /// </summary>
    public ResourceCacheConfig CacheConfig { get; set; } = new ResourceCacheConfig();

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public ResourceCacheStatistics CacheStatistics { get; private set; } = new ResourceCacheStatistics();

    /// <summary>
    /// 预加载任务取消令牌源
    /// </summary>
    private CancellationTokenSource _preloadCancellationTokenSource = new CancellationTokenSource();

    /// <summary>
    /// 文档路径正则表达式
    /// </summary>
    public static readonly Regex PtDoc = new(@"^/?Doc_\d+/?$", RegexOptions.Compiled);

    /// <summary>
    /// 签名目录路径正则表达式
    /// </summary>
    public static readonly Regex PtSigns = new(@"^/?Doc_\d+/Signs/?$", RegexOptions.Compiled);

    /// <summary>
    /// 单个签名路径正则表达式
    /// </summary>
    public static readonly Regex PtSign = new(@"^/?Doc_\d+/Signs/Sign_\d+/?$", RegexOptions.Compiled);

    /// <summary>
    /// 页面目录路径正则表达式
    /// </summary>
    public static readonly Regex PtPages = new(@"^/?Doc_\d+/Pages/?$", RegexOptions.Compiled);

    /// <summary>
    /// 单个页面路径正则表达式
    /// </summary>
    public static readonly Regex PtPage = new(@"^/?Doc_\d+/Pages/Page_\d+/?$", RegexOptions.Compiled);

    /// <summary>
    /// 页面资源路径正则表达式
    /// </summary>
    public static readonly Regex PtPageRes = new(@"^/?Doc_\d+/Pages/Page_\d+/PageRes\.xml$", RegexOptions.Compiled);

    /// <summary>
    /// 文档资源路径正则表达式
    /// </summary>
    public static readonly Regex PtDocRes = new(@"^/?Doc_\d+/DocumentRes\.xml$", RegexOptions.Compiled);

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="rootContainer">OFD根容器</param>
    public ResourceLocator(VirtualContainer rootContainer)
    {
        _rootContainer = rootContainer ?? throw new ArgumentNullException(nameof(rootContainer));
        _currentContainer = rootContainer;
        _workingDirectoryStack = new Stack<string>();
        _currentWorkingDirectory = "/";
    }

    /// <summary>
    /// 获取当前工作目录
    /// </summary>
    /// <returns>当前工作目录路径</returns>
    public string Pwd()
    {
        return _currentWorkingDirectory;
    }

    /// <summary>
    /// 切换工作目录
    /// </summary>
    /// <param name="path">目标路径</param>
    /// <returns>this</returns>
    public ResourceLocator Cd(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return this;
        }

        string targetPath = ToAbsolutePath(path);
        var targetContainer = GetContainerByPath(targetPath);

        if (targetContainer != null)
        {
            _currentContainer = targetContainer;
            _currentWorkingDirectory = targetPath.TrimEnd('/');
            if (string.IsNullOrEmpty(_currentWorkingDirectory))
            {
                _currentWorkingDirectory = "/";
            }
        }

        return this;
    }

    /// <summary>
    /// 切换到指定的文档容器（DocDir）
    /// </summary>
    /// <param name="docDir">文档容器包装器</param>
    /// <returns>this</returns>
    public ResourceLocator Cd(DocDir docDir)
    {
        if (docDir == null)
        {
            return this;
        }

        try
        {
            var container = docDir.ObtainDocDefault();
            if (container != null)
            {
                _currentContainer = container;
                _currentWorkingDirectory = container.GetAbsLoc().ToString();
            }
        }
        catch
        {
            // ignore and keep current
        }

        return this;
    }

    /// <summary>
    /// 保存当前工作目录到栈中
    /// </summary>
    /// <returns>this</returns>
    public ResourceLocator Save()
    {
        _workingDirectoryStack.Push(_currentWorkingDirectory);
        return this;
    }

    /// <summary>
    /// 从栈中恢复工作目录
    /// </summary>
    /// <returns>this</returns>
    public ResourceLocator Restore()
    {
        if (_workingDirectoryStack.Count > 0)
        {
            string savedPath = _workingDirectoryStack.Pop();
            Cd(savedPath);
        }
        return this;
    }

    /// <summary>
    /// 重置工作目录到根目录
    /// </summary>
    /// <returns>this</returns>
    public ResourceLocator RestWd()
    {
        return Cd("/");
    }

    /// <summary>
    /// 转换为绝对路径
    /// </summary>
    /// <param name="relativePath">相对路径</param>
    /// <returns>绝对路径</returns>
    public string ToAbsolutePath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return _currentWorkingDirectory;
        }

        // 如果已经是绝对路径
        if (relativePath.StartsWith("/"))
        {
            return NormalizePath(relativePath);
        }

        // 相对路径处理
        string basePath = _currentWorkingDirectory;
        if (!basePath.EndsWith("/"))
        {
            basePath += "/";
        }

        return NormalizePath(basePath + relativePath);
    }

    /// <summary>
    /// 获取相对于当前位置的绝对路径
    /// </summary>
    /// <param name="loc">位置对象</param>
    /// <returns>绝对路径</returns>
    public StLoc GetAbsTo(StLoc loc)
    {
        if (loc == null)
        {
            throw new ArgumentNullException(nameof(loc));
        }

        string absolutePath = ToAbsolutePath(loc.ToString());
        return new StLoc(absolutePath);
    }

    /// <summary>
    /// 获取文件
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件路径</returns>
    public string GetFile(string fileName)
    {
        return _currentContainer.GetFile(fileName);
    }

    /// <summary>
    /// 获取文件
    /// </summary>
    /// <param name="loc">文件位置</param>
    /// <returns>文件路径</returns>
    public string GetFile(StLoc loc)
    {
        if (loc == null)
        {
            throw new ArgumentNullException(nameof(loc));
        }

        string absolutePath = ToAbsolutePath(loc.ToString());
        var container = GetContainerByPath(Path.GetDirectoryName(absolutePath) ?? "/");
        string fileName = Path.GetFileName(absolutePath);

        return container?.GetFile(fileName) ?? throw new FileNotFoundException($"文件不存在: {absolutePath}");
    }

    /// <summary>
    /// 获取对象
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="constructor">对象构造函数</param>
    /// <returns>对象实例</returns>
    public T Get<T>(string fileName, Func<XElement, T> constructor) where T : class
    {
        var element = _currentContainer.GetObj(fileName);
        return constructor(element);
    }

    /// <summary>
    /// 获取对象
    /// </summary>
    /// <param name="loc">文件位置</param>
    /// <param name="constructor">对象构造函数</param>
    /// <returns>对象实例</returns>
    public T Get<T>(StLoc loc, Func<XElement, T> constructor) where T : class
    {
        if (loc == null)
        {
            throw new ArgumentNullException(nameof(loc));
        }

        string absolutePath = ToAbsolutePath(loc.ToString());
        var element = _rootContainer.GetObj(new StLoc(absolutePath));

        if (element == null)
        {
            throw new FileNotFoundException($"文件不存在: {absolutePath}");
        }

        return constructor(element);
    }

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>是否存在</returns>
    public bool Exist(string fileName)
    {
        try
        {
            _currentContainer.GetObj(fileName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取容器
    /// </summary>
    /// <param name="name">容器名称</param>
    /// <returns>容器实例</returns>
    public VirtualContainer GetContainer(string name)
    {
        return _currentContainer.GetContainer(name, () => new VirtualContainer(Path.Combine(_currentContainer.GetSysAbsPath(), name)));
    }

    /// <summary>
    /// 标准化路径
    /// </summary>
    /// <param name="path">原始路径</param>
    /// <returns>标准化后的路径</returns>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "/";
        }

        // 处理 . 和 .. 路径
        string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var normalizedParts = new List<string>();

        foreach (string part in parts)
        {
            if (part == "." || string.IsNullOrEmpty(part))
            {
                continue; // 忽略当前目录引用
            }
            else if (part == "..")
            {
                if (normalizedParts.Count > 0)
                {
                    normalizedParts.RemoveAt(normalizedParts.Count - 1);
                }
            }
            else
            {
                normalizedParts.Add(part);
            }
        }

        string result = "/" + string.Join("/", normalizedParts);
        return result == "/" ? "/" : result;
    }

    /// <summary>
    /// 根据路径获取容器
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>容器实例</returns>
    private VirtualContainer? GetContainerByPath(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
        {
            return _rootContainer;
        }

        string normalizedPath = NormalizePath(path);
        string[] parts = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        VirtualContainer current = _rootContainer;

        foreach (string part in parts)
        {
            try
            {
                current = current.GetContainer(part, () => new VirtualContainer(Path.Combine(current.GetSysAbsPath(), part)));
            }
            catch
            {
                return null; // 容器不存在
            }
        }

        return current;
    }

    // ===== T029: 新增缓存管理和预加载功能方法 =====

    /// <summary>
    /// 带缓存的获取文件内容
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件内容</returns>
    public string GetFileWithCache(string fileName)
    {
        if (!CacheConfig.EnableCache)
        {
            return GetFile(fileName);
        }

        var absolutePath = ToAbsolutePath(fileName);
        var startTime = DateTime.UtcNow;

        if (_fileContentCache.TryGetValue(absolutePath, out var cachedContent))
        {
            CacheStatistics.RecordHit();
            return cachedContent;
        }

        var content = GetFile(fileName);
        var loadTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

        CacheStatistics.RecordMiss(loadTime);

        if (ShouldCache(content.Length))
        {
            _fileContentCache.TryAdd(absolutePath, content);
        }

        return content;
    }

    /// <summary>
    /// 带缓存的获取XML文档
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>XML文档</returns>
    public XDocument GetXmlDocumentWithCache(string fileName)
    {
        if (!CacheConfig.EnableCache)
        {
            var content = GetFile(fileName);
            return XDocument.Parse(content);
        }

        var absolutePath = ToAbsolutePath(fileName);
        var startTime = DateTime.UtcNow;

        if (_xmlDocumentCache.TryGetValue(absolutePath, out var cachedDoc))
        {
            CacheStatistics.RecordHit();
            return cachedDoc;
        }

        var fileContent = GetFile(fileName);
        var xmlDoc = XDocument.Parse(fileContent);
        var loadTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

        CacheStatistics.RecordMiss(loadTime);

        if (ShouldCache(EstimateXmlDocumentSize(xmlDoc)))
        {
            _xmlDocumentCache.TryAdd(absolutePath, xmlDoc);
        }

        return xmlDoc;
    }

    /// <summary>
    /// 带缓存的获取对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="fileName">文件名</param>
    /// <param name="constructor">构造函数</param>
    /// <returns>对象实例</returns>
    public T GetWithCache<T>(string fileName, Func<XElement, T> constructor) where T : class
    {
        if (!CacheConfig.EnableCache)
        {
            return Get(fileName, constructor);
        }

        var absolutePath = ToAbsolutePath(fileName);
        var cacheKey = $"{typeof(T).Name}_{absolutePath}";
        var startTime = DateTime.UtcNow;

        if (_resourceCache.TryGetValue(cacheKey, out var entry) && entry.Resource is T cachedResource)
        {
            entry.UpdateAccess();
            CacheStatistics.RecordHit();
            return cachedResource;
        }

        var resource = Get(fileName, constructor);
        var loadTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

        CacheStatistics.RecordMiss(loadTime);

        if (ShouldCache(EstimateObjectSize(resource)))
        {
            var cacheEntry = new ResourceCacheEntry
            {
                Resource = resource,
                Size = EstimateObjectSize(resource),
                ResourceType = typeof(T).Name,
                CreatedTime = DateTime.UtcNow,
                LastAccessTime = DateTime.UtcNow,
                AccessCount = 1
            };

            _resourceCache.TryAdd(cacheKey, cacheEntry);
        }

        return resource;
    }

    /// <summary>
    /// 异步预加载资源
    /// </summary>
    /// <param name="resourcePaths">资源路径列表</param>
    /// <returns>预加载任务</returns>
    public async Task PreloadResourcesAsync(IEnumerable<string> resourcePaths)
    {
        if (!CacheConfig.EnablePreloading)
        {
            return;
        }

        var semaphore = new SemaphoreSlim(CacheConfig.PreloadConcurrency);
        var tasks = resourcePaths.Select(async path =>
        {
            await semaphore.WaitAsync(_preloadCancellationTokenSource.Token);
            try
            {
                if (_preloadCancellationTokenSource.Token.IsCancellationRequested)
                    return;

                await PreloadSingleResourceAsync(path);
                CacheStatistics.RecordPreload();
            }
            catch (OperationCanceledException)
            {
                // 忽略取消异常
            }
            catch
            {
                // 忽略预加载错误，不影响正常功能
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 预加载单个资源
    /// </summary>
    /// <param name="resourcePath">资源路径</param>
    private async Task PreloadSingleResourceAsync(string resourcePath)
    {
        await Task.Run(() =>
        {
            try
            {
                // 尝试预加载文件内容
                if (resourcePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    GetXmlDocumentWithCache(resourcePath);
                }
                else
                {
                    GetFileWithCache(resourcePath);
                }

                // 标记为预加载资源
                var absolutePath = ToAbsolutePath(resourcePath);
                if (_fileContentCache.ContainsKey(absolutePath) || _xmlDocumentCache.ContainsKey(absolutePath))
                {
                    var preloadCacheKey = $"preload_{absolutePath}";
                    _resourceCache.TryAdd(preloadCacheKey, new ResourceCacheEntry
                    {
                        Resource = null,
                        IsPreloaded = true,
                        ResourceType = "PreloadMarker"
                    });
                }
            }
            catch
            {
                // 忽略预加载错误
            }
        });
    }

    /// <summary>
    /// 清理过期缓存
    /// </summary>
    public void CleanupExpiredCache()
    {
        CleanupExpiredFileCache();
        CleanupExpiredXmlCache();
        CleanupExpiredResourceCache();
    }

    /// <summary>
    /// 清理所有缓存
    /// </summary>
    public void ClearAllCache()
    {
        _fileContentCache.Clear();
        _xmlDocumentCache.Clear();
        _resourceCache.Clear();
        CacheStatistics.Reset();
    }

    /// <summary>
    /// 获取缓存使用情况
    /// </summary>
    /// <returns>缓存使用报告</returns>
    public ResourceCacheUsageReport GetCacheUsageReport()
    {
        long totalMemoryUsage = 0;

        // 计算文件内容缓存内存使用
        foreach (var content in _fileContentCache.Values)
        {
            totalMemoryUsage += content.Length * 2; // 字符串UTF-16编码
        }

        // 计算XML文档缓存内存使用
        foreach (var doc in _xmlDocumentCache.Values)
        {
            totalMemoryUsage += EstimateXmlDocumentSize(doc);
        }

        // 计算资源缓存内存使用
        foreach (var entry in _resourceCache.Values)
        {
            totalMemoryUsage += entry.Size;
        }

        return new ResourceCacheUsageReport
        {
            TotalMemoryUsage = totalMemoryUsage,
            FileContentCacheCount = _fileContentCache.Count,
            XmlDocumentCacheCount = _xmlDocumentCache.Count,
            ResourceCacheCount = _resourceCache.Count,
            Statistics = CacheStatistics,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 强制执行缓存清理策略
    /// </summary>
    public void EnforceCachePolicy()
    {
        var report = GetCacheUsageReport();

        // 检查内存使用限制
        if (report.TotalMemoryUsage > CacheConfig.MaxMemoryUsage)
        {
            EvictLeastRecentlyUsedResources();
        }

        // 检查条目数量限制
        var totalEntries = report.FileContentCacheCount + report.XmlDocumentCacheCount + report.ResourceCacheCount;
        if (totalEntries > CacheConfig.MaxCacheEntries)
        {
            EvictOldestEntries();
        }

        // 清理过期条目
        CleanupExpiredCache();
    }

    /// <summary>
    /// 取消预加载任务
    /// </summary>
    public void CancelPreloading()
    {
        _preloadCancellationTokenSource.Cancel();
        _preloadCancellationTokenSource.Dispose();
        _preloadCancellationTokenSource = new CancellationTokenSource();
    }

    // 私有辅助方法

    /// <summary>
    /// 判断是否应该缓存
    /// </summary>
    private bool ShouldCache(long size)
    {
        if (!CacheConfig.EnableCache)
            return false;

        var report = GetCacheUsageReport();
        return report.TotalMemoryUsage + size <= CacheConfig.MaxMemoryUsage;
    }

    /// <summary>
    /// 估算XML文档大小
    /// </summary>
    private long EstimateXmlDocumentSize(XDocument doc)
    {
        // 简化估算：XML文档的字符串长度 * 2（UTF-16）
        return doc.ToString().Length * 2;
    }

    /// <summary>
    /// 估算对象大小
    /// </summary>
    private long EstimateObjectSize(object obj)
    {
        // 简化估算，实际应该使用更精确的方法
        return obj?.ToString()?.Length * 2 ?? 256;
    }

    /// <summary>
    /// 清理过期文件缓存
    /// </summary>
    private void CleanupExpiredFileCache()
    {
        // 文件内容缓存没有时间戳，这里简化处理
        if (_fileContentCache.Count > CacheConfig.MaxCacheEntries / 3)
        {
            var keysToRemove = _fileContentCache.Keys.Take(_fileContentCache.Count / 4).ToList();
            foreach (var key in keysToRemove)
            {
                _fileContentCache.TryRemove(key, out _);
                CacheStatistics.RecordEviction();
            }
        }
    }

    /// <summary>
    /// 清理过期XML缓存
    /// </summary>
    private void CleanupExpiredXmlCache()
    {
        if (_xmlDocumentCache.Count > CacheConfig.MaxCacheEntries / 3)
        {
            var keysToRemove = _xmlDocumentCache.Keys.Take(_xmlDocumentCache.Count / 4).ToList();
            foreach (var key in keysToRemove)
            {
                _xmlDocumentCache.TryRemove(key, out _);
                CacheStatistics.RecordEviction();
            }
        }
    }

    /// <summary>
    /// 清理过期资源缓存
    /// </summary>
    private void CleanupExpiredResourceCache()
    {
        var expiredKeys = _resourceCache
            .Where(kvp => kvp.Value.IsExpired(CacheConfig.CacheExpiration))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _resourceCache.TryRemove(key, out _);
            CacheStatistics.RecordEviction();
        }
    }

    /// <summary>
    /// 清理最少使用的资源
    /// </summary>
    private void EvictLeastRecentlyUsedResources()
    {
        var entriesToEvict = _resourceCache.Values
            .OrderBy(e => e.LastAccessTime)
            .Take(_resourceCache.Count / 4)
            .ToList();

        foreach (var entry in entriesToEvict)
        {
            var keyToRemove = _resourceCache.FirstOrDefault(kvp => kvp.Value == entry).Key;
            if (keyToRemove != null)
            {
                _resourceCache.TryRemove(keyToRemove, out _);
                CacheStatistics.RecordEviction();
            }
        }
    }

    /// <summary>
    /// 清理最旧的条目
    /// </summary>
    private void EvictOldestEntries()
    {
        var entriesToEvict = _resourceCache.Values
            .OrderBy(e => e.CreatedTime)
            .Take(_resourceCache.Count / 4)
            .ToList();

        foreach (var entry in entriesToEvict)
        {
            var keyToRemove = _resourceCache.FirstOrDefault(kvp => kvp.Value == entry).Key;
            if (keyToRemove != null)
            {
                _resourceCache.TryRemove(keyToRemove, out _);
                CacheStatistics.RecordEviction();
            }
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // T029: 增强的资源清理
        CancelPreloading();
        ClearAllCache();

        _workingDirectoryStack.Clear();
        // 注意：不要释放容器，因为它们可能被其他地方使用
    }
}
