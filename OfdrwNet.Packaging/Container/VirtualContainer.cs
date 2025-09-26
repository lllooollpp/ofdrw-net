using System.Security.Cryptography;
using System.Xml.Linq;
using OfdrwNet.Core;
using OfdrwNet.Core.Container;
using OfdrwNet.Core.BasicType;
using System.Diagnostics;

namespace OfdrwNet.Packaging.Container;

/// <summary>
/// 虚拟容器对象
/// 对应 Java 版本的 org.ofdrw.pkg.container.VirtualContainer
/// 用于管理 OFD 文档的目录结构和文件
/// </summary>
public class VirtualContainer : IContainer
{
    /// <summary>
    /// 文件根路径（完整路径包含当前文件名）
    /// </summary>
    private string _fullPath;

    /// <summary>
    /// 目录名称
    /// </summary>
    private string _name;

    /// <summary>
    /// 所属容器
    /// </summary>
    private VirtualContainer? _parent;

    /// <summary>
    /// 文件缓存（XML元素缓存）
    /// </summary>
    private readonly Dictionary<string, XElement> _fileCache;

    /// <summary>
    /// 用于保存读取到的文件的Hash
    /// 因为读取操作导致文档加载到缓存，
    /// 但是文件在flush时候，反序列丢失格式字符等导致文件改动。
    /// </summary>
    private readonly Dictionary<string, byte[]> _fileSrcHash;

    /// <summary>
    /// 目录中的虚拟容器缓存
    /// </summary>
    private readonly Dictionary<string, VirtualContainer> _dirCache;

    /// <summary>
    /// 已解析文件缓存：key=请求的 fileName（逻辑名），value=实际绝对路径或 null(表示确实不存在)
    /// 减少重复的不存在路径探测、异常与深度遍历。
    /// </summary>
    private readonly Dictionary<string, string?> _resolvedFileCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 是否已释放
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 获取虚拟容器的名称
    /// </summary>
    /// <returns>名称</returns>
    public string GetContainerName()
    {
        return _name;
    }

    /// <summary>
    /// 私有构造函数，初始化缓存
    /// </summary>
    private VirtualContainer()
    {
        _fileCache = new Dictionary<string, XElement>(7);
        _dirCache = new Dictionary<string, VirtualContainer>(5);
        _fileSrcHash = new Dictionary<string, byte[]>(7);
        _parent = this;
        _fullPath = "";
        _name = "";
    }

    /// <summary>
    /// 通过完整路径构造一个虚拟容器
    /// </summary>
    /// <param name="fullDir">容器完整路径</param>
    /// <exception cref="ArgumentException">参数错误</exception>
    public VirtualContainer(string fullDir) : this()
    {
        if (string.IsNullOrEmpty(fullDir))
        {
            throw new ArgumentException("完整路径(fullDir)为空");
        }

        // 目录不存在或不是一个目录
        if (!Directory.Exists(fullDir))
        {
            try
            {
                // 创建目录
                Directory.CreateDirectory(fullDir);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("无法创建指定目录", e);
            }
        }

        _fullPath = Path.GetFullPath(fullDir);
        _name = Path.GetFileName(_fullPath) ?? "";
    }

    /// <summary>
    /// 创建一个虚拟容器
    /// </summary>
    /// <param name="parent">根目录</param>
    /// <param name="dirName">新建目录的名称</param>
    /// <exception cref="ArgumentException">参数异常</exception>
    public VirtualContainer(string parent, string dirName) : this()
    {
        if (string.IsNullOrEmpty(parent))
        {
            throw new ArgumentException("根路径(parent)为空");
        }

        string fullPath = Path.Combine(parent, dirName);
        if (!Directory.Exists(fullPath))
        {
            try
            {
                Directory.CreateDirectory(fullPath);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("无法创建指定目录", e);
            }
        }

        if (!Directory.Exists(parent))
        {
            throw new InvalidOperationException("请传入基础目录路径，而不是文件");
        }

        _fullPath = Path.GetFullPath(fullPath);
        _name = dirName;
    }

    /// <summary>
    /// 获取当前容器完整路径
    /// </summary>
    /// <returns>容器完整路径（绝对路径）</returns>
    public string GetSysAbsPath()
    {
        return _fullPath;
    }

    /// <summary>
    /// 向虚拟容器中加入文件
    /// </summary>
    /// <param name="file">文件路径</param>
    /// <returns>this</returns>
    /// <exception cref="IOException">IO异常</exception>
    public VirtualContainer PutFile(string file)
    {
        PutFileWithPath(file);
        return this;
    }

    /// <summary>
    /// 实际执行文件复制的方法
    /// </summary>
    /// <param name="file">源文件路径</param>
    /// <returns>目标文件路径</returns>
    private string PutFileWithPath(string file)
    {
        if (!File.Exists(file))
        {
            LogMissing("PutFileWithPath", file, _fullPath);
            throw new FileNotFoundException($"文件不存在: {file}");
        }

        string fileName = Path.GetFileName(file);
        string target = Path.Combine(_fullPath, fileName);

        // 检查文件是否已存在且内容相同
        if (File.Exists(target))
        {
            byte[] sourceHash = ComputeFileHash(file);
            byte[] targetHash = ComputeFileHash(target);

            if (sourceHash.SequenceEqual(targetHash))
            {
                return target;
            }
            else
            {
                // 修改更名文件名称，添加前缀时间防止冲突
                string prefix = DateTime.Now.ToString("yyyyMMddHHmmss_");
                target = Path.Combine(_fullPath, prefix + fileName);
            }
        }

        // 复制文件到指定目录
        File.Copy(file, target, true);
        return target;
    }

    /// <summary>
    /// 计算文件的哈希值
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件哈希值</returns>
    private static byte[] ComputeFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        return sha256.ComputeHash(stream);
    }

    /// <summary>
    /// 向虚拟容器中直接加入流类型资源
    /// 根据提供文件名称创建文件
    /// 输入流内容将直接写入文件内，不做检查
    /// 若文件已经存在，那么将会覆盖原文件！
    /// </summary>
    /// <param name="fileName">文件名称</param>
    /// <param name="inputStream">输入流</param>
    /// <returns>this</returns>
    public VirtualContainer AddRaw(string fileName, Stream inputStream)
    {
        string target = Path.Combine(_fullPath, fileName);
        using var fileStream = File.Create(target);
        inputStream.CopyTo(fileStream);
        return this;
    }

    /// <summary>
    /// 向容器加入原始数据流（IContainer接口实现）
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="inputStream">原始数据流</param>
    /// <returns>容器接口</returns>
    IContainer IContainer.AddRaw(string fileName, Stream inputStream)
    {
        return AddRaw(fileName, inputStream);
    }

    /// <summary>
    /// 向虚拟容器加入对象
    /// 对象将会被序列化为XML并存储
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="obj">OFD对象</param>
    /// <returns>this</returns>
    public VirtualContainer PutObj(string fileName, OfdElement obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj), "待加入对象为空");
        }

        _fileCache[fileName] = obj.ToXElement();
        return this;
    }

    /// <summary>
    /// 向容器加入OFD对象（IContainer接口实现）
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="obj">OFD对象</param>
    /// <returns>容器接口</returns>
    IContainer IContainer.PutObj(string fileName, OfdElement obj)
    {
        return PutObj(fileName, obj);
    }

    /// <summary>
    /// 向虚拟容器加入字符串内容
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="content">字符串内容</param>
    /// <returns>this</returns>
    public VirtualContainer PutObj(string fileName, string content)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException("文件名不能为空", nameof(fileName));
        }

        if (content != null)
        {
            string filePath = Path.Combine(_fullPath, fileName);
            File.WriteAllText(filePath, content, System.Text.Encoding.UTF8);
        }

        return this;
    }

    /// <summary>
    /// 向虚拟容器加入字节数组
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="data">字节数组数据</param>
    /// <returns>this</returns>
    public async Task PutObjAsync(string fileName, byte[] data)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException("文件名不能为空", nameof(fileName));
        }

        string filePath = Path.Combine(_fullPath, fileName);
        await File.WriteAllBytesAsync(filePath, data ?? Array.Empty<byte>());
    }

    /// <summary>
    /// 通过文件名从容器中获取OFD对象
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>OFD对象对应的XML元素</returns>
    /// <exception cref="FileNotFoundException">文件不存在</exception>
    public XElement GetObj(string fileName)
    {
        // 首先从缓存中查找
        if (_fileCache.TryGetValue(fileName, out var cachedElement))
        {
            return cachedElement;
        }

        // 从文件系统中加载
        string filePath = Path.Combine(_fullPath, fileName);
        if (!File.Exists(filePath))
        {
            LogMissing("GetObj", fileName, _fullPath);
            throw new FileNotFoundException($"容器中不存在文件: {fileName}");
        }

        // 加载XML文件
        var element = XElement.Load(filePath);
        _fileCache[fileName] = element;
        return element;
    }

    /// <summary>
    /// 通过路径获取对象
    /// </summary>
    /// <param name="loc">文件位置</param>
    /// <returns>对象元素，若文件路径不存在则返回null</returns>
    public XElement? GetObj(StLoc loc)
    {
        if (loc == null)
        {
            return null;
        }

        string[] pathParts = loc.ToString()
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        return GetObj(pathParts);
    }

    /// <summary>
    /// 通过路径获取元素
    /// </summary>
    /// <param name="relativeDst">相对路径数组</param>
    /// <returns>元素对象，若文件路径不存在则返回null</returns>
    private XElement? GetObj(string[] relativeDst)
    {
        if (relativeDst == null || relativeDst.Length == 0)
        {
            return null;
        }

        if (relativeDst.Length == 1)
        {
            // 只有一个元素且为空无法找到
            if (string.IsNullOrEmpty(relativeDst[0]))
            {
                return null;
            }

            // 只有一个元素，则从缓存中获取
            try
            {
                return GetObj(relativeDst[0]);
            }
            catch (FileNotFoundException)
            {
                // 找不到文件
                return null;
            }
        }

        try
        {
            var child = GetContainer(relativeDst[0], () => new VirtualContainer());
            // 移除子元素
            string[] sub = relativeDst.Skip(1).ToArray();
            // 递归获取
            return child.GetObj(sub);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// 获取子容器
    /// </summary>
    /// <param name="name">容器名称</param>
    /// <param name="constructor">容器构造函数</param>
    /// <returns>子容器</returns>
    /// <exception cref="FileNotFoundException">容器不存在</exception>
    public T GetContainer<T>(string name, Func<T> constructor) where T : VirtualContainer
    {
        if (_dirCache.TryGetValue(name, out var cached))
        {
            return (T)cached;
        }

        string containerPath = Path.Combine(_fullPath, name);
        if (!Directory.Exists(containerPath))
        {
            LogMissing("GetContainer", name, _fullPath);
            throw new FileNotFoundException($"容器目录不存在: {name}");
        }

        var container = constructor();
        if (container is VirtualContainer vc)
        {
            vc._fullPath = containerPath;
            vc._name = name;
            vc._parent = this;
        }

        _dirCache[name] = container;
        return container;
    }

    /// <summary>
    /// 获取或创建子容器
    /// </summary>
    /// <param name="name">容器名称</param>
    /// <param name="constructor">容器构造函数</param>
    /// <returns>子容器</returns>
    public T ObtainContainer<T>(string name, Func<T> constructor) where T : VirtualContainer
    {
        try
        {
            return GetContainer(name, constructor);
        }
        catch (FileNotFoundException)
        {
            // 容器不存在，创建新的
            string containerPath = Path.Combine(_fullPath, name);
            Directory.CreateDirectory(containerPath);

            var container = constructor();
            if (container is VirtualContainer vc)
            {
                vc._fullPath = containerPath;
                vc._name = name;
                vc._parent = this;
            }

            _dirCache[name] = container;
            return container;
        }
    }

    /// <summary>
    /// 获取或创建子容器（IContainer接口实现）
    /// </summary>
    /// <param name="name">容器名称</param>
    /// <param name="containerFactory">容器工厂函数</param>
    /// <returns>子容器</returns>
    IContainer IContainer.ObtainContainer(string name, Func<IContainer> containerFactory)
    {
        return ObtainContainer(name, () => (VirtualContainer)containerFactory());
    }

    /// <summary>
    /// 获取容器内的绝对位置
    /// </summary>
    /// <returns>绝对位置</returns>
    public StLoc GetAbsLoc()
    {
        if (_parent == this)
        {
            // 根容器
            return new StLoc("/");
        }

        string parentPath = _parent?.GetAbsLoc().ToString() ?? "/";
        return new StLoc(parentPath).Cat(_name);
    }

    /// <summary>
    /// 获取文件
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件路径</returns>
    /// <exception cref="FileNotFoundException">文件不存在</exception>
    public string GetFile(string fileName)
    {
        System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 开始查找文件: {fileName}");

        if (string.IsNullOrWhiteSpace(fileName))
        {
            System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 文件名为空");
            throw new ArgumentException("文件名不能为空", nameof(fileName));
        }

        // 检查缓存
        string? resolvedPath = GetCachedPath(fileName);
        if (resolvedPath != null)
        {
            System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 缓存命中: {fileName} -> {resolvedPath}");
            return resolvedPath;
        }

        System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 缓存未命中，开始路径解析: {fileName}");

        // 尝试各种路径解析策略
        resolvedPath = TryResolveFilePath(fileName);

        if (resolvedPath != null)
        {
            System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 文件解析成功: {fileName} -> {resolvedPath}");
            CacheResolved(fileName, resolvedPath);
            return resolvedPath;
        }

        // 所有策略都失败，记录并抛出异常
        System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 文件查找失败: {fileName} (容器: {_fullPath})");
        LogMissing("GetFile", fileName, _fullPath);
        CacheResolved(fileName, null);
        throw new FileNotFoundException($"文件不存在: {fileName} (容器: {_fullPath})");
    }

    /// <summary>
    /// 从缓存中获取已解析的文件路径
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>缓存的路径，null表示未缓存或已缓存为不存在</returns>
    private string? GetCachedPath(string fileName)
    {
        lock (_resolvedFileCache)
        {
            if (_resolvedFileCache.TryGetValue(fileName, out var cached))
            {
                if (cached == null)
                {
                    // 之前已确认不存在
                    throw new FileNotFoundException($"文件不存在(缓存): {fileName}");
                }

                // 验证缓存路径是否仍然有效
                if (File.Exists(cached))
                {
                    return cached;
                }

                // 缓存失效，清除
                _resolvedFileCache.Remove(fileName);
            }
        }
        return null;
    }

    /// <summary>
    /// 尝试通过多种策略解析文件路径
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>解析到的文件路径，如果失败返回null</returns>
    private string? TryResolveFilePath(string fileName)
    {
        System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 开始路径解析策略: {fileName}");

        // 策略1：根容器相对路径解析（优先处理包含路径的文件名）
        if (fileName.Contains('/') || fileName.Contains('\\'))
        {
            System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 尝试策略1-根容器相对路径: {fileName}");
            string? rootRelativePath = TryRootRelativeResolve(fileName);
            if (rootRelativePath != null)
            {
                System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 策略1成功: {fileName} -> {rootRelativePath}");
                return rootRelativePath;
            }
            System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 策略1失败: {fileName}");
        }

        // 策略2：直接在当前容器查找
        System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 尝试策略2-直接路径: {fileName}");
        string directPath = Path.Combine(_fullPath, fileName);
        if (File.Exists(directPath))
        {
            System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 策略2成功: {fileName} -> {directPath}");
            return directPath;
        }
        System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 策略2失败，路径不存在: {directPath}");

        // 策略3：Doc_x映射规则（作为备用方案）
        System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 尝试策略3-Doc映射: {fileName}");
        string? docMappedPath = TryDocMapping(fileName);
        if (docMappedPath != null)
        {
            System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 策略3成功: {fileName} -> {docMappedPath}");
            return docMappedPath;
        }
        System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 策略3失败: {fileName}");

        // 策略4：资源目录搜索
        System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 尝试策略4-资源目录搜索: {fileName}");
        string? resourcePath = TryResourceDirectorySearch(fileName);
        if (resourcePath != null)
        {
            System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 策略4成功: {fileName} -> {resourcePath}");
            return resourcePath;
        }
        System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 策略4失败: {fileName}");

        // 策略5：深度搜索（最后手段）
        System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 尝试策略5-深度搜索: {fileName}");
        string? deepSearchPath = TryDeepSearch(fileName);
        if (deepSearchPath != null)
        {
            System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 策略5成功: {fileName} -> {deepSearchPath}");
            return deepSearchPath;
        }
        System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 策略5失败: {fileName}");
        System.Diagnostics.Trace.WriteLine($"[VirtualContainer] 所有策略失败: {fileName}");

        return null;
    }

    /// <summary>
    /// 尝试Doc_x相关的路径映射和资源搜索
    /// </summary>
    private string? TryDocMapping(string fileName)
    {
        try
        {
            var normalizedPath = fileName.Replace('\\', '/');
            if (!normalizedPath.StartsWith("Doc_", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var parts = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return null;
            }

            var root = GetRoot();
            var docFolder = parts[0];  // Doc_0, Doc_1, etc.
            var resourceFile = parts[1]; // Image_4.PNG, etc.

            // 优先策略：尝试直接在Doc_x/Res下查找
            var docResPath = Path.Combine(root._fullPath, docFolder, "Res", resourceFile);
            if (File.Exists(docResPath))
            {
                Trace.WriteLine($"[DocMapping-DirectRes] {fileName} -> {docResPath}");
                return docResPath;
            }

            // 备用策略1：尝试在Doc_x根目录查找
            var docDirectPath = Path.Combine(root._fullPath, docFolder, resourceFile);
            if (File.Exists(docDirectPath))
            {
                Trace.WriteLine($"[DocMapping-Direct] {fileName} -> {docDirectPath}");
                return docDirectPath;
            }

            // 备用策略2：映射到统一的Doc/Res结构（兼容旧版本）
            if (resourceFile.StartsWith("Image_", StringComparison.OrdinalIgnoreCase))
            {
                var unifiedDocResPath = Path.Combine(root._fullPath, "Doc", "Res", resourceFile);
                if (File.Exists(unifiedDocResPath))
                {
                    Trace.WriteLine($"[DocMapping-Unified] {fileName} -> {unifiedDocResPath}");
                    return unifiedDocResPath;
                }

                var unifiedDocPath = Path.Combine(root._fullPath, "Doc", resourceFile);
                if (File.Exists(unifiedDocPath))
                {
                    Trace.WriteLine($"[DocMapping-UnifiedDoc] {fileName} -> {unifiedDocPath}");
                    return unifiedDocPath;
                }

                // 尝试扩展名变体
                return TryExtensionVariants(root._fullPath, $"{docFolder}/Res", resourceFile) ??
                       TryExtensionVariants(root._fullPath, docFolder, resourceFile) ??
                       TryExtensionVariants(root._fullPath, "Doc/Res", resourceFile) ??
                       TryExtensionVariants(root._fullPath, "Doc", resourceFile);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 尝试不同扩展名的文件变体
    /// </summary>
    private string? TryExtensionVariants(string basePath, string subDir, string fileName)
    {
        var ext = Path.GetExtension(fileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

        if (string.IsNullOrEmpty(ext))
        {
            return null;
        }

        var extensions = new[] { ".png", ".PNG", ".jpg", ".JPG", ".jpeg", ".JPEG" };
        foreach (var altExt in extensions)
        {
            if (string.Equals(ext, altExt, StringComparison.OrdinalIgnoreCase))
            {
                continue; // 跳过原扩展名
            }

            var altFile = nameWithoutExt + altExt;
            var altPath = Path.Combine(basePath, subDir, altFile);
            if (File.Exists(altPath))
            {
                Trace.WriteLine($"[ExtensionVariant] {fileName} -> {altPath}");
                return altPath;
            }
        }
        return null;
    }

    /// <summary>
    /// 尝试从根容器解析相对路径
    /// </summary>
    private string? TryRootRelativeResolve(string fileName)
    {
        if ((!fileName.Contains('/') && !fileName.Contains('\\')) || Path.IsPathRooted(fileName))
        {
            return null;
        }

        try
        {
            var root = GetRoot();
            var normalizedPath = fileName.Replace('\\', '/').TrimStart('/');
            var parts = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var resolvedPath = Path.Combine(new[] { root._fullPath }.Concat(parts).ToArray());

            if (File.Exists(resolvedPath))
            {
                Trace.WriteLine($"[RootRelative] {fileName} -> {resolvedPath}");
                return resolvedPath;
            }
        }
        catch
        {
            // 忽略路径解析异常
        }
        return null;
    }

    /// <summary>
    /// 在常见资源目录中搜索文件
    /// </summary>
    private string? TryResourceDirectorySearch(string fileName)
    {
        if (fileName.Contains(Path.DirectorySeparatorChar) || fileName.Contains(Path.AltDirectorySeparatorChar))
        {
            return null; // 只搜索简单文件名
        }

        var searchPaths = new List<string>();

        // 从当前容器向上查找Doc_x容器
        var docRoot = FindAncestor(c => c._name.StartsWith("Doc_", StringComparison.OrdinalIgnoreCase));
        if (docRoot != null)
        {
            searchPaths.AddRange(new[]
            {
                Path.Combine(docRoot._fullPath, "Res"),
                Path.Combine(docRoot._fullPath, "Resources"),
                Path.Combine(docRoot._fullPath, "Images"),
                docRoot._fullPath
            });
        }

        // 全局根目录资源搜索
        var root = GetRoot();
        searchPaths.AddRange(new[]
        {
            Path.Combine(root._fullPath, "Res"),
            Path.Combine(root._fullPath, "Resources"),
            Path.Combine(root._fullPath, "Images")
        });

        foreach (var searchPath in searchPaths.Distinct())
        {
            var candidate = Path.Combine(searchPath, fileName);
            if (File.Exists(candidate))
            {
                Trace.WriteLine($"[ResourceSearch] {fileName} -> {candidate}");
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// 深度递归搜索文件（最后手段）
    /// </summary>
    private string? TryDeepSearch(string fileName)
    {
        if (fileName.Contains(Path.DirectorySeparatorChar) || fileName.Contains(Path.AltDirectorySeparatorChar))
        {
            return null; // 避免路径遍历风险
        }

        try
        {
            var root = GetRoot();
            var found = Directory.EnumerateFiles(root._fullPath, fileName, SearchOption.AllDirectories)
                                 .FirstOrDefault();

            if (found != null)
            {
                Trace.WriteLine($"[DeepSearch] {fileName} -> {found}");
                return found;
            }
        }
        catch
        {
            // 忽略搜索异常
        }

        return null;
    }

    private void CacheResolved(string fileName, string? resolved)
    {
        lock (_resolvedFileCache)
        {
            _resolvedFileCache[fileName] = resolved;
        }
    }

    /// <summary>
    /// 查找满足条件的祖先容器（不含自身）
    /// </summary>
    private VirtualContainer? FindAncestor(Func<VirtualContainer, bool> predicate)
    {
        var cur = _parent;
        while (cur != null && cur != this)
        {
            if (predicate(cur)) return cur;
            cur = cur._parent;
        }
        return null;
    }

    /// <summary>
    /// 获取根容器
    /// </summary>
    private VirtualContainer GetRoot()
    {
        var cur = this;
        while (cur._parent != null && cur._parent != cur)
        {
            cur = cur._parent;
        }
        return cur;
    }

    /// <summary>
    /// 刷新缓存到文件系统
    /// </summary>
    public void Flush()
    {
        foreach (var kvp in _fileCache)
        {
            string filePath = Path.Combine(_fullPath, kvp.Key);
            kvp.Value.Save(filePath);
        }

        // 递归刷新子容器
        foreach (var child in _dirCache.Values)
        {
            child.Flush();
        }
    }

    /// <summary>
    /// 记录缺失文件/目录的诊断信息
    /// </summary>
    private void LogMissing(string operation, string fileName, string containerPath)
    {
        try
        {
            var msg = $"[Packaging-Miss] op={operation} file='{fileName}' container='{containerPath}' cwd='{Environment.CurrentDirectory}'";
            System.Diagnostics.Trace.WriteLine(msg);
            Console.Error.WriteLine(msg);
        }
        catch
        {
            // 忽略日志记录异常
        }
    }

    /// <summary>
    /// 异步刷新缓存到文件系统
    /// </summary>
    /// <returns>异步任务</returns>
    public async Task FlushAsync()
    {
        await Task.Run(() => Flush());
    }

    /// <summary>
    /// 清理容器（删除目录和所有内容）
    /// </summary>
    public void Clean()
    {
        if (Directory.Exists(_fullPath))
        {
            Directory.Delete(_fullPath, true);
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源的实际实现
    /// </summary>
    /// <param name="disposing">是否正在释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // 刷新缓存
            Flush();

            // 释放子容器
            foreach (var child in _dirCache.Values)
            {
                child?.Dispose();
            }

            _fileCache.Clear();
            _dirCache.Clear();
            _fileSrcHash.Clear();
            _disposed = true;
        }
    }
}
