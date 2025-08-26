using System.Text.RegularExpressions;
using OfdrwNet.Core;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Packaging.Container;
using System.Xml.Linq;

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

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _workingDirectoryStack.Clear();
        // 注意：不要释放容器，因为它们可能被其他地方使用
    }
}